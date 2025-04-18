using Microsoft.AspNetCore.Mvc;

namespace ProyectoAmbiente.Controllers
{
    public class PDFAdmin : Controller
    {
        public IActionResult Read()
        {
            return View();
        }
    }
}
