using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Mscc.GenerativeAI;



public class FoodNoteRequest
{
    public string Description { get; set; } = string.Empty;
}

public class NutritionResult
{
    public int Calories { get; set; }
    public int Protein { get; set; }
    public int Carbs { get; set; }
    public int Fat { get; set; }
    public double Hydration { get; set; }
}

public class ItemNutrition
{
    public string Name { get; set; } = string.Empty;
    public int Calories { get; set; }
    public int Protein { get; set; }
    public int Carbs { get; set; }
    public int Fat { get; set; }
}

public class ModelNutritionResponse
{
    public NutritionResult Totals { get; set; }
    public List<ItemNutrition> Items { get; set; }
}

[Route("api/nutrition")]
[ApiController]
public class NutritionController : ControllerBase
{
    // Cache for AI suggestions: email -> (suggestion, timestamp)
    private static Dictionary<string, (string suggestion, DateTime timestamp)> _suggestionCache = new();
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromHours(1);

    private readonly string _geminiApiKey;
    private readonly ILogger<NutritionController> _logger;
    private readonly Microsoft.Extensions.Configuration.IConfiguration _configuration;

    public NutritionController(IConfiguration configuration, ILogger<NutritionController> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _geminiApiKey = configuration["Gemini:ApiKey"] ?? string.Empty;

        if (string.IsNullOrEmpty(_geminiApiKey))
        {
            _logger.LogError("Gemini API Key is missing. Please set 'Gemini:ApiKey' in configuration.");
            throw new InvalidOperationException("Gemini API Key is not configured.");
        }
    }

    [HttpGet("daily")]
    public async Task<IActionResult> DailySummary([FromQuery] string email)
    {
        try
        {
            var connStr = _configuration.GetConnectionString("DefaultConnection");
            await using var conn = new Npgsql.NpgsqlConnection(connStr);
            await conn.OpenAsync();

            var query = "SELECT * FROM t_food_data WHERE c_email = @email ORDER BY c_food_name";
            await using var cmd = new Npgsql.NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("email", email ?? (object)DBNull.Value);

            var items = new System.Collections.Generic.List<string>();
            await using var reader = await cmd.ExecuteReaderAsync();

            // detect date-like column
            string dateCol = null;
            for (int i = 0; i < reader.FieldCount; i++)
            {
                var n = reader.GetName(i).ToLowerInvariant();
                if (n.Contains("date") || n.Contains("created") || n.Contains("time"))
                {
                    dateCol = reader.GetName(i);
                    break;
                }
            }

            while (await reader.ReadAsync())
            {
                bool include = true;
                if (!string.IsNullOrEmpty(dateCol))
                {
                    var val = reader[dateCol];
                    if (val == DBNull.Value) include = false;
                    else
                    {
                        DateTime dt;
                        if (val is DateTime d) dt = d;
                        else if (!DateTime.TryParse(val.ToString(), out dt)) include = false;

                        // compare date portion
                        if (include)
                        {
                            var localDate = dt.Date;
                            var today = DateTime.UtcNow.Date;
                            // allow server local dates too
                            if (localDate != today && localDate != DateTime.Now.Date) include = false;
                        }
                    }
                }

                if (!include) continue;

                var food = reader["c_food_name"] != DBNull.Value ? reader["c_food_name"].ToString() : string.Empty;
                // Use only the food name for the AI description as requested
                var piece = food ?? string.Empty;
                items.Add(piece);
            }

            if (items.Count == 0)
            {
                return Ok(new NutritionResult { Calories = 0, Protein = 0, Carbs = 0, Fat = 0 });
            }

            var description = string.Join("; ", items);
            var result = await GetCaloriesFromAI(description);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to compute daily summary for {Email}", email);
            return StatusCode(500, new { message = "Failed to compute daily summary." });
        }
    }

    [HttpGet("goal")]
    public async Task<IActionResult> GetUserGoalTargets([FromQuery] string email)
    {
        try
        {
            var connStr = _configuration.GetConnectionString("DefaultConnection");
            await using var conn = new Npgsql.NpgsqlConnection(connStr);
            await conn.OpenAsync();

            // read latest health info for user
            var query = "SELECT c_weight, c_goal FROM t_healthinfo WHERE c_email = @email LIMIT 1";
            await using var cmd = new Npgsql.NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("email", email ?? (object)DBNull.Value);

            double weight = 0;
            string goal = string.Empty;

            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                if (reader["c_weight"] != DBNull.Value) double.TryParse(reader["c_weight"].ToString(), out weight);
                if (reader["c_goal"] != DBNull.Value) goal = reader["c_goal"].ToString() ?? string.Empty;
            }

            // Compute baseline calories
            int baseCalories = 2000;
            if (weight > 0)
            {
                // approximate basal daily need: 24 kcal per kg
                baseCalories = (int)Math.Round(24.0 * weight);
            }

            var g = (goal ?? string.Empty).ToLowerInvariant();
            int targetCalories = baseCalories;
            if (g.Contains("increase") || g.Contains("gain") || g.Contains("bulking")) targetCalories = baseCalories + 500;
            else if (g.Contains("lose") || g.Contains("loss") || g.Contains("cut") || g.Contains("reduce")) targetCalories = Math.Max(1200, baseCalories - 500);

            // protein grams: prefer weight-based if available
            int proteinGrams;
            if (weight > 0)
            {
                double factor = 1.6; // maintenance
                if (g.Contains("increase") || g.Contains("gain")) factor = 1.8;
                else if (g.Contains("lose") || g.Contains("loss") || g.Contains("cut")) factor = 1.8;
                proteinGrams = (int)Math.Round(weight * factor);
            }
            else
            {
                proteinGrams = (int)Math.Round((targetCalories * 0.2) / 4.0);
            }

            // fat calories ~25% of total
            var fatCalories = targetCalories * 0.25;
            var fatGrams = (int)Math.Round(fatCalories / 9.0);

            // carbs grams consume remaining calories
            var remainingCalories = targetCalories - (proteinGrams * 4) - (fatGrams * 9);
            var carbGrams = remainingCalories > 0 ? (int)Math.Round(remainingCalories / 4.0) : 0;

            // message of the day
            var messages = new[] {
                "Message of the day: Keep going — small steps add up!",
                "Message of the day: You're doing great — stay consistent!",
                "Message of the day: Nice work — track progress and adjust as needed.",
                "Message of the day: Keep it up — hydrate and rest well!"
            };
            var rnd = new Random();
            var motd = messages[rnd.Next(messages.Length)];

            return Ok(new { calories = targetCalories, protein = proteinGrams, carbs = carbGrams, fat = fatGrams, message = motd, goal = goal });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to compute goal targets for {Email}", email);
            return StatusCode(500, new { message = "Failed to compute goal targets." });
        }
    }

    [HttpGet("suggestions")]
    public async Task<IActionResult> GetNutritionSuggestions([FromQuery] string email)
    {
        try
        {
            // Check if suggestion is cached and still valid
            if (_suggestionCache.TryGetValue(email ?? "", out var cached))
            {
                if (DateTime.UtcNow - cached.timestamp < CacheExpiration)
                {
                    _logger.LogInformation("Returning cached suggestion for {Email}", email);
                    return Ok(new { suggestion = cached.suggestion });
                }
                else
                {
                    // Cache expired, remove it
                    _suggestionCache.Remove(email ?? "");
                }
            }

            var connStr = _configuration.GetConnectionString("DefaultConnection");
            await using var conn = new Npgsql.NpgsqlConnection(connStr);
            await conn.OpenAsync();

            // Get today's nutrition
            var foodItems = new List<string>();
            var dailyQuery = "SELECT c_food_name FROM t_food_data WHERE c_email = @email";
            await using var dailyCmd = new Npgsql.NpgsqlCommand(dailyQuery, conn);
            dailyCmd.Parameters.AddWithValue("email", email ?? (object)DBNull.Value);

            await using (var dailyReader = await dailyCmd.ExecuteReaderAsync())
            {
                while (await dailyReader.ReadAsync())
                {
                    var food = dailyReader["c_food_name"] != DBNull.Value ? dailyReader["c_food_name"].ToString() : string.Empty;
                    if (!string.IsNullOrEmpty(food)) foodItems.Add(food);
                }
            }

            // Get user goal - use separate connection to avoid "operation in progress" error
            string userGoal = string.Empty;
            await using var conn2 = new Npgsql.NpgsqlConnection(connStr);
            await conn2.OpenAsync();
            
            var goalQuery = "SELECT c_goal FROM t_healthinfo WHERE c_email = @email LIMIT 1";
            await using var goalCmd = new Npgsql.NpgsqlCommand(goalQuery, conn2);
            goalCmd.Parameters.AddWithValue("email", email ?? (object)DBNull.Value);

            await using (var goalReader = await goalCmd.ExecuteReaderAsync())
            {
                if (await goalReader.ReadAsync())
                {
                    if (goalReader["c_goal"] != DBNull.Value) userGoal = goalReader["c_goal"].ToString() ?? string.Empty;
                }
            }

            // Generate AI suggestion based on foods eaten and goal
            string suggestion = await GenerateSuggestion(string.Join("; ", foodItems), userGoal);
            
            // Cache the suggestion
            _suggestionCache[email ?? ""] = (suggestion, DateTime.UtcNow);
            _logger.LogInformation("Generated and cached suggestion for {Email}", email);

            return Ok(new { suggestion = suggestion });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate suggestions for {Email}", email);
            return StatusCode(500, new { message = "Failed to generate suggestions." });
        }
    }

    private async Task<string> GenerateSuggestion(string foodsEaten, string userGoal)
    {
        if (string.IsNullOrEmpty(foodsEaten))
        {
            return "You haven't logged any meals yet. Start by adding some food items to get personalized suggestions!";
        }

        var googleAI = new GoogleAI(apiKey: _geminiApiKey);
        var model = googleAI.GenerativeModel(model: Model.Gemini25Flash);

        var prompt = $@"You are a nutrition advisor. Based on the foods eaten today and the user's goal, provide ONE specific, actionable suggestion.

Foods eaten today: {foodsEaten}
User's goal: {userGoal}

Provide a brief, friendly suggestion (1-2 sentences max) that is:
- Specific to the foods they ate
- Aligned with their goal (weight gain, weight loss, maintenance, etc.)
- Actionable (what they should do or eat next)
- Motivational

Keep it short and conversational. No bullet points.";

        var content = new Content
        {
            Parts = new List<IPart>
            {
                new TextData { Text = prompt }
            }
        };

        var request = new GenerateContentRequest
        {
            Contents = new List<Content> { content }
        };

        var response = await model.GenerateContent(request);
        return response.Text?.Trim() ?? "Keep up with your nutrition goals!";
    }

    [HttpPost("analyze")]
    [ProducesResponseType(typeof(NutritionResult), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> AnalyzeFood([FromBody] FoodNoteRequest input)
    {
        if (input == null || string.IsNullOrWhiteSpace(input.Description))
        {
            return BadRequest(new { message = "Food description is required." });
        }

        _logger.LogInformation("Attempting to analyze food: {Description}", input.Description);

        try
        {
            var result = await GetCaloriesFromAI(input.Description);
            _logger.LogInformation("Successfully analyzed food: Calories={Calories}", result.Calories);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze food description: {Description}", input.Description);
            return StatusCode(500, new { message = "An internal service error occurred while processing the request." });
        }
    }

    private async Task<NutritionResult> GetCaloriesFromAI(string description)
    {
        var googleAI = new GoogleAI(apiKey: _geminiApiKey);
        var model = googleAI.GenerativeModel(model: Model.Gemini25Flash);

                var prompt = $@"You are a nutrition analysis assistant. Analyze the following semicolon-separated list
of food items (meal and food name, optional notes) and provide a PER-ITEM breakdown plus the TOTAL nutritional
intake for today.

Return ONLY a single JSON object (no explanation, no markdown, no extra text) with these exact properties:

{{
    ""items"": [
        {{ ""name"": ""string"", ""calories"": integer, ""protein"": integer, ""carbs"": integer, ""fat"": integer }},
        ...
    ],
    ""totals"": {{ ""calories"": integer, ""protein"": integer, ""carbs"": integer, ""fat"": integer, ""hydration"": number }}
}}

Rules:
- Provide an entry in the ""items"" array for each food; ""name"" should be the meal and food (and optional notes) as a short string.
- All macro values (calories, protein, carbs, fat) should be whole numbers (integers). Hydration should be a number in liters with one decimal place.
- Round macro values to the nearest whole number.
- If unable to estimate a value for an item, use 0 for that numeric field.
- Respond with JSON ONLY — do not include any explanatory text, code blocks or markdown.

Example valid output:
{{
    ""items"": [
        {{ ""name"": ""Breakfast: oatmeal (with banana)"", ""calories"": 300, ""protein"": 8, ""carbs"": 54, ""fat"": 5 }},
        {{ ""name"": ""Lunch: grilled chicken sandwich"", ""calories"": 550, ""protein"": 35, ""carbs"": 45, ""fat"": 18 }}
    ],
    ""totals"": {{ ""calories"": 850, ""protein"": 43, ""carbs"": 99, ""fat"": 23, ""hydration"": 1.2 }}
}}

Input description: '{description}'
";

        // ✅ Use TextData with property initializer instead of constructor
        var content = new Content
        {
            Parts = new List<IPart>
            {
                new TextData { Text = prompt }
            }
        };

        var request = new GenerateContentRequest
        {
            Contents = new List<Content> { content },
            GenerationConfig = new GenerationConfig
            {
                ResponseMimeType = "application/json"
            }
        };

        // ✅ Generate response
        var response = await model.GenerateContent(request);

        // Ensure the model returned some text
        if (string.IsNullOrWhiteSpace(response.Text))
        {
            _logger.LogWarning("Gemini API returned an empty response for description: {Description}", description);
            return new NutritionResult();
        }

        try
        {
            var rawText = response.Text.Trim();

            // Unwrap quoted JSON if necessary
            if (rawText.StartsWith("\"") && rawText.EndsWith("\""))
            {
                rawText = JsonSerializer.Deserialize<string>(rawText) ?? rawText;
            }

            // First try to parse a detailed response with per-item breakdown.
            try
            {
                var modelResp = JsonSerializer.Deserialize<ModelNutritionResponse>(rawText,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (modelResp != null && modelResp.Items != null && modelResp.Items.Count > 0)
                {
                    // Recompute authoritative totals from item breakdown to avoid model inconsistencies
                    var caloriesSum = modelResp.Items.Sum(i => i.Calories);
                    var proteinSum = modelResp.Items.Sum(i => i.Protein);
                    var carbsSum = modelResp.Items.Sum(i => i.Carbs);
                    var fatSum = modelResp.Items.Sum(i => i.Fat);

                    var hydration = modelResp.Totals?.Hydration ?? 0.0;

                    return new NutritionResult
                    {
                        Calories = caloriesSum,
                        Protein = proteinSum,
                        Carbs = carbsSum,
                        Fat = fatSum,
                        Hydration = Math.Round(hydration, 1)
                    };
                }
            }
            catch (JsonException) {  }

            // Fallback: parse the simple totals-only response
            var simple = JsonSerializer.Deserialize<NutritionResult>(rawText,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return simple ?? new NutritionResult();
        }
        catch (JsonException jEx)
        {
            _logger.LogError(jEx, "AI returned malformed JSON for description: {Description}. Raw response: {Response}", description, response.Text);
            return new NutritionResult();
        }
    }



}
