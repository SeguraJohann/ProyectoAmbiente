using Microsoft.AspNetCore.Mvc;

namespace ProyectoAmbiente.Controllers
{
    public class TestController : Controller
    {
        public IActionResult TestViewer()
        {
            return View();
        }

        // Endpoint opcional para gestionar PDFs desde el servidor
        [HttpGet("Test/GetPdf")]
        public IActionResult GetPdf(string pdfUrl)
        {
            // Puedes validar la URL aquí antes de devolverla
            // También puedes buscar en una base de datos, etc.

            if (string.IsNullOrEmpty(pdfUrl))
            {
                return BadRequest("URL del PDF no proporcionada");
            }

            // En una aplicación real, podrías:
            // 1. Validar la URL
            // 2. Validar permisos de usuario
            // 3. Registrar accesos
            // 4. Cachear respuestas

            return Ok(new { url = pdfUrl });
        }
    }
}