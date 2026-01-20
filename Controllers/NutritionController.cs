using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
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
}

[Route("api/nutrition")]
[ApiController]
public class NutritionController : ControllerBase
{
    private readonly string _geminiApiKey;
    private readonly ILogger<NutritionController> _logger;

    public NutritionController(IConfiguration configuration, ILogger<NutritionController> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _geminiApiKey = configuration["Gemini:ApiKey"] ?? string.Empty;

        if (string.IsNullOrEmpty(_geminiApiKey))
        {
            _logger.LogError("Gemini API Key is missing. Please set 'Gemini:ApiKey' in configuration.");
            throw new InvalidOperationException("Gemini API Key is not configured.");
        }
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

        var prompt =
            $"Analyze the following food description and provide its estimated nutritional content as a JSON object " +
            $"with whole number values for calories, protein (grams), carbs (grams), and fat (grams). " +
            $"Description: '{description}'";

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

        // ✅ Ensure there is actual text in the response
        if (string.IsNullOrWhiteSpace(response.Text))
        {
            _logger.LogWarning("Gemini API returned an empty response for description: {Description}", description);
            return new NutritionResult();
        }

        try
        {
            var rawText = response.Text.Trim();

            // Step 2: If response.Text is a quoted JSON string, unescape it
            if (rawText.StartsWith("\"") && rawText.EndsWith("\""))
            {
                rawText = JsonSerializer.Deserialize<string>(rawText) ?? rawText;
            }

            // Step 3: Now deserialize the actual object
            var result = JsonSerializer.Deserialize<NutritionResult>(rawText,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return result ?? new NutritionResult();
        }
        catch (JsonException jEx)
        {
            _logger.LogError(jEx, "AI returned malformed JSON for description: {Description}. Raw response: {Response}", description, response.Text);
            return new NutritionResult();
        }
    }



}
