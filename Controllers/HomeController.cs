using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using FNS.Models;
using FNS.Repository;

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
        var name = HttpContext.Session.GetString("Name");
        var firstName = HttpContext.Session.GetString("FirstName");
        var lastName = HttpContext.Session.GetString("LastName");
        var email = HttpContext.Session.GetString("Email");
        
        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(email))
        {
            return RedirectToAction("Login", "User");
        }

        // Check if user has been approved through payment approval workflow

        ViewBag.Name = name;
        ViewBag.Email = email;
        ViewBag.FirstName = firstName;
        ViewBag.LastName = lastName;
        return View();
    }

    public IActionResult AwaitingApproval()
    {
        var email = HttpContext.Session.GetString("Email");
        ViewBag.Email = email;
        return View();
    }

    public IActionResult AccessDenied()
    {
        var email = HttpContext.Session.GetString("Email");
        ViewBag.Email = email;
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
