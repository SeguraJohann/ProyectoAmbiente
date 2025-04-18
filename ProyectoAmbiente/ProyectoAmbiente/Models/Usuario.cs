using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ProyectoAmbiente.Models
{
    public class Usuario
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres")]
        public string Nombre { get; set; }

        [Required(ErrorMessage = "El correo electrónico es obligatorio")]
        [EmailAddress(ErrorMessage = "El formato del correo electrónico no es válido")]
        [StringLength(200, ErrorMessage = "El correo no puede superar los 200 caracteres")]
        public string Email { get; set; }

        [Required(ErrorMessage = "La contraseña es obligatoria")]
        [StringLength(200, MinimumLength = 8, ErrorMessage = "La contraseña debe tener entre 8 y 200 caracteres")]
        [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d).+$", ErrorMessage = "La contraseña debe contener al menos una letra y un número")]
        public string Password { get; set; }

        public DateTime FechaRegistro { get; set; }
        public bool Activo { get; set; }
        public DateTime? UltimoAcceso { get; set; }

        // Relaciones de navegación
        public virtual ICollection<DocumentoPDF>? Documentos { get; set; }
        public virtual ICollection<ProgresoMecanografia>? Progresos { get; set; }
        public virtual ICollection<EstadisticasMecanografia>? Estadisticas { get; set; }
    }
}