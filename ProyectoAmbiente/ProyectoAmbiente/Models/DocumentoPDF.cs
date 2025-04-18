using System;
using System.Collections.Generic;

namespace ProyectoAmbiente.Models
{
    public class DocumentoPDF
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }
        public string Nombre { get; set; }
        public string Ruta { get; set; }
        public DateTime FechaSubida { get; set; }
        public long TamañoBytes { get; set; }
        public int NumPaginas { get; set; }
        public string EstructuraJSON { get; set; } // Almacena elementos de texto con sus coordenadas

        // Relaciones de navegación
        public virtual Usuario Usuario { get; set; }
        public virtual ICollection<ProgresoMecanografia> Progresos { get; set; }
        public virtual ICollection<EstadisticasMecanografia> Estadisticas { get; set; }
    }
}
