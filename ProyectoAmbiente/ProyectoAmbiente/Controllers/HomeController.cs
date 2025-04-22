using Microsoft.AspNetCore.Mvc;
using ProyectoAmbiente.Models;
using System.Diagnostics;

namespace ProyectoAmbiente.Controllers{
    //controlador para la gestión de las solicitudes relacionadas a la página príncipal.
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger){
            _logger = logger;
        }

        public IActionResult Index(){
            return View();
        }

        public IActionResult Privacy(){
            return View();
        }

        public IActionResult styleguide(){
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
