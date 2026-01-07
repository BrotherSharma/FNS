using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Npgsql;
using FNS.Models;
using System.Security.Cryptography;
using System.Text;
using System.Data;
using System.Text.Json;

namespace FNS.Repository
{
    public class Improve : IImprove
    {
        private readonly NpgsqlConnection _con;

        public Improve(NpgsqlConnection con)
        {
            _con = con;
        }

        public bool SaveHealthInfoAsync(JsonElement healthInfo, string email)
        {
            try
            {
                double weight = 0;
                double height = 0;
                int age = 0;
                string goal = string.Empty;
                string referral = string.Empty;
                string diet = string.Empty;
                string lifestyle = string.Empty;
                string bloodType = string.Empty;
                double sleepPatterns = 0;
                string alcoholConsumption = string.Empty;

                if (healthInfo.TryGetProperty("weight", out var w))
                {
                    if (w.ValueKind == JsonValueKind.Number) weight = w.GetDouble();
                    else double.TryParse(w.GetString(), out weight);
                }
                if (healthInfo.TryGetProperty("height", out var h))
                {
                    if (h.ValueKind == JsonValueKind.Number) height = h.GetDouble();
                    else double.TryParse(h.GetString(), out height);
                }
                if (healthInfo.TryGetProperty("age", out var a))
                {
                    if (a.ValueKind == JsonValueKind.Number) age = a.GetInt32();
                    else int.TryParse(a.GetString(), out age);
                }
                if (healthInfo.TryGetProperty("goal", out var g)) goal = g.GetString() ?? string.Empty;
                if (healthInfo.TryGetProperty("referral", out var r)) referral = r.GetString() ?? string.Empty;
                if (healthInfo.TryGetProperty("diet", out var d)) diet = d.GetString() ?? string.Empty;
                if (healthInfo.TryGetProperty("lifestyle", out var l)) lifestyle = l.GetString() ?? string.Empty;
                if (healthInfo.TryGetProperty("bloodType", out var b)) bloodType = b.GetString() ?? string.Empty;
                if (healthInfo.TryGetProperty("sleepPatterns", out var s))
                {
                    if (s.ValueKind == JsonValueKind.Number) sleepPatterns = s.GetDouble();
                    else double.TryParse(s.GetString(), out sleepPatterns);
                }
                if (healthInfo.TryGetProperty("alcoholConsumption", out var ac)) alcoholConsumption = ac.GetString() ?? string.Empty;

                _con.Open();

                string query = "INSERT INTO public.t_healthInfo (c_weight, c_height, c_age, c_goal, c_referral, c_diet, c_lifestyle, c_bloodType, c_sleepPatterns, c_alcoholConsumption, c_email) " +
                            "VALUES (@Weight, @Height, @Age, @Goal, @Referral, @Diet, @Lifestyle, @BloodType, @SleepPatterns, @AlcoholConsumption, @email)";

                // Using NpgsqlCommand to execute the query
                using (NpgsqlCommand cmd = new NpgsqlCommand(query, _con))
                {
                    // Add parameters to the command to prevent SQL injection
                    cmd.Parameters.AddWithValue("@Weight", weight);
                    cmd.Parameters.AddWithValue("@Height", height);
                    cmd.Parameters.AddWithValue("@Age", age);
                    cmd.Parameters.AddWithValue("@Goal", goal);
                    cmd.Parameters.AddWithValue("@Referral", (object)referral ?? DBNull.Value);  // Allow null for referral
                    cmd.Parameters.AddWithValue("@Diet", diet);
                    cmd.Parameters.AddWithValue("@Lifestyle", lifestyle);
                    cmd.Parameters.AddWithValue("@BloodType", bloodType);
                    cmd.Parameters.AddWithValue("@SleepPatterns", sleepPatterns);
                    cmd.Parameters.AddWithValue("@AlcoholConsumption", alcoholConsumption);
                    cmd.Parameters.AddWithValue("@email", email);

                    int rowsAffected = cmd.ExecuteNonQuery();

                    return rowsAffected > 0;
                }
            }
            catch (Exception ex)
            {
                // Handle the exception (log it, etc.)
                Console.WriteLine("Error: " + ex.Message);
                return false;
            }
            finally
            {
                // Ensure the connection is closed asynchronously
                if (_con.State == System.Data.ConnectionState.Open)
                {
                    _con.Close();
                }
            }
        }











        // Helper method to execute scalar query (for count or any other scalar query)
        private int ExecuteScalarQuery(string query, params NpgsqlParameter[] parameters)
        {
            using (var cmd = new NpgsqlCommand(query, _con))
            {
                cmd.Parameters.AddRange(parameters); // Add parameters to command
                return Convert.ToInt32(cmd.ExecuteScalar()); // Execute the query and return the result as an integer
            }
        }

        // Simple logging method (for demonstration purposes, you can replace with a proper logging framework like Serilog or NLog)
        private void LogError(string message, Exception ex)
        {
            // Log the error (you can save it to a file, database, or send it to a logging system)
            Console.WriteLine($"{message}: {ex.Message}");
        }
    }

    }
