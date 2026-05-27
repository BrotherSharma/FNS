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
                string query = "SELECT c_email, c_firstname, c_lastname FROM public.t_user WHERE c_email = @Email AND c_password = @Password";
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
        public DataTable RegisterUser(string email, string password, string firstName, string lastName, string username, string gender, DateTime dob)
        {
            DataTable resultTable = new DataTable();
            try
            {
                _con.Open();

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
            DataRow row = resultDays.NewRow();
            row["daysCount"] = daysDifference;
            row["dob"] = resultTable.Rows[0]["c_dob"];
            row["goal"] = resultTable.Rows[0]["c_goal"];
            resultDays.Rows.Add(row);
            return resultDays;
        }




        public DataTable UpdateUserProfile(string email, string firstName, string lastName, string goal)
        {
            DataTable resultTable = new DataTable();
            try
            {
                _con.Open();

                string query = @"
                    UPDATE public.t_user
                    SET c_firstname = @FirstName, c_lastname = @LastName
                    WHERE c_email = @Email;

                    UPDATE public.t_healthinfo
                    SET c_goal = @Goal
                    WHERE c_email = @Email;
                ";

                using (NpgsqlCommand cmd = new NpgsqlCommand(query, _con))
                {
                    cmd.Parameters.AddWithValue("@Email", email);
                    cmd.Parameters.AddWithValue("@FirstName", firstName);
                    cmd.Parameters.AddWithValue("@LastName", lastName);
                    cmd.Parameters.AddWithValue("@Goal", goal);

                    int rowsAffected = cmd.ExecuteNonQuery();

                    resultTable.Columns.Add("Status", typeof(string));
                    resultTable.Columns.Add("Message", typeof(string));
                    
                    if (rowsAffected > 0)
                    {
                        resultTable.Rows.Add("Success", "Profile updated successfully.");
                    }
                    else
                    {
                        resultTable.Rows.Add("Error", "User not found.");
                    }
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

        public bool UserExistsByEmail(string email)
        {
            try
            {
                _con.Open();
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

        // Simple logging method (for demonstration purposes, you can replace with a proper logging framework like Serilog or NLog)
        private void LogError(string message, Exception ex)
        {
            // Log the error (you can save it to a file, database, or send it to a logging system)
            Console.WriteLine($"{message}: {ex.Message}");
        }
    }

    }
