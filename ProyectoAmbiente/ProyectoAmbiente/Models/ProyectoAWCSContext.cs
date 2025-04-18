using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace ProyectoAmbiente.Models
{
    public class ProyectoAWCSContext : DbContext
    {
        public ProyectoAWCSContext(DbContextOptions<ProyectoAWCSContext> options) : base(options) { }

        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<DocumentoPDF> DocumentosPDF { get; set; }
        public DbSet<ProgresoMecanografia> ProgresosMecanografia { get; set; }
        public DbSet<EstadisticasMecanografia> EstadisticasMecanografia { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configuración Usuario
            modelBuilder.Entity<Usuario>(usuario =>
            {
                usuario.HasKey(e => e.Id);
                usuario.Property(e => e.Nombre).IsRequired().HasMaxLength(100);
                usuario.Property(e => e.Email).IsRequired().HasMaxLength(200);
                usuario.Property(e => e.Password).IsRequired().HasMaxLength(200);
                usuario.Property(e => e.FechaRegistro).IsRequired();
                usuario.Property(e => e.Activo).IsRequired();
            });

            // Configuración DocumentoPDF
            modelBuilder.Entity<DocumentoPDF>(documento =>
            {
                documento.HasKey(e => e.Id);
                documento.Property(e => e.Nombre).IsRequired().HasMaxLength(200);
                documento.Property(e => e.Ruta).IsRequired().HasMaxLength(500);
                documento.Property(e => e.FechaSubida).IsRequired();
                documento.Property(e => e.NumPaginas).IsRequired();
                documento.Property(e => e.EstructuraJSON).HasColumnType("nvarchar(max)");
            });

            // Configuración ProgresoMecanografia
            modelBuilder.Entity<ProgresoMecanografia>(progreso =>
            {
                progreso.HasKey(e => e.Id);
                progreso.Property(e => e.PaginaActual).IsRequired();
                progreso.Property(e => e.IndiceCaracter).IsRequired();
                progreso.Property(e => e.PosicionX).IsRequired();
                progreso.Property(e => e.PosicionY).IsRequired();
                progreso.Property(e => e.ElementoId).HasMaxLength(100);
                progreso.Property(e => e.FragmentoContexto).HasMaxLength(500);
                progreso.Property(e => e.UltimaActualizacion).IsRequired();
                progreso.Property(e => e.PorcentajeCompletado).IsRequired();
                progreso.Property(e => e.TextoCompletado).HasColumnType("nvarchar(max)");
            });

            // Configuración EstadisticasMecanografia
            modelBuilder.Entity<EstadisticasMecanografia>(estadisticas =>
            {
                estadisticas.HasKey(e => e.Id);
                estadisticas.Property(e => e.FechaSesion).IsRequired();
                estadisticas.Property(e => e.DuracionMinutos).IsRequired();
                estadisticas.Property(e => e.WPM).IsRequired();
                estadisticas.Property(e => e.PaginaInicio).IsRequired();
                estadisticas.Property(e => e.PaginaFin).IsRequired();
            });

            // Relaciones

            // Usuario - DocumentoPDF (one-to-many)
            modelBuilder.Entity<DocumentoPDF>()
                .HasOne(d => d.Usuario)
                .WithMany(u => u.Documentos)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);

            // Usuario - ProgresoMecanografia (one-to-many)
            modelBuilder.Entity<ProgresoMecanografia>()
                .HasOne(p => p.Usuario)
                .WithMany(u => u.Progresos)
                .HasForeignKey(p => p.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);

            // DocumentoPDF - ProgresoMecanografia (one-to-many)
            modelBuilder.Entity<ProgresoMecanografia>()
                .HasOne(p => p.Documento)
                .WithMany(d => d.Progresos)
                .HasForeignKey(p => p.DocumentoId)
                .OnDelete(DeleteBehavior.Restrict);

            // Usuario - EstadisticasMecanografia (one-to-many)
            modelBuilder.Entity<EstadisticasMecanografia>()
                .HasOne(e => e.Usuario)
                .WithMany(u => u.Estadisticas)
                .HasForeignKey(e => e.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);

            // DocumentoPDF - EstadisticasMecanografia (one-to-many)
            modelBuilder.Entity<EstadisticasMecanografia>()
                .HasOne(e => e.Documento)
                .WithMany(d => d.Estadisticas)
                .HasForeignKey(e => e.DocumentoId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
