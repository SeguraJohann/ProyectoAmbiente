using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using ProyectoAmbiente.Models;
using iTextSharp.text.pdf;
using System.IO;
using System.Reflection.PortableExecutable;

namespace ProyectoAmbiente.Controllers
{
    public class UserController : Controller
    {
        private readonly ProyectoAWCSContext _context;
        private readonly ILogger<UserController> _logger;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public UserController(ProyectoAWCSContext context, ILogger<UserController> logger, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _logger = logger;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: /User/Index
        public async Task<IActionResult> Index()
        {
            // Verifica si el usuario está autenticado
            int? usuarioId = HttpContext.Session.GetInt32("UsuarioId");
            if (usuarioId == null)
            {
                return RedirectToAction("Login");
            }

            // Obtener información del usuario para mostrar en la vista
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == usuarioId);
            if (usuario == null)
            {
                return RedirectToAction("Login");
            }

            ViewBag.UsuarioEmail = usuario.Email;
            ViewBag.FechaRegistro = usuario.FechaRegistro.ToString("dd MMMM, yyyy");

            // Obtener los PDFs del usuario
            var documentos = await _context.DocumentosPDF
                .Where(d => d.UsuarioId == usuarioId)
                .OrderByDescending(d => d.FechaSubida)
                .ToListAsync();

            return View(documentos);
        }

        // POST: /User/SubirPDF
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubirPDF(List<IFormFile> pdfFiles)
        {
            // Verifica si el usuario está autenticado
            int? usuarioId = HttpContext.Session.GetInt32("UsuarioId");
            if (usuarioId == null)
            {
                return RedirectToAction("Login");
            }

            if (pdfFiles == null || pdfFiles.Count == 0)
            {
                TempData["Message"] = "No se seleccionaron archivos.";
                TempData["AlertType"] = "alert-warning";
                return RedirectToAction("Index");
            }

            // Directorio base para almacenar PDFs
            string pdfsDirectory = "D:\\U\\U\\Ambiente web Cliente Servidor\\Proyecto\\ProyectoAmbiente\\ProyectoAmbiente\\ProyectoAmbiente\\PDFS\\";

            // Crear directorio si no existe
            if (!Directory.Exists(pdfsDirectory))
            {
                Directory.CreateDirectory(pdfsDirectory);
            }

            int archivosSubidos = 0;
            List<string> errores = new List<string>();

            foreach (var pdfFile in pdfFiles)
            {
                // Validar que sea un PDF
                if (Path.GetExtension(pdfFile.FileName).ToLower() != ".pdf")
                {
                    errores.Add($"El archivo {pdfFile.FileName} no es un PDF válido.");
                    continue;
                }

                // Validar tamaño (25MB máximo)
                if (pdfFile.Length > 25 * 1024 * 1024)
                {
                    errores.Add($"El archivo {pdfFile.FileName} excede el tamaño máximo de 25MB.");
                    continue;
                }

                try
                {
                    // Generar nombre único para el archivo
                    string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                    string nombreArchivo = $"{usuarioId}_{timestamp}_{Path.GetFileName(pdfFile.FileName)}";
                    nombreArchivo = nombreArchivo.Replace(" ", "_"); // Eliminar espacios

                    string rutaCompleta = Path.Combine(pdfsDirectory, nombreArchivo);

                    // Guardar el archivo
                    using (var stream = new FileStream(rutaCompleta, FileMode.Create))
                    {
                        await pdfFile.CopyToAsync(stream);
                    }

                    // Obtener el número de páginas del PDF
                    int numPaginas = 0;
                    using (PdfReader reader = new PdfReader(rutaCompleta))
                    {
                        numPaginas = reader.NumberOfPages;
                    }

                    // Guardar información en la base de datos
                    var documentoPDF = new DocumentoPDF
                    {
                        UsuarioId = usuarioId.Value,
                        Nombre = Path.GetFileName(pdfFile.FileName),
                        Ruta = rutaCompleta,
                        FechaSubida = DateTime.Now,
                        TamañoBytes = pdfFile.Length,
                        NumPaginas = numPaginas
                    };

                    documentoPDF.EstructuraJSON = "{}";
                    _context.DocumentosPDF.Add(documentoPDF);

                    await _context.SaveChangesAsync();

                    archivosSubidos++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al subir el archivo {FileName}", pdfFile.FileName);
                    errores.Add($"Error al subir {pdfFile.FileName}: {ex.Message}");
                }
            }

            // Mensaje para el usuario
            if (archivosSubidos > 0)
            {
                TempData["Message"] = $"Se han subido {archivosSubidos} archivo(s) correctamente.";
                TempData["AlertType"] = "alert-success";
            }

            if (errores.Count > 0)
            {
                TempData["Message"] = $"{TempData["Message"]} {string.Join(" ", errores)}";
                TempData["AlertType"] = "alert-warning";
            }

            return RedirectToAction("Index");
        }

        // POST: /User/EliminarPDF
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarPDF(int id)
        {
            // Verifica si el usuario está autenticado
            int? usuarioId = HttpContext.Session.GetInt32("UsuarioId");
            if (usuarioId == null)
            {
                return RedirectToAction("Login");
            }

            // Buscar el documento
            var documento = await _context.DocumentosPDF
                .FirstOrDefaultAsync(d => d.Id == id && d.UsuarioId == usuarioId);

            if (documento == null)
            {
                TempData["Message"] = "El documento no se encontró o no tienes permiso para eliminarlo.";
                TempData["AlertType"] = "alert-danger";
                return RedirectToAction("Index");
            }

            try
            {
                // Eliminar el archivo físico
                if (System.IO.File.Exists(documento.Ruta))
                {
                    System.IO.File.Delete(documento.Ruta);
                }

                // Eliminar los registros de progreso
                var progresos = await _context.ProgresosMecanografia
                    .Where(p => p.DocumentoId == id)
                    .ToListAsync();

                _context.ProgresosMecanografia.RemoveRange(progresos);

                // Eliminar los registros de estadísticas
                var estadisticas = await _context.EstadisticasMecanografia
                    .Where(e => e.DocumentoId == id)
                    .ToListAsync();

                _context.EstadisticasMecanografia.RemoveRange(estadisticas);

                // Eliminar el registro del documento
                _context.DocumentosPDF.Remove(documento);

                await _context.SaveChangesAsync();

                TempData["Message"] = "El documento se ha eliminado correctamente.";
                TempData["AlertType"] = "alert-success";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar el documento {Id}", id);
                TempData["Message"] = $"Error al eliminar el documento: {ex.Message}";
                TempData["AlertType"] = "alert-danger";
            }

            return RedirectToAction("Index");
        }

        // GET: /User/Login
        public IActionResult Login()
        {
            // Si ya está autenticado, redirige a Index
            if (HttpContext.Session.GetInt32("UsuarioId") != null)
            {
                return RedirectToAction("Index");
            }
            return View();
        }

        // POST: /User/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ModelState.AddModelError("", "Debes ingresar un correo y contraseña");
                return View();
            }

            // Busca el usuario en la base de datos
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == email);

            if (usuario != null)
            {
                // Encripta la contraseña para compararla con la almacenada
                string passwordHash = ConvertirSha256(password);

                if (usuario.Password == passwordHash)
                {
                    // Guarda información en la sesión
                    HttpContext.Session.SetInt32("UsuarioId", usuario.Id);
                    HttpContext.Session.SetString("UsuarioNombre", usuario.Nombre);

                    // Actualiza la fecha de último acceso
                    usuario.UltimoAcceso = DateTime.Now;
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Usuario {Email} ha iniciado sesión", email);
                    return RedirectToAction("Index", "User");
                }
            }

            ModelState.AddModelError("", "Correo o contraseña incorrectos");
            return View();
        }

        // GET: /User/Register
        public IActionResult Register()
        {
            // Si ya está autenticado, redirige a Index
            if (HttpContext.Session.GetInt32("UsuarioId") != null)
            {
                return RedirectToAction("Index");
            }
            return View();
        }

        // POST: /User/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(Usuario usuario, string confirmarPassword)
        {
            // Validación del modelo
            if (!ModelState.IsValid)
            {
                return View(usuario);
            }

            // Validación de correo ya existente
            if (await _context.Usuarios.AnyAsync(u => u.Email == usuario.Email))
            {
                ModelState.AddModelError("Email", "Este correo ya está registrado");
                return View(usuario);
            }

            // Validación de contraseña
            if (string.IsNullOrEmpty(usuario.Password) || usuario.Password.Length < 8)
            {
                ModelState.AddModelError("Password", "La contraseña debe tener al menos 8 caracteres");
                return View(usuario);
            }

            // Verificar que la contraseña tenga letras y números
            if (!Regex.IsMatch(usuario.Password, @"^(?=.*[A-Za-z])(?=.*\d).+$"))
            {
                ModelState.AddModelError("Password", "La contraseña debe contener letras y números");
                return View(usuario);
            }

            // Verificar que las contraseñas coincidan
            if (usuario.Password != confirmarPassword)
            {
                ModelState.AddModelError("", "Las contraseñas no coinciden");
                return View(usuario);
            }

            // Encriptar la contraseña
            usuario.Password = ConvertirSha256(usuario.Password);
            usuario.FechaRegistro = DateTime.Now;
            usuario.Activo = true;

            // Guardar el usuario
            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Nuevo usuario registrado: {Email}", usuario.Email);

            return RedirectToAction("Login");
        }

        // POST: /User/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        // Método para encriptar contraseñas con SHA-256
        public static string ConvertirSha256(string texto)
        {
            StringBuilder sb = new StringBuilder();
            using (SHA256 hash = SHA256.Create())
            {
                Encoding enc = Encoding.UTF8;
                byte[] result = hash.ComputeHash(enc.GetBytes(texto));

                foreach (byte b in result)
                    sb.Append(b.ToString("x2"));
            }

            return sb.ToString();
        }
    }
}