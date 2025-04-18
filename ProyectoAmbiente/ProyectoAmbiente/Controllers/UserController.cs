using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using ProyectoAmbiente.Models;
using System.IO;

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
            if (HttpContext.Session.GetInt32("UsuarioId") == null)
            {
                return RedirectToAction("Login");
            }

            // Obtiene el ID del usuario desde la sesión
            int usuarioId = HttpContext.Session.GetInt32("UsuarioId").Value;

            // Carga los documentos PDF del usuario
            var documentos = await _context.DocumentosPDF
                .Where(d => d.UsuarioId == usuarioId)
                .OrderByDescending(d => d.FechaSubida)
                .ToListAsync();

            return View(documentos);
        }

        // POST: /User/SubirPDF
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubirPDF(List<IFormFile> archivos)
        {
            // Verifica si el usuario está autenticado
            if (HttpContext.Session.GetInt32("UsuarioId") == null)
            {
                return RedirectToAction("Login");
            }

            // Obtiene el ID del usuario desde la sesión
            int usuarioId = HttpContext.Session.GetInt32("UsuarioId").Value;

            if (archivos == null || archivos.Count == 0)
            {
                TempData["Error"] = "No se ha seleccionado ningún archivo.";
                return RedirectToAction("Index");
            }

            // Validaciones y procesamiento de cada archivo
            int contadorExito = 0;
            int contadorError = 0;

            foreach (var archivo in archivos)
            {
                if (archivo.Length > 0)
                {
                    // Verificar extensión
                    string extension = Path.GetExtension(archivo.FileName).ToLower();
                    if (extension != ".pdf")
                    {
                        contadorError++;
                        continue;
                    }

                    // Verificar tamaño (25MB máximo)
                    if (archivo.Length > 25 * 1024 * 1024)
                    {
                        contadorError++;
                        continue;
                    }

                    try
                    {
                        // Crear directorio si no existe
                        string rutaDirectorio = Path.Combine("D:", "U", "U", "Ambiente web Cliente Servidor",
                            "Proyecto", "ProyectoAmbiente", "ProyectoAmbiente", "ProyectoAmbiente", "PDFS");

                        if (!Directory.Exists(rutaDirectorio))
                        {
                            Directory.CreateDirectory(rutaDirectorio);
                        }

                        // Generar nombre único para el archivo
                        string nombreOriginal = Path.GetFileNameWithoutExtension(archivo.FileName);
                        // Sanitizar el nombre del archivo (eliminar caracteres no válidos)
                        nombreOriginal = Regex.Replace(nombreOriginal, @"[^\w\-]", "_");

                        string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                        string nombreArchivo = $"{usuarioId}_{timestamp}_{nombreOriginal}.pdf";
                        string rutaCompleta = Path.Combine(rutaDirectorio, nombreArchivo);

                        // Guardar el archivo en el sistema de archivos
                        using (var stream = new FileStream(rutaCompleta, FileMode.Create))
                        {
                            await archivo.CopyToAsync(stream);
                        }

                        // Obtener número de páginas (implementación básica, se puede mejorar)
                        int numPaginas = 1; // Placeholder, en un sistema real se leería del PDF

                        // Crear registro en la base de datos
                        var documentoPDF = new DocumentoPDF
                        {
                            UsuarioId = usuarioId,
                            Nombre = Path.GetFileName(archivo.FileName),
                            Ruta = rutaCompleta,
                            FechaSubida = DateTime.Now,
                            TamañoBytes = archivo.Length,
                            NumPaginas = numPaginas,
                            EstructuraJSON = "{}" // Placeholder, en un sistema real se generaría
                        };

                        _context.DocumentosPDF.Add(documentoPDF);
                        await _context.SaveChangesAsync();

                        contadorExito++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error al subir archivo PDF");
                        contadorError++;
                    }
                }
            }

            // Mensaje de resultado
            if (contadorExito > 0)
            {
                TempData["Success"] = $"Se han subido {contadorExito} archivos correctamente.";
            }
            if (contadorError > 0)
            {
                TempData["Warning"] = $"No se pudieron subir {contadorError} archivos.";
            }

            return RedirectToAction("Index");
        }

        // POST: /User/EliminarPDF
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarPDF(int id)
        {
            // Verifica si el usuario está autenticado
            if (HttpContext.Session.GetInt32("UsuarioId") == null)
            {
                return RedirectToAction("Login");
            }

            // Obtiene el ID del usuario desde la sesión
            int usuarioId = HttpContext.Session.GetInt32("UsuarioId").Value;

            // Busca el documento en la base de datos
            var documento = await _context.DocumentosPDF
                .FirstOrDefaultAsync(d => d.Id == id && d.UsuarioId == usuarioId);

            if (documento == null)
            {
                TempData["Error"] = "El archivo no existe o no tienes permisos para eliminarlo.";
                return RedirectToAction("Index");
            }

            try
            {
                // Eliminar el archivo físico
                if (System.IO.File.Exists(documento.Ruta))
                {
                    System.IO.File.Delete(documento.Ruta);
                }

                // Eliminar registro de la base de datos
                _context.DocumentosPDF.Remove(documento);
                await _context.SaveChangesAsync();

                TempData["Success"] = "El archivo se ha eliminado correctamente.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar archivo PDF");
                TempData["Error"] = "Ha ocurrido un error al eliminar el archivo.";
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