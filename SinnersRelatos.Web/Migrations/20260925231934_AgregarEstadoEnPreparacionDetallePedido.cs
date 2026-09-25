using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SinnersRelatos.Web.Migrations
{
    /// <inheritdoc />
    public partial class AgregarEstadoEnPreparacionDetallePedido : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FechaCambioEstado",
                table: "DetallesPedido",
                type: "datetime2",
                nullable: true);

            // El nuevo estado intermedio "EnPreparacion" (1) se insertó entre los dos valores
            // existentes, así que "Listo" pasa de valer 1 a valer 2. "Recibido" (antes
            // "Pendiente") se queda en 0, no necesita remapeo.
            migrationBuilder.Sql("UPDATE DetallesPedido SET Estado = 2 WHERE Estado = 1;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE DetallesPedido SET Estado = 1 WHERE Estado = 2;");

            migrationBuilder.DropColumn(
                name: "FechaCambioEstado",
                table: "DetallesPedido");
        }
    }
}
