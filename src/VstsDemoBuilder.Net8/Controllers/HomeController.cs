using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VstsDemoBuilder.Infrastructure;

namespace VstsDemoBuilder.Controllers
{
    public class HomeController : LegacyController
    {
        [AllowAnonymous]
        public IActionResult Index()
        {
            return View();
        }

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }
    }
}
