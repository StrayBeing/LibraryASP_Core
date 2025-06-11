using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Biblioteka.Models;
using Microsoft.AspNetCore.Authorization;

namespace Biblioteka.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        [AllowAnonymous]
        public IActionResult Index()
        {
            return View();
        }

        [AllowAnonymous]
        public IActionResult Privacy()
        {
            return View();
        }

        [AllowAnonymous]
        public IActionResult ToggleTheme(string returnUrl = "/")
        {
            var currentTheme = Request.Cookies["theme"];
            var newTheme = currentTheme == "dark" ? "light" : "dark";

            Response.Cookies.Append("theme", newTheme, new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                HttpOnly = false,
                Secure = true,
                SameSite = SameSiteMode.Strict
            });

            return LocalRedirect(returnUrl);
        }

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}