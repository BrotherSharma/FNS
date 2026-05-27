using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using FNS.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace FNS.Controllers
{
    public class ImproveController : Controller
    {
        private readonly IImprove _logger;

        public ImproveController(IImprove logger)
        {
            _logger = logger;
        }

        [HttpPost]
        public IActionResult SubmitHealthInfo([FromBody] JsonElement healthInfo)
        {

            string email = GetStringProperty(healthInfo, "email") ?? HttpContext.Session.GetString("Email");
            string name = GetStringProperty(healthInfo, "name") ?? HttpContext.Session.GetString("Name");
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(email))
            {
                return RedirectToAction("Login", "User");
            }
            var result = _logger.SaveHealthInfoAsync(healthInfo, email);
            HttpContext.Session.SetString("Name", name); 
            HttpContext.Session.SetString("Email", email);

            string firstName = GetStringProperty(healthInfo, "firstName") ?? HttpContext.Session.GetString("FirstName");
            string lastName = GetStringProperty(healthInfo, "lastName") ?? HttpContext.Session.GetString("LastName");
            if (!string.IsNullOrEmpty(firstName))
            {
                HttpContext.Session.SetString("FirstName", firstName);
            }
            if (!string.IsNullOrEmpty(lastName))
            {
                HttpContext.Session.SetString("LastName", lastName);
            }

            if (result)
            {
                return Ok(new { message = "Your health information has been successfully submitted!", success = true });
            }
            else
            {
                return StatusCode(500, new { message = "There was an error while saving your information." });
            }
        }

        private static string GetStringProperty(JsonElement element, string propertyName)
        {
            if (element.ValueKind == JsonValueKind.Object &&
                element.TryGetProperty(propertyName, out var property) &&
                property.ValueKind != JsonValueKind.Null)
            {
                return property.GetString();
            }

            return null;
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error!");
        }
    }
}
