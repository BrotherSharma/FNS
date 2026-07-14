using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FNS.Controllers
{
    [Route("[controller]")]
    public class FoodLogController : Controller
    {
        private readonly IConfiguration _configuration;

        public FoodLogController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost("Save")]
        public async Task<IActionResult> Save([FromForm] string foodName, [FromForm] string meal, [FromForm] string notes, [FromForm] string email)
        {
            string query = @"
                    INSERT INTO t_food_data
                    (c_food_name, c_meal, c_notes, c_email)
                    VALUES
                    (@food, @meal, @notes, @email)";

            try
            {
                var connStr = _configuration.GetConnectionString("DefaultConnection");
                await using var conn = new NpgsqlConnection(connStr);
                await conn.OpenAsync();
                await using var cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("food", foodName ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("meal", meal ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("notes", notes ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("email", email ?? (object)DBNull.Value);

                var rows = await cmd.ExecuteNonQueryAsync();
                if (rows > 0)
                    return Json(new { success = true });
                else
                    return Json(new { success = false, message = "No rows affected." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("List")]
        public async Task<IActionResult> List([FromQuery] string email, [FromQuery] int page = 1, [FromQuery] int? pageSize = null, [FromQuery] string search = null)
        {
            var results = new List<object>();
            var defaultPageSize = _configuration.GetValue<int?>("FoodLog:DefaultPageSize") ?? 10;
            var effectivePageSize = pageSize.GetValueOrDefault(defaultPageSize);

            if (page < 1)
                page = 1;

            if (effectivePageSize < 1)
                effectivePageSize = defaultPageSize;

            if (effectivePageSize < 1)
                effectivePageSize = 10;

            effectivePageSize = Math.Min(effectivePageSize, 100);
            var offset = (page - 1) * effectivePageSize;
            var hasSearch = !string.IsNullOrWhiteSpace(search);
            var searchTerm = hasSearch ? $"%{search.Trim()}%" : string.Empty;

            string whereClause = @"
                WHERE c_email = @email
                  AND (
                      @hasSearch = false
                      OR c_food_name ILIKE @search
                      OR c_meal ILIKE @search
                      OR c_notes ILIKE @search
                  )";

            string countQuery = $"SELECT COUNT(*) FROM t_food_data {whereClause}";

            // select all columns so we can detect a date-like column if present
            string query = $@"
                SELECT *
                FROM t_food_data
                {whereClause}
                ORDER BY c_food_name
                LIMIT @pageSize OFFSET @offset";
            try
            {
                var connStr = _configuration.GetConnectionString("DefaultConnection");
                await using var conn = new NpgsqlConnection(connStr);
                await conn.OpenAsync();

                var totalRecords = 0;
                await using (var countCmd = new NpgsqlCommand(countQuery, conn))
                {
                    countCmd.Parameters.AddWithValue("email", email ?? (object)DBNull.Value);
                    countCmd.Parameters.AddWithValue("hasSearch", hasSearch);
                    countCmd.Parameters.AddWithValue("search", searchTerm);
                    totalRecords = Convert.ToInt32(await countCmd.ExecuteScalarAsync());
                }

                await using var cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("email", email ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("hasSearch", hasSearch);
                cmd.Parameters.AddWithValue("search", searchTerm);
                cmd.Parameters.AddWithValue("pageSize", effectivePageSize);
                cmd.Parameters.AddWithValue("offset", offset);

                await using var reader = await cmd.ExecuteReaderAsync();
                // determine if there is a date-like column name
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
                    string foodName = reader["c_food_name"] != DBNull.Value ? reader["c_food_name"].ToString() : string.Empty;
                    string meal = reader["c_meal"] != DBNull.Value ? reader["c_meal"].ToString() : string.Empty;
                    string notes = reader["c_notes"] != DBNull.Value ? reader["c_notes"].ToString() : string.Empty;
                    string mail = reader["c_email"] != DBNull.Value ? reader["c_email"].ToString() : string.Empty;
                    string dateVal = string.Empty;
                    if (!string.IsNullOrEmpty(dateCol))
                    {
                        var val = reader[dateCol];
                        if (val != DBNull.Value)
                        {
                            if (val is DateTime dt)
                                dateVal = dt.ToString("yyyy-MM-dd HH:mm:ss");
                            else
                                dateVal = val.ToString();
                        }
                    }

                    results.Add(new
                    {
                        foodName = foodName,
                        meal = meal,
                        notes = notes,
                        email = mail,
                        date = dateVal
                    });
                }

                var totalPages = (int)Math.Ceiling(totalRecords / (double)effectivePageSize);

                return Json(new
                {
                    items = results,
                    page = page,
                    pageSize = effectivePageSize,
                    totalRecords = totalRecords,
                    totalPages = totalPages,
                    defaultPageSize = defaultPageSize
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
