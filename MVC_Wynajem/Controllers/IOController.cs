using Microsoft.AspNetCore.Mvc;
using Reservo.Models;
using Reservo.Attributes;
using System.Linq;

namespace Reservo.Controllers
{
    public class IOController : Controller
    {
        private readonly AppDbContext _db;

        public IOController(AppDbContext db)
        {
            _db = db;
        }

        // GET: /IO/Logowanie
        [HttpGet]
        public IActionResult Logowanie()
        {
            return View();
        }

        // POST: /IO/Logowanie
        [HttpPost]
        public IActionResult Logowanie(string login, string haslo)
        {
            var user = _db.Users.SingleOrDefault(u => u.Username == login);
            if (user != null && AuthHelper.VerifyPassword(user, user.Password, haslo))
            {
                HttpContext.Session.SetString("IsLogged", "true");
                HttpContext.Session.SetString("UserId", user.Id.ToString());
                HttpContext.Session.SetString("Username", user.Username);
                HttpContext.Session.SetString("UserRole", user.Role);
                return RedirectToAction("Zalogowany");
            }

            var oldUser = _db.Loginy.SingleOrDefault(u => u.User == login);
            if (oldUser != null)
            {
                var hasher = new Microsoft.AspNetCore.Identity.PasswordHasher<Login>();
                var result = hasher.VerifyHashedPassword(oldUser, oldUser.Password, haslo);
                if (result == Microsoft.AspNetCore.Identity.PasswordVerificationResult.Success)
                {
                    HttpContext.Session.SetString("IsLogged", "true");
                    HttpContext.Session.SetString("Username", oldUser.User);
                    HttpContext.Session.SetString("UserRole", "Admin");
                    return RedirectToAction("Zalogowany");
                }
            }

            ViewData["Error"] = "Niepoprawny login lub hasło";
            return View();
        }

        // GET: /IO/Zalogowany
        [HttpGet]
        [Authorize]
        public IActionResult Zalogowany()
        {
            ViewData["Username"] = HttpContext.Session.GetString("Username");
            ViewData["UserRole"] = HttpContext.Session.GetString("UserRole");
            return View();
        }

        // POST: /IO/Wyloguj
        [HttpPost]
        public IActionResult Wyloguj()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Logowanie");
        }

        // GET: /IO/Rejestracja - tylko dla adminów
        [HttpGet]
        [Authorize("Admin")]
        public IActionResult Rejestracja()
        {
            return View();
        }

        // POST: /IO/Rejestracja - tylko dla adminów
        [HttpPost]
        [Authorize("Admin")]
        public IActionResult Rejestracja(string login, string haslo, string potwierdzHaslo, string rola = "User")
        {
            // 1) Walidacja
            if (string.IsNullOrWhiteSpace(login) ||
                string.IsNullOrWhiteSpace(haslo) ||
                haslo != potwierdzHaslo)
            {
                ViewData["Error"] = "Niepoprawne dane rejestracji";
                return View();
            }

            // 2) Sprawdź poprawność roli
            if (rola != "Admin" && rola != "User")
            {
                ViewData["Error"] = "Niepoprawna rola";
                return View();
            }

            // 3) Unikalność loginu (sprawdź oba systemy)
            if (_db.Users.Any(u => u.Username == login) || _db.Loginy.Any(u => u.User == login))
            {
                ViewData["Error"] = "Użytkownik o takim loginie już istnieje";
                return View();
            }

            // 4) Hash i zapis
            var user = new User 
            { 
                Username = login,
                Role = rola,
                ApiKey = AuthHelper.GenerateApiKey()
            };
            user.Password = AuthHelper.HashPassword(user, haslo);
            _db.Users.Add(user);
            _db.SaveChanges();

            ViewData["Success"] = "Użytkownik został utworzony pomyślnie";
            return View();
        }

        // GET: /IO/Users - zarządzanie użytkownikami dla adminów
        [HttpGet]
        [Authorize("Admin")]
        public IActionResult Users()
        {
            var users = _db.Users.OrderBy(u => u.Username).ToList();
            return View(users);
        }
    }
}
