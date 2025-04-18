using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoAmbiente.Models;
using System.IO;

namespace ProyectoAmbiente.Controllers
{
    public class PDFAdminController : Controller
    {
        private readonly ProyectoAWCSContext _context;
        private readonly ILogger<PDFAdminController> _logger;

        public PDFAdminController(ProyectoAWCSContext context, ILogger<PDFAdminController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: /PDFAdmin/Read/5
        public async Task<IActionResult> Read(int id)
        {
            // Verificar si el usuario está autenticado
            int? usuarioId = HttpContext.Session.GetInt32("UsuarioId");
            if (usuarioId == null)
            {
                return RedirectToAction("Login", "User");
            }

            // Buscar el documento PDF
            var documento = await _context.DocumentosPDF
                .Include(d => d.Usuario)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (documento == null)
            {
                TempData["Message"] = "El documento solicitado no existe.";
                TempData["AlertType"] = "alert-danger";
                return RedirectToAction("Index", "User");
            }

            // Verificar que el documento pertenezca al usuario o sea público
            if (documento.UsuarioId != usuarioId)
            {
                TempData["Message"] = "No tienes permiso para acceder a este documento.";
                TempData["AlertType"] = "alert-danger";
                return RedirectToAction("Index", "User");
            }

            // Verificar que el archivo físico exista
            if (!System.IO.File.Exists(documento.Ruta))
            {
                TempData["Message"] = "El archivo físico no se encuentra en el servidor.";
                TempData["AlertType"] = "alert-danger";
                return RedirectToAction("Index", "User");
            }

            // Obtener el progreso actual del usuario en este documento
            var progreso = await _context.ProgresosMecanografia
                .Where(p => p.UsuarioId == usuarioId && p.DocumentoId == id)
                .OrderByDescending(p => p.UltimaActualizacion)
                .FirstOrDefaultAsync();

            // Si no hay progreso, crear uno nuevo con valores iniciales
            if (progreso == null)
            {
                progreso = new ProgresoMecanografia
                {
                    UsuarioId = usuarioId.Value,
                    DocumentoId = id,
                    PaginaActual = 1,
                    IndiceCaracter = 0,
                    PosicionX = 0,
                    PosicionY = 0,
                    UltimaActualizacion = DateTime.Now,
                    PorcentajeCompletado = 0,
                    TextoCompletado = "",
                    ElementoId = "",
                    FragmentoContexto = ""
                };

                _context.ProgresosMecanografia.Add(progreso);
                await _context.SaveChangesAsync();
            }

            // Pasar los datos a la vista
            ViewBag.Documento = documento;
            ViewBag.Progreso = progreso;

            return View();
        }

        // GET: /PDFAdmin/ObtenerPDF/5
        public async Task<IActionResult> ObtenerPDF(int id)
        {
            // Verificar si el usuario está autenticado
            int? usuarioId = HttpContext.Session.GetInt32("UsuarioId");
            if (usuarioId == null)
            {
                return Unauthorized();
            }

            // Buscar el documento
            var documento = await _context.DocumentosPDF
                .FirstOrDefaultAsync(d => d.Id == id && d.UsuarioId == usuarioId);

            if (documento == null)
            {
                return NotFound();
            }

            // Verificar que el archivo exista
            if (!System.IO.File.Exists(documento.Ruta))
            {
                return NotFound();
            }

            // Leer el archivo y devolverlo como FileResult
            var fileBytes = await System.IO.File.ReadAllBytesAsync(documento.Ruta);
            return File(fileBytes, "application/pdf", documento.Nombre);
        }

        // POST: /PDFAdmin/GuardarProgreso
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuardarProgreso(int documentoId, int paginaActual, int indiceCaracter,
            float posicionX, float posicionY, string elementoId, string fragmentoContexto,
            double porcentajeCompletado, string textoCompletado)
        {
            // Verificar si el usuario está autenticado
            int? usuarioId = HttpContext.Session.GetInt32("UsuarioId");
            if (usuarioId == null)
            {
                return Json(new { success = false, message = "No autorizado" });
            }

            try
            {
                // Buscar si ya existe un progreso para este documento y usuario
                var progreso = await _context.ProgresosMecanografia
                    .Where(p => p.UsuarioId == usuarioId && p.DocumentoId == documentoId)
                    .OrderByDescending(p => p.UltimaActualizacion)
                    .FirstOrDefaultAsync();

                if (progreso == null)
                {
                    // Crear un nuevo progreso
                    progreso = new ProgresoMecanografia
                    {
                        UsuarioId = usuarioId.Value,
                        DocumentoId = documentoId,
                        PaginaActual = paginaActual,
                        IndiceCaracter = indiceCaracter,
                        PosicionX = posicionX,
                        PosicionY = posicionY,
                        ElementoId = elementoId,
                        FragmentoContexto = fragmentoContexto,
                        UltimaActualizacion = DateTime.Now,
                        PorcentajeCompletado = porcentajeCompletado,
                        TextoCompletado = textoCompletado
                    };

                    _context.ProgresosMecanografia.Add(progreso);
                }
                else
                {
                    // Actualizar el progreso existente
                    progreso.PaginaActual = paginaActual;
                    progreso.IndiceCaracter = indiceCaracter;
                    progreso.PosicionX = posicionX;
                    progreso.PosicionY = posicionY;
                    progreso.ElementoId = elementoId;
                    progreso.FragmentoContexto = fragmentoContexto;
                    progreso.UltimaActualizacion = DateTime.Now;
                    progreso.PorcentajeCompletado = porcentajeCompletado;
                    progreso.TextoCompletado = textoCompletado;

                    _context.ProgresosMecanografia.Update(progreso);
                }

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Progreso guardado correctamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al guardar el progreso para el documento {DocumentoId}", documentoId);
                return Json(new { success = false, message = "Error al guardar el progreso" });
            }
        }

        // POST: /PDFAdmin/GuardarEstadisticas
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuardarEstadisticas(int documentoId, double duracionMinutos,
            int wpm, int paginaInicio, int paginaFin)
        {
            // Verificar si el usuario está autenticado
            int? usuarioId = HttpContext.Session.GetInt32("UsuarioId");
            if (usuarioId == null)
            {
                return Json(new { success = false, message = "No autorizado" });
            }

            try
            {
                // Crear un nuevo registro de estadísticas
                var estadisticas = new EstadisticasMecanografia
                {
                    UsuarioId = usuarioId.Value,
                    DocumentoId = documentoId,
                    FechaSesion = DateTime.Now,
                    DuracionMinutos = duracionMinutos,
                    WPM = wpm,
                    PaginaInicio = paginaInicio,
                    PaginaFin = paginaFin
                };

                _context.EstadisticasMecanografia.Add(estadisticas);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Estadísticas guardadas correctamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al guardar las estadísticas para el documento {DocumentoId}", documentoId);
                return Json(new { success = false, message = "Error al guardar las estadísticas" });
            }
        }
    }
}