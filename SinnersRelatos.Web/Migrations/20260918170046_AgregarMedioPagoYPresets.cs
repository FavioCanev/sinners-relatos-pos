using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SinnersRelatos.Web.Migrations
{
    /// <inheritdoc />
    public partial class AgregarMedioPagoYPresets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MedioPago",
                table: "Pedidos",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Presets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Marca = table.Column<int>(type: "int", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Presets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PresetProductos",
                columns: table => new
                {
                    PresetId = table.Column<int>(type: "int", nullable: false),
                    ProductoId = table.Column<int>(type: "int", nullable: false),
                    Cantidad = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PresetProductos", x => new { x.PresetId, x.ProductoId });
                    table.ForeignKey(
                        name: "FK_PresetProductos_Presets_PresetId",
                        column: x => x.PresetId,
                        principalTable: "Presets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PresetProductos_Productos_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Productos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PresetProductos_ProductoId",
                table: "PresetProductos",
                column: "ProductoId");

            migrationBuilder.CreateIndex(
                name: "IX_Presets_Marca_Nombre",
                table: "Presets",
                columns: new[] { "Marca", "Nombre" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PresetProductos");

            migrationBuilder.DropTable(
                name: "Presets");

            migrationBuilder.DropColumn(
                name: "MedioPago",
                table: "Pedidos");
        }
    }
}
