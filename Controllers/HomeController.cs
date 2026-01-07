using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using FNS.Models;

namespace FNS.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        var Name = HttpContext.Session.GetString("Name");
        var Email = HttpContext.Session.GetString("Email");
        if (string.IsNullOrEmpty(Name) || string.IsNullOrEmpty(Email))
        {
            return RedirectToAction("Login", "User");
        }

        ViewBag.Name = Name;
        ViewBag.Email = Email;
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

        
}
