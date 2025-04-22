using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

//el Framwork presenta herramientas de migración, permitiendo traer bases de datos y sus registros con comandos.
namespace ProyectoAmbiente.Migrations
{
    /// <inheritdoc />
    public partial class MigracionInicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Password = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FechaRegistro = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    UltimoAcceso = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DocumentosPDF",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Ruta = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FechaSubida = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TamañoBytes = table.Column<long>(type: "bigint", nullable: false),
                    NumPaginas = table.Column<int>(type: "int", nullable: false),
                    EstructuraJSON = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentosPDF", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentosPDF_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EstadisticasMecanografia",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    DocumentoId = table.Column<int>(type: "int", nullable: false),
                    FechaSesion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DuracionMinutos = table.Column<double>(type: "float", nullable: false),
                    WPM = table.Column<int>(type: "int", nullable: false),
                    PaginaInicio = table.Column<int>(type: "int", nullable: false),
                    PaginaFin = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EstadisticasMecanografia", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EstadisticasMecanografia_DocumentosPDF_DocumentoId",
                        column: x => x.DocumentoId,
                        principalTable: "DocumentosPDF",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EstadisticasMecanografia_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProgresosMecanografia",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    DocumentoId = table.Column<int>(type: "int", nullable: false),
                    PaginaActual = table.Column<int>(type: "int", nullable: false),
                    IndiceCaracter = table.Column<int>(type: "int", nullable: false),
                    PosicionX = table.Column<float>(type: "real", nullable: false),
                    PosicionY = table.Column<float>(type: "real", nullable: false),
                    ElementoId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FragmentoContexto = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    UltimaActualizacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PorcentajeCompletado = table.Column<double>(type: "float", nullable: false),
                    TextoCompletado = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgresosMecanografia", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProgresosMecanografia_DocumentosPDF_DocumentoId",
                        column: x => x.DocumentoId,
                        principalTable: "DocumentosPDF",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProgresosMecanografia_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentosPDF_UsuarioId",
                table: "DocumentosPDF",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_EstadisticasMecanografia_DocumentoId",
                table: "EstadisticasMecanografia",
                column: "DocumentoId");

            migrationBuilder.CreateIndex(
                name: "IX_EstadisticasMecanografia_UsuarioId",
                table: "EstadisticasMecanografia",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_ProgresosMecanografia_DocumentoId",
                table: "ProgresosMecanografia",
                column: "DocumentoId");

            migrationBuilder.CreateIndex(
                name: "IX_ProgresosMecanografia_UsuarioId",
                table: "ProgresosMecanografia",
                column: "UsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EstadisticasMecanografia");

            migrationBuilder.DropTable(
                name: "ProgresosMecanografia");

            migrationBuilder.DropTable(
                name: "DocumentosPDF");

            migrationBuilder.DropTable(
                name: "Usuarios");
        }
    }
}
