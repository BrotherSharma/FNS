using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using FNS.Models;
using FNS.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using FNS.Models;
using System.Data;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace FNS.Controllers
{
    public class UserController : Controller
    {

        private readonly IUserLogin _userLogin;

        public UserController(IUserLogin userLogin)
        {
            _userLogin = userLogin;
        }


        // [HttpPost]
        public IActionResult Login()
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

            return RedirectToAction("Login", "User");
        }




        [HttpGet]
        public IActionResult Register()
        {
            return View(); // This will look for Register.cshtml in Views/User/Register.cshtml
        }

        [HttpPost]
        public IActionResult RegisterUser([FromBody] JsonElement credentials)
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
                ViewBag.Email = email;

                return Ok(new { success = true, message = "User registered successfully." });

            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Internal server error", details = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult streak([FromQuery] string email)
        {
            if (string.IsNullOrEmpty(email))
                return BadRequest(new { message = "Email is required" });

            DataTable streakTable = _userLogin.GetUserStreakByEmail(email); // return int or calculate streak

            if (streakTable == null || streakTable.Rows.Count == 0)
                return NotFound(new { message = "User not found" });

            int streakCount = 0;
            if (streakTable.Rows[0].Table.Columns.Contains("daysCount"))
            {
                streakCount = Convert.ToInt32(streakTable.Rows[0]["daysCount"]);
            }
            else
            {
                // Fallback to first column if column name differs
                streakCount = Convert.ToInt32(streakTable.Rows[0][0]);
            }

            return Ok(streakCount);

        }




        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error!");
        }
    }
}