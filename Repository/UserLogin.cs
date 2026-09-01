using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Npgsql;
using FNS.Models;
using System.Security.Cryptography;
using System.Text;
using System.Data;
using System.Globalization;

namespace FNS.Repository
{
    public class UserLogin : IUserLogin
    {
        private readonly NpgsqlConnection _con;

        public UserLogin(NpgsqlConnection con)
        {
            _con = con;
        }

        public DataTable LoginUser(string email= "", string password = "")
        {
            try
            {
                _con.Open();
                EnsurePremiumColumn();
                string query = "SELECT c_email, c_firstname, c_lastname, c_is_premium FROM public.t_user WHERE c_email = @Email AND c_password = @Password";
                DataTable userTable = new DataTable();

                using (NpgsqlCommand cmd = new NpgsqlCommand(query, _con))
                {
                    cmd.Parameters.AddWithValue("@Email", email);
                    cmd.Parameters.AddWithValue("@Password", password);

                    using (NpgsqlDataReader reader = cmd.ExecuteReader())
                    {
                        userTable.Load(reader);
                    }
                }
                _con.Close();
                return userTable;
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void EnsureProfileImageColumn()
        {
            using var cmd = new NpgsqlCommand(@"
                ALTER TABLE public.t_user
                ADD COLUMN IF NOT EXISTS c_profile_image_path TEXT NULL;", _con);
            cmd.ExecuteNonQuery();
        }

        private void EnsurePremiumColumn()
        {
            using var cmd = new NpgsqlCommand(@"
                ALTER TABLE public.t_user
                ADD COLUMN IF NOT EXISTS c_is_premium BOOLEAN DEFAULT FALSE;", _con);
            cmd.ExecuteNonQuery();
        }

        public DataTable RegisterUser(string email, string password, string firstName, string lastName, string username, string gender, DateTime dob)
        {
            DataTable resultTable = new DataTable();
            try
            {
                _con.Open();
                EnsureProfileImageColumn();
                EnsurePremiumColumn();

                string query = @"
                    INSERT INTO public.t_user (c_email, c_password, c_role, c_firstname, c_lastname, c_username, c_gender, c_dob, c_createddate)
                    VALUES (@Email, @Password, 'EndUser', @FirstName, @LastName, @Username, @Gender, @Dob, NOW());
                ";

                using (NpgsqlCommand cmd = new NpgsqlCommand(query, _con))
                {
                    // Adding parameters to prevent SQL injection
                    cmd.Parameters.AddWithValue("@Email", email);
                    cmd.Parameters.AddWithValue("@Password", password);
                    cmd.Parameters.AddWithValue("@FirstName", firstName);
                    cmd.Parameters.AddWithValue("@LastName", lastName);
                    cmd.Parameters.AddWithValue("@Username", username);
                    cmd.Parameters.AddWithValue("@Gender", gender);
                    cmd.Parameters.AddWithValue("@Dob", dob);

                    cmd.ExecuteNonQuery();

                    resultTable.Columns.Add("Status", typeof(string));
                    resultTable.Columns.Add("Message", typeof(string));
                    resultTable.Rows.Add("Success", "User registered successfully.");
                }
            }
            catch (Exception ex)
            {
                resultTable.Columns.Add("Status", typeof(string));
                resultTable.Columns.Add("Message", typeof(string));
                resultTable.Rows.Add("Error", $"An error occurred: {ex.Message}");
            }
            finally
            {
                _con.Close();
            }
            return resultTable;
        }
        public DataTable GetUserStreakByEmail(string email)
        {
            DataTable resultTable = new DataTable();
            try
            {
                _con.Open();

                string query = @"
                    select * from t_user users inner join t_healthinfo h on h.c_email = users.c_email where users.c_email = @Email ";

                using (NpgsqlCommand cmd = new NpgsqlCommand(query, _con))
                {
                    cmd.Parameters.AddWithValue("@Email", email);

                    using (NpgsqlDataReader reader = cmd.ExecuteReader())
                    {
                        resultTable.Load(reader);
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                if (_con.State == System.Data.ConnectionState.Open)
                    _con.Close();
            }

            // If no rows returned, return zero streak
            if (resultTable == null || resultTable.Rows.Count == 0)
            {
                DataTable emptyDays = new DataTable();
                emptyDays.Columns.Add("daysCount", typeof(int));
                DataRow emptyRow = emptyDays.NewRow();
                emptyRow["daysCount"] = 0;
                emptyDays.Rows.Add(emptyRow);
                return emptyDays;
            }

            object createdObj = resultTable.Rows[0]["c_createddate"];
            DateTime dateValue;
            if (createdObj is DateTime dt)
            {
                dateValue = dt;
            }
            else
            {
                if (!DateTime.TryParse(createdObj?.ToString(), out dateValue))
                {
                    throw new Exception("Invalid date format in DataTable");
                }
            }

            DateTime today = DateTime.Today;
            int daysDifference = (today - dateValue.Date).Days;
            DataTable resultDays = new DataTable();
            resultDays.Columns.Add("daysCount", typeof(int));
            resultDays.Columns.Add("dob", typeof(DateTime));
            resultDays.Columns.Add("goal", typeof(string));
            resultDays.Columns.Add("weight", typeof(double));
            resultDays.Columns.Add("height", typeof(double));
            resultDays.Columns.Add("age", typeof(int));
            resultDays.Columns.Add("diet", typeof(string));
            resultDays.Columns.Add("lifestyle", typeof(string));
            resultDays.Columns.Add("bloodType", typeof(string));
            resultDays.Columns.Add("sleepPatterns", typeof(double));
            resultDays.Columns.Add("gender", typeof(string));
            DataRow row = resultDays.NewRow();
            row["daysCount"] = daysDifference;
            row["dob"] = resultTable.Rows[0]["c_dob"];
            row["goal"] = resultTable.Rows[0]["c_goal"];

            // Health info fields
            var srcRow = resultTable.Rows[0];
            row["weight"] = resultTable.Columns.Contains("c_weight") && srcRow["c_weight"] != DBNull.Value
                ? Convert.ToDouble(srcRow["c_weight"]) : 0.0;
            row["height"] = resultTable.Columns.Contains("c_height") && srcRow["c_height"] != DBNull.Value
                ? Convert.ToDouble(srcRow["c_height"]) : 0.0;
            row["age"] = resultTable.Columns.Contains("c_age") && srcRow["c_age"] != DBNull.Value
                ? Convert.ToInt32(srcRow["c_age"]) : 0;
            row["diet"] = resultTable.Columns.Contains("c_diet") && srcRow["c_diet"] != DBNull.Value
                ? srcRow["c_diet"].ToString() : "";
            row["lifestyle"] = resultTable.Columns.Contains("c_lifestyle") && srcRow["c_lifestyle"] != DBNull.Value
                ? srcRow["c_lifestyle"].ToString() : "";
            row["bloodType"] = resultTable.Columns.Contains("c_bloodtype") && srcRow["c_bloodtype"] != DBNull.Value
                ? srcRow["c_bloodtype"].ToString() : "";
            row["sleepPatterns"] = resultTable.Columns.Contains("c_sleeppatterns") && srcRow["c_sleeppatterns"] != DBNull.Value
                ? Convert.ToDouble(srcRow["c_sleeppatterns"]) : 0.0;
            row["gender"] = resultTable.Columns.Contains("c_gender") && srcRow["c_gender"] != DBNull.Value
                ? srcRow["c_gender"].ToString() : "";
            resultDays.Rows.Add(row);
            return resultDays;
        }




        public DataTable UpdateUserProfile(string email, string firstName, string lastName, string goal, string? profileImagePath = null,
            double? weight = null, double? height = null, int? age = null, string? diet = null,
            string? lifestyle = null, string? bloodType = null, double? sleepPatterns = null, string? gender = null)
        {
            DataTable resultTable = new DataTable();
            try
            {
                _con.Open();
                EnsureProfileImageColumn();

                string userQuery = @"
                    UPDATE public.t_user
                    SET c_firstname = @FirstName,
                        c_lastname = @LastName,
                        c_gender = COALESCE(@Gender, c_gender),
                        c_profile_image_path = COALESCE(@ProfileImagePath, c_profile_image_path)
                    WHERE c_email = @Email;
                ";

                string healthUpdateQuery = @"
                    UPDATE public.t_healthinfo
                    SET c_goal = @Goal,
                        c_weight = COALESCE(@Weight, c_weight),
                        c_height = COALESCE(@Height, c_height),
                        c_age = COALESCE(@Age, c_age),
                        c_diet = COALESCE(@Diet, c_diet),
                        c_lifestyle = COALESCE(@Lifestyle, c_lifestyle),
                        c_bloodtype = COALESCE(@BloodType, c_bloodtype),
                        c_sleeppatterns = COALESCE(@SleepPatterns, c_sleeppatterns)
                    WHERE c_email = @Email;
                ";

                string healthInsertQuery = @"
                    INSERT INTO public.t_healthinfo (c_email, c_goal, c_weight, c_height, c_age, c_diet, c_lifestyle, c_bloodtype, c_sleeppatterns)
                    VALUES (
                        @Email,
                        COALESCE(@Goal, ''),
                        COALESCE(@Weight, 0),
                        COALESCE(@Height, 0),
                        COALESCE(@Age, 0),
                        COALESCE(@Diet, ''),
                        COALESCE(@Lifestyle, ''),
                        COALESCE(@BloodType, ''),
                        COALESCE(@SleepPatterns, 0)
                    );
                ";

                int totalRows = 0;

                using (NpgsqlCommand cmd = new NpgsqlCommand(userQuery, _con))
                {
                    cmd.Parameters.AddWithValue("@Email", email);
                    cmd.Parameters.AddWithValue("@FirstName", firstName);
                    cmd.Parameters.AddWithValue("@LastName", lastName);
                    cmd.Parameters.AddWithValue("@Gender", (object?)gender ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ProfileImagePath", (object?)profileImagePath ?? DBNull.Value);
                    totalRows += cmd.ExecuteNonQuery();
                }

                int healthRowsAffected;
                using (NpgsqlCommand cmd = new NpgsqlCommand(healthUpdateQuery, _con))
                {
                    cmd.Parameters.AddWithValue("@Email", email);
                    cmd.Parameters.AddWithValue("@Goal", goal);
                    cmd.Parameters.AddWithValue("@Weight", weight.HasValue ? (object)weight.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@Height", height.HasValue ? (object)height.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@Age", age.HasValue ? (object)age.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@Diet", (object?)diet ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Lifestyle", (object?)lifestyle ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@BloodType", (object?)bloodType ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@SleepPatterns", sleepPatterns.HasValue ? (object)sleepPatterns.Value : DBNull.Value);
                    healthRowsAffected = cmd.ExecuteNonQuery();
                }

                if (healthRowsAffected == 0)
                {
                    using (NpgsqlCommand cmd = new NpgsqlCommand(healthInsertQuery, _con))
                    {
                        cmd.Parameters.AddWithValue("@Email", email);
                        cmd.Parameters.AddWithValue("@Goal", goal);
                        cmd.Parameters.AddWithValue("@Weight", weight.HasValue ? (object)weight.Value : DBNull.Value);
                        cmd.Parameters.AddWithValue("@Height", height.HasValue ? (object)height.Value : DBNull.Value);
                        cmd.Parameters.AddWithValue("@Age", age.HasValue ? (object)age.Value : DBNull.Value);
                        cmd.Parameters.AddWithValue("@Diet", (object?)diet ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Lifestyle", (object?)lifestyle ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@BloodType", (object?)bloodType ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@SleepPatterns", sleepPatterns.HasValue ? (object)sleepPatterns.Value : DBNull.Value);
                        totalRows += cmd.ExecuteNonQuery();
                    }
                }
                else
                {
                    totalRows += healthRowsAffected;
                }

                resultTable.Columns.Add("Status", typeof(string));
                resultTable.Columns.Add("Message", typeof(string));
                
                if (totalRows > 0)
                {
                    resultTable.Rows.Add("Success", "Profile updated successfully.");
                }
                else
                {
                    resultTable.Rows.Add("Error", "User not found.");
                }
            }
            catch (Exception ex)
            {
                resultTable.Columns.Add("Status", typeof(string));
                resultTable.Columns.Add("Message", typeof(string));
                resultTable.Rows.Add("Error", $"An error occurred: {ex.Message}");
            }
            finally
            {
                _con.Close();
            }
            return resultTable;
        }

        public DataTable UpdateProfileImagePath(string email, string? profileImagePath)
        {
            DataTable resultTable = new DataTable();
            try
            {
                _con.Open();
                EnsureProfileImageColumn();

                using var cmd = new NpgsqlCommand(@"
                    UPDATE public.t_user
                    SET c_profile_image_path = @ProfileImagePath
                    WHERE c_email = @Email;", _con);

                cmd.Parameters.AddWithValue("@Email", email);
                cmd.Parameters.AddWithValue("@ProfileImagePath", (object?)profileImagePath ?? DBNull.Value);

                int rowsAffected = cmd.ExecuteNonQuery();

                resultTable.Columns.Add("Status", typeof(string));
                resultTable.Columns.Add("Message", typeof(string));

                if (rowsAffected > 0)
                {
                    resultTable.Rows.Add("Success", "Profile image updated successfully.");
                }
                else
                {
                    resultTable.Rows.Add("Error", "User not found.");
                }
            }
            catch (Exception ex)
            {
                resultTable.Columns.Add("Status", typeof(string));
                resultTable.Columns.Add("Message", typeof(string));
                resultTable.Rows.Add("Error", $"An error occurred: {ex.Message}");
            }
            finally
            {
                _con.Close();
            }

            return resultTable;
        }

        public string GetProfileImagePath(string email)
        {
            try
            {
                _con.Open();
                EnsureProfileImageColumn();

                using var cmd = new NpgsqlCommand(@"
                    SELECT c_profile_image_path
                    FROM public.t_user
                    WHERE c_email = @Email
                    LIMIT 1;", _con);
                cmd.Parameters.AddWithValue("@Email", email);

                var result = cmd.ExecuteScalar();
                return result == null || result == DBNull.Value ? string.Empty : result.ToString() ?? string.Empty;
            }
            finally
            {
                if (_con.State == ConnectionState.Open)
                    _con.Close();
            }
        }

        public bool UserExistsByEmail(string email)
        {
            try
            {
                _con.Open();
                EnsureProfileImageColumn();
                EnsurePasswordResetTable();

                using (var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM public.t_user WHERE c_email = @Email", _con))
                {
                    cmd.Parameters.AddWithValue("@Email", email);
                    return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                }
            }
            finally
            {
                if (_con.State == ConnectionState.Open)
                    _con.Close();
            }
        }

        public bool SavePasswordResetToken(string email, string token, DateTime expiresAt)
        {
            try
            {
                _con.Open();
                EnsurePasswordResetTable();

                using (var expireOldCmd = new NpgsqlCommand(@"
                    UPDATE public.t_password_reset
                    SET c_used = TRUE
                    WHERE c_email = @Email AND c_used = FALSE;", _con))
                {
                    expireOldCmd.Parameters.AddWithValue("@Email", email);
                    expireOldCmd.ExecuteNonQuery();
                }

                using (var cmd = new NpgsqlCommand(@"
                    INSERT INTO public.t_password_reset (c_email, c_token, c_expires_at, c_used, c_created_at)
                    VALUES (@Email, @Token, @ExpiresAt, FALSE, NOW());", _con))
                {
                    cmd.Parameters.AddWithValue("@Email", email);
                    cmd.Parameters.AddWithValue("@Token", token);
                    cmd.Parameters.AddWithValue("@ExpiresAt", expiresAt);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
            finally
            {
                if (_con.State == ConnectionState.Open)
                    _con.Close();
            }
        }

        public string GetEmailByValidResetToken(string token)
        {
            try
            {
                _con.Open();
                EnsurePasswordResetTable();

                using (var cmd = new NpgsqlCommand(@"
                    SELECT c_email
                    FROM public.t_password_reset
                    WHERE c_token = @Token
                      AND c_used = FALSE
                      AND c_expires_at > NOW()
                    ORDER BY c_created_at DESC
                    LIMIT 1;", _con))
                {
                    cmd.Parameters.AddWithValue("@Token", token);
                    return cmd.ExecuteScalar()?.ToString();
                }
            }
            finally
            {
                if (_con.State == ConnectionState.Open)
                    _con.Close();
            }
        }

        public bool ResetPassword(string token, string newPassword)
        {
            try
            {
                _con.Open();
                EnsurePasswordResetTable();

                using (var transaction = _con.BeginTransaction())
                {
                    string email;
                    using (var getEmailCmd = new NpgsqlCommand(@"
                        SELECT c_email
                        FROM public.t_password_reset
                        WHERE c_token = @Token
                          AND c_used = FALSE
                          AND c_expires_at > NOW()
                        ORDER BY c_created_at DESC
                        LIMIT 1;", _con, transaction))
                    {
                        getEmailCmd.Parameters.AddWithValue("@Token", token);
                        email = getEmailCmd.ExecuteScalar()?.ToString();
                    }

                    if (string.IsNullOrEmpty(email))
                    {
                        transaction.Rollback();
                        return false;
                    }

                    using (var updatePasswordCmd = new NpgsqlCommand(@"
                        UPDATE public.t_user
                        SET c_password = @Password
                        WHERE c_email = @Email;", _con, transaction))
                    {
                        updatePasswordCmd.Parameters.AddWithValue("@Password", newPassword);
                        updatePasswordCmd.Parameters.AddWithValue("@Email", email);
                        if (updatePasswordCmd.ExecuteNonQuery() == 0)
                        {
                            transaction.Rollback();
                            return false;
                        }
                    }

                    using (var markUsedCmd = new NpgsqlCommand(@"
                        UPDATE public.t_password_reset
                        SET c_used = TRUE
                        WHERE c_token = @Token;", _con, transaction))
                    {
                        markUsedCmd.Parameters.AddWithValue("@Token", token);
                        markUsedCmd.ExecuteNonQuery();
                    }

                    transaction.Commit();
                    return true;
                }
            }
            finally
            {
                if (_con.State == ConnectionState.Open)
                    _con.Close();
            }
        }

        public bool UpgradeUserToPremium(string email)
        {
            try
            {
                if (_con.State != ConnectionState.Open)
                    _con.Open();
                
                EnsurePremiumColumn();

                using (var cmd = new NpgsqlCommand(@"
                    UPDATE public.t_user
                    SET c_is_premium = TRUE
                    WHERE c_email = @Email;", _con))
                {
                    cmd.Parameters.AddWithValue("@Email", email);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
            finally
            {
                if (_con.State == ConnectionState.Open)
                    _con.Close();
            }
        }

        private void EnsurePasswordResetTable()
        {
            using (var cmd = new NpgsqlCommand(@"
                CREATE TABLE IF NOT EXISTS public.t_password_reset (
                    c_id SERIAL PRIMARY KEY,
                    c_email TEXT NOT NULL,
                    c_token TEXT NOT NULL,
                    c_expires_at TIMESTAMP NOT NULL,
                    c_used BOOLEAN NOT NULL DEFAULT FALSE,
                    c_created_at TIMESTAMP NOT NULL DEFAULT NOW()
                );", _con))
            {
                cmd.ExecuteNonQuery();
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

        public DataTable FindOrCreateGoogleUser(string email, string firstName, string lastName, string profileImageUrl)
        {
            DataTable userTable = new DataTable();
            try
            {
                _con.Open();
                EnsureProfileImageColumn();
                EnsurePremiumColumn();

                // Check if user already exists
                string selectQuery = "SELECT c_email, c_firstname, c_lastname, c_is_premium FROM public.t_user WHERE c_email = @Email";
                using (NpgsqlCommand selectCmd = new NpgsqlCommand(selectQuery, _con))
                {
                    selectCmd.Parameters.AddWithValue("@Email", email);
                    using (NpgsqlDataReader reader = selectCmd.ExecuteReader())
                    {
                        userTable.Load(reader);
                    }
                }

                if (userTable.Rows.Count > 0)
                {
                    userTable.Columns.Add("IsNewUser", typeof(bool));
                    userTable.Rows[0]["IsNewUser"] = false;

                    // User exists – update profile image from Google if currently empty
                    if (!string.IsNullOrEmpty(profileImageUrl))
                    {
                        string currentImage = GetProfileImagePathInternal(email);
                        if (string.IsNullOrEmpty(currentImage))
                        {
                            using var updateCmd = new NpgsqlCommand(
                                "UPDATE public.t_user SET c_profile_image_path = @Img WHERE c_email = @Email", _con);
                            updateCmd.Parameters.AddWithValue("@Img", profileImageUrl);
                            updateCmd.Parameters.AddWithValue("@Email", email);
                            updateCmd.ExecuteNonQuery();
                        }
                    }
                    return userTable;
                }

                // User doesn't exist – auto-register with Google info
                string randomPassword = Guid.NewGuid().ToString("N").Substring(0, 16);
                string insertQuery = @"
                    INSERT INTO public.t_user (c_email, c_password, c_role, c_firstname, c_lastname, c_username, c_gender, c_dob, c_createddate, c_profile_image_path)
                    VALUES (@Email, @Password, 'EndUser', @FirstName, @LastName, @Username, 'Other', @Dob, NOW(), @ProfileImage)
                    RETURNING c_email, c_firstname, c_lastname, c_is_premium;";

                DataTable newUserTable = new DataTable();
                using (NpgsqlCommand insertCmd = new NpgsqlCommand(insertQuery, _con))
                {
                    insertCmd.Parameters.AddWithValue("@Email", email);
                    insertCmd.Parameters.AddWithValue("@Password", randomPassword);
                    insertCmd.Parameters.AddWithValue("@FirstName", firstName);
                    insertCmd.Parameters.AddWithValue("@LastName", lastName);
                    insertCmd.Parameters.AddWithValue("@Username", email.Split('@')[0]);
                    insertCmd.Parameters.AddWithValue("@Dob", DateTime.Now.Date);
                    insertCmd.Parameters.AddWithValue("@ProfileImage", (object)profileImageUrl ?? DBNull.Value);

                    using (NpgsqlDataReader reader = insertCmd.ExecuteReader())
                    {
                        newUserTable.Load(reader);
                    }
                }

                newUserTable.Columns.Add("IsNewUser", typeof(bool));
                if (newUserTable.Rows.Count > 0)
                {
                    newUserTable.Rows[0]["IsNewUser"] = true;
                }

                return newUserTable;
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                if (_con.State == ConnectionState.Open)
                    _con.Close();
            }
        }

        /// <summary>
        /// Internal helper – reads profile image path without opening/closing the connection
        /// (assumes connection is already open).
        /// </summary>
        private string GetProfileImagePathInternal(string email)
        {
            using var cmd = new NpgsqlCommand(
                "SELECT c_profile_image_path FROM public.t_user WHERE c_email = @Email LIMIT 1;", _con);
            cmd.Parameters.AddWithValue("@Email", email);
            var result = cmd.ExecuteScalar();
            return result == null || result == DBNull.Value ? string.Empty : result.ToString() ?? string.Empty;
        }

        // Simple logging method (for demonstration purposes, you can replace with a proper logging framework like Serilog or NLog)
        private void LogError(string message, Exception ex)
        {
            // Log the error (you can save it to a file, database, or send it to a logging system)
            Console.WriteLine($"{message}: {ex.Message}");
        }
    }

    }
