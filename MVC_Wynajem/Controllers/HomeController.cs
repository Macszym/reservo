using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Reservo.Models;

namespace Reservo.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        // Przekieruj do logowania jeśli nie zalogowany
        if (HttpContext.Session.GetString("IsLogged") != "true")
        {
            return RedirectToAction("Logowanie", "IO");
        }

        // Przekieruj do panelu głównego jeśli zalogowany
        return RedirectToAction("Zalogowany", "IO");
    }

    public IActionResult Reports()
    {
        return RedirectToAction("Index", "Reports");
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
