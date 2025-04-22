using System;
using System.Collections.Generic;

namespace ProyectoAmbiente.Models
{
    //se crea la entidad DocumentoPDF, conteniendo cada uno de los atributos para su utilización
    public class DocumentoPDF
    {

        public int Id { get; set; }
        public int UsuarioId { get; set; }
        public string Nombre { get; set; }
        public string Ruta { get; set; }
        public DateTime FechaSubida { get; set; }
        public long TamañoBytes { get; set; }
        public int NumPaginas { get; set; }
        public int PaginaActual {  get; set; }
        public String NombreGuardado { get; set; }
        public string EstructuraJSON { get; set; } // Almacena elementos de texto con sus coordenadas
        // Relaciones de navegación
        public virtual Usuario Usuario { get; set; }
        public virtual ICollection<ProgresoMecanografia> Progresos { get; set; }
        public virtual ICollection<EstadisticasMecanografia> Estadisticas { get; set; }
    }
}
