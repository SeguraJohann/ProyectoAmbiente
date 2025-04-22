
using System;

namespace ProyectoAmbiente.Models
{
    //Entidad de las estadisticas, el proyecto final probablemente no las implemente.
    public class EstadisticasMecanografia
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }
        public int DocumentoId { get; set; }
        public DateTime FechaSesion { get; set; }
        public double DuracionMinutos { get; set; }
        public int WPM { get; set; } // Palabras por minuto
        public int PaginaInicio { get; set; }
        public int PaginaFin { get; set; }

        // Relaciones de navegación
        public virtual Usuario Usuario { get; set; }
        public virtual DocumentoPDF Documento { get; set; }
    }
}
