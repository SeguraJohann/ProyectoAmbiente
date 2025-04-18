using System;

namespace ProyectoAmbiente.Models
{
    public class ProgresoMecanografia
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }
        public int DocumentoId { get; set; }
        public int PaginaActual { get; set; }

        // Posición específica
        public int IndiceCaracter { get; set; }
        public float PosicionX { get; set; }
        public float PosicionY { get; set; }
        public string ElementoId { get; set; } // Identificador del elemento de texto

        // Contexto para desambiguación
        public string FragmentoContexto { get; set; } // Segmento de texto alrededor para verificar posición

        public DateTime UltimaActualizacion { get; set; }
        public double PorcentajeCompletado { get; set; }
        public string TextoCompletado { get; set; } // Opcional: texto ya mecanografiado

        // Relaciones de navegación
        public virtual Usuario Usuario { get; set; }
        public virtual DocumentoPDF Documento { get; set; }
    }
}
