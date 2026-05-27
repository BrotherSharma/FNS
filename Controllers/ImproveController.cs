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

            string email = healthInfo.GetProperty("email").GetString();
            string name = healthInfo.GetProperty("name").GetString();
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(email))
            {
                return RedirectToAction("Login", "User");
            }
            var result = _logger.SaveHealthInfoAsync(healthInfo, email);
            HttpContext.Session.SetString("Name", name); 
            HttpContext.Session.SetString("Email", email);
            HttpContext.Session.SetString("FirstName", healthInfo.GetProperty("firstName").GetString());
            HttpContext.Session.SetString("LastName", healthInfo.GetProperty("lastName").GetString());

            if (result)
            {
                return Ok(new { message = "Your health information has been successfully submitted!", success = true });
            }
            else
            {
                return StatusCode(500, new { message = "There was an error while saving your information." });
            }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error!");
        }
    }
}