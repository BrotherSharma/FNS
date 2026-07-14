using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using FNS.Models;
using FNS.Repository;
using FNS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Security.Cryptography;
using System.Text;

namespace FNS.Controllers
{
    public class UserController : Controller
    {
        private const long MaxProfileImageSizeBytes = 1 * 1024 * 1024;
        private static readonly HashSet<string> AllowedProfileImageExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".gif",
            ".webp"
        };

        private readonly IUserLogin _userLogin;
        private readonly IEmailService _emailService;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public UserController(IUserLogin userLogin, IEmailService emailService, IWebHostEnvironment webHostEnvironment)
        {
            _userLogin = userLogin;
            _emailService = emailService;
            _webHostEnvironment = webHostEnvironment;
        }


        // [HttpPost]
        public IActionResult Login()
        {
            return View();

        }
        public IActionResult Improve()
        {
            return View();
        }
        [HttpPost]
        public IActionResult Authenticate([FromBody] JsonElement credentials)
        {
            try
            {
                string email = null;
                string password = null;

                if (credentials.ValueKind == JsonValueKind.Object && credentials.EnumerateObject().Any())
                {
                    if (credentials.TryGetProperty("c_email", out var e)) email = e.GetString();
                    if (credentials.TryGetProperty("c_password", out var p)) password = p.GetString();
                }
                else
                {
                    // Fallback: try reading form values in case request is form-encoded
                    email = Request.Form["c_email"].FirstOrDefault();
                    password = Request.Form["c_password"].FirstOrDefault();
                }
                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
                {
                    return BadRequest(new { success = false, message = "Email and password are required." });
                }

                DataTable dtlogin = _userLogin.LoginUser(email, password);
                if (dtlogin.Rows.Count > 0)
                {
                    var firstName = dtlogin.Rows[0]["c_firstname"].ToString();
                    var lastName = dtlogin.Rows[0]["c_lastname"].ToString();
                    var Email = dtlogin.Rows[0]["c_email"].ToString();
                    string name = firstName + " " + lastName;
                    HttpContext.Session.SetString("Name", name);
                    HttpContext.Session.SetString("Email", Email);
                    HttpContext.Session.SetString("FirstName", firstName);
                    HttpContext.Session.SetString("LastName", lastName);
                    return Json(new { success = true });
                }
                else
                {
                    return Json(new { success = false });
                }

            }
            catch (Exception ex)
            {
                // Return exception details for debugging (remove in production)
                System.Diagnostics.Debug.WriteLine($"Authenticate error: {ex}");
                return StatusCode(500, new { success = false, message = ex.Message, details = ex.ToString() });
            }
        }
        public IActionResult Logout()
        {
            // Remove user session data
            HttpContext.Session.Remove("Email");
            HttpContext.Session.Remove("Name");
            HttpContext.Session.Remove("FirstName");
            HttpContext.Session.Remove("LastName");

            return RedirectToAction("Login", "User");
        }




        [HttpGet]
        public IActionResult Register()
        {
            return View(); 
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> RequestPasswordReset([FromBody] JsonElement request)
        {
            string email = null;
            if (request.ValueKind == JsonValueKind.Object &&
                request.TryGetProperty("email", out var emailElement))
            {
                email = emailElement.GetString();
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                return BadRequest(new { success = false, message = "Email is required." });
            }

            if (_userLogin.UserExistsByEmail(email))
            {
                string token = GenerateResetToken();
                DateTime expiresAt = DateTime.Now.AddMinutes(30);

                if (_userLogin.SavePasswordResetToken(email, token, expiresAt))
                {
                    string resetUrl = $"{Request.Scheme}://{Request.Host}/User/ResetPassword?token={Uri.EscapeDataString(token)}";
                    await _emailService.SendPasswordResetEmailAsync(email, resetUrl);
                }
            }

            return Ok(new
            {
                success = true,
                message = "If an account exists for this email, a password reset link has been sent."
            });
        }

        [HttpGet]
        public IActionResult ResetPassword([FromQuery] string token)
        {
            if (string.IsNullOrWhiteSpace(token) ||
                string.IsNullOrWhiteSpace(_userLogin.GetEmailByValidResetToken(token)))
            {
                ViewBag.IsValidToken = false;
                ViewBag.Token = "";
            }
            else
            {
                ViewBag.IsValidToken = true;
                ViewBag.Token = token;
            }

            return View();
        }

        [HttpPost]
        public IActionResult ResetPasswordSubmit([FromBody] JsonElement request)
        {
            string token = null;
            string password = null;

            if (request.ValueKind == JsonValueKind.Object)
            {
                if (request.TryGetProperty("token", out var tokenElement)) token = tokenElement.GetString();
                if (request.TryGetProperty("password", out var passwordElement)) password = passwordElement.GetString();
            }

            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(password))
            {
                return BadRequest(new { success = false, message = "Reset token and new password are required." });
            }

            if (password.Length < 6)
            {
                return BadRequest(new { success = false, message = "Password must be at least 6 characters long." });
            }

            bool updated = _userLogin.ResetPassword(token, password);
            if (!updated)
            {
                return BadRequest(new { success = false, message = "Reset link is invalid or expired." });
            }

            return Ok(new { success = true, message = "Password reset successfully. You can now log in." });
        }

        [HttpPost]
        public async Task<IActionResult> RegisterUser([FromBody] JsonElement credentials)
        {
            try
            {
                var firstName = credentials.GetProperty("c_firstname").GetString();
                var lastName = credentials.GetProperty("c_lastname").GetString();
                var email = credentials.GetProperty("c_email").GetString();
                var password = credentials.GetProperty("c_password").GetString();
                var username = credentials.GetProperty("c_username").GetString(); // Added missing username
                var gender = credentials.GetProperty("c_gender").GetString();

                DateTime dob;
                if (credentials.TryGetProperty("c_dob", out var dobElement))
                {
                    dob = dobElement.GetDateTime();
                }
                else
                {
                    return BadRequest(new { success = false, message = "Date of birth is required." });
                }

                _userLogin.RegisterUser(email, password, firstName, lastName, username, gender, dob);
                string name = firstName + " " + lastName;
                HttpContext.Session.SetString("Name", name);
                HttpContext.Session.SetString("Email", email);
                ViewBag.Name = name;
                ViewBag.FirstName = firstName;
                ViewBag.LastName = lastName;
                ViewBag.Email = email;

                string loginUrl = $"{Request.Scheme}://{Request.Host}/User/Login";
                bool emailSent = await _emailService.SendRegistrationWelcomeEmailAsync(email, name, loginUrl);

                return Ok(new
                {
                    success = true,
                    message = emailSent
                        ? "User registered successfully. Welcome email sent."
                        : "User registered successfully. Welcome email was not sent because email configuration is incomplete.",
                    emailSent
                });

            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Internal server error", details = ex.Message });
            }
        }

        private static string GenerateResetToken()
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
                .Replace("+", "-")
                .Replace("/", "_")
                .TrimEnd('=');
        }

        private static string BuildProfileImageBaseName(string email)
        {
            var normalizedEmail = (email ?? string.Empty).Trim().ToLowerInvariant();
            var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalizedEmail));
            return $"profile-{Convert.ToHexString(hashBytes).ToLowerInvariant().Substring(0, 24)}";
        }

        private string GetProfileImageFolder()
        {
            var folder = Path.Combine(_webHostEnvironment.WebRootPath, "images");
            Directory.CreateDirectory(folder);
            return folder;
        }

        private void DeleteProfileImageFiles(string email)
        {
            var uploadsFolder = GetProfileImageFolder();
            var baseFileName = BuildProfileImageBaseName(email);

            foreach (var extension in AllowedProfileImageExtensions)
            {
                var filePath = Path.Combine(uploadsFolder, baseFileName + extension);
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }
        }

        [HttpGet]
        public IActionResult streak([FromQuery] string email)
        {
            if (string.IsNullOrEmpty(email))
                return BadRequest(new { message = "Email is required" });

            DataTable streakTable = _userLogin.GetUserStreakByEmail(email);

            if (streakTable == null || streakTable.Rows.Count == 0)
                return NotFound(new { message = "User not found" });

            DataRow row = streakTable.Rows[0];

            int streakCount = 0;
            if (streakTable.Columns.Contains("daysCount"))
            {
                streakCount = Convert.ToInt32(row["daysCount"]);
            }
            else
            {
                streakCount = Convert.ToInt32(row[0]);
            }

            // Get DOB and Goal
            string dob = row.Table.Columns.Contains("dob") ? row["dob"]?.ToString() : null;
            string goal = row.Table.Columns.Contains("goal") ? row["goal"]?.ToString() : null;

            return Ok(new
            {
                streak = streakCount,
                dob = dob,
                goal = goal
            });
        }




        [HttpPost]
        public async Task<IActionResult> UpdateProfile([FromForm] string email, [FromForm] string firstName, [FromForm] string lastName, [FromForm] string goal, [FromForm] IFormFile profileImage = null)
        {
            try
            {
                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(lastName))
                {
                    return BadRequest(new { success = false, message = "Email, first name, and last name are required." });
                }

                string profileImagePath = null;
                if (profileImage != null && profileImage.Length > 0)
                {
                    if (profileImage.Length > MaxProfileImageSizeBytes)
                    {
                        return BadRequest(new { success = false, message = "Profile image must be 1 MB or smaller." });
                    }

                    var extension = Path.GetExtension(profileImage.FileName);
                    var safeExtension = string.IsNullOrWhiteSpace(extension) ? ".png" : extension.ToLowerInvariant();

                    if (!AllowedProfileImageExtensions.Contains(safeExtension))
                    {
                        return BadRequest(new { success = false, message = "Only JPG, JPEG, PNG, GIF, and WEBP images are allowed." });
                    }

                    if (!string.IsNullOrWhiteSpace(profileImage.ContentType) && !profileImage.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                    {
                        return BadRequest(new { success = false, message = "The selected file must be an image." });
                    }

                    var uploadsFolder = GetProfileImageFolder();

                    var baseFileName = BuildProfileImageBaseName(email);
                    foreach (var existingExtension in AllowedProfileImageExtensions)
                    {
                        var existingFile = Path.Combine(uploadsFolder, baseFileName + existingExtension);
                        if (System.IO.File.Exists(existingFile) && !string.Equals(existingFile, Path.Combine(uploadsFolder, baseFileName + safeExtension), StringComparison.OrdinalIgnoreCase))
                        {
                            System.IO.File.Delete(existingFile);
                        }
                    }

                    var fileName = $"{baseFileName}{safeExtension}";
                    var filePath = Path.Combine(uploadsFolder, fileName);

                    await using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
                    await profileImage.CopyToAsync(stream);

                    profileImagePath = $"/images/{fileName}";
                }

                DataTable result = _userLogin.UpdateUserProfile(email, firstName, lastName, goal, profileImagePath);
                
                if (result.Rows.Count > 0 && result.Rows[0]["Status"].ToString() == "Success")
                {
                    // Update session with new name values so the profile form reflects updated data after reload
                    string newName = firstName + " " + lastName;
                    HttpContext.Session.SetString("Name", newName);
                    HttpContext.Session.SetString("FirstName", firstName);
                    HttpContext.Session.SetString("LastName", lastName);

                    return Ok(new { success = true, message = "Profile updated successfully." });
                }
                else
                {
                    return StatusCode(500, new { success = false, message = result.Rows[0]["Message"].ToString() });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Internal server error", details = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfileImage([FromForm] string email, [FromForm] IFormFile profileImage)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email) || profileImage == null || profileImage.Length == 0)
                {
                    return BadRequest(new { success = false, message = "Email and profile image are required." });
                }

                if (profileImage.Length > MaxProfileImageSizeBytes)
                {
                    return BadRequest(new { success = false, message = "Profile image must be 1 MB or smaller." });
                }

                var extension = Path.GetExtension(profileImage.FileName);
                var safeExtension = string.IsNullOrWhiteSpace(extension) ? ".png" : extension.ToLowerInvariant();

                if (!AllowedProfileImageExtensions.Contains(safeExtension))
                {
                    return BadRequest(new { success = false, message = "Only JPG, JPEG, PNG, GIF, and WEBP images are allowed." });
                }

                if (!string.IsNullOrWhiteSpace(profileImage.ContentType) && !profileImage.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(new { success = false, message = "The selected file must be an image." });
                }

                DeleteProfileImageFiles(email);

                var uploadsFolder = GetProfileImageFolder();
                var baseFileName = BuildProfileImageBaseName(email);
                var fileName = $"{baseFileName}{safeExtension}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                await using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await profileImage.CopyToAsync(stream);
                }

                var profileImagePath = $"/images/{fileName}";
                DataTable result = _userLogin.UpdateProfileImagePath(email, profileImagePath);

                if (result.Rows.Count > 0 && result.Rows[0]["Status"].ToString() == "Success")
                {
                    return Ok(new { success = true, message = "Profile image updated successfully.", profileImagePath });
                }

                return StatusCode(500, new { success = false, message = result.Rows[0]["Message"].ToString() });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Internal server error", details = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult RemoveProfileImage([FromForm] string email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                {
                    return BadRequest(new { success = false, message = "Email is required." });
                }

                DeleteProfileImageFiles(email);

                DataTable result = _userLogin.UpdateProfileImagePath(email, null);

                if (result.Rows.Count > 0 && result.Rows[0]["Status"].ToString() == "Success")
                {
                    return Ok(new { success = true, message = "Profile image removed successfully." });
                }

                return StatusCode(500, new { success = false, message = result.Rows[0]["Message"].ToString() });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Internal server error", details = ex.Message });
            }
        }




        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error!");
        }
    }
}
