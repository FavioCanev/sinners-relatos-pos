using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SinnersRelatos.Web.Migrations
{
    /// <inheritdoc />
    public partial class AgregarProductoARecetaOpcionModificador : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_RecetasOpcionModificador",
                table: "RecetasOpcionModificador");

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "RecetasOpcionModificador",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<int>(
                name: "ProductoId",
                table: "RecetasOpcionModificador",
                type: "int",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_RecetasOpcionModificador",
                table: "RecetasOpcionModificador",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_RecetasOpcionModificador_OpcionModificadorId_IngredienteId",
                table: "RecetasOpcionModificador",
                columns: new[] { "OpcionModificadorId", "IngredienteId" },
                unique: true,
                filter: "[ProductoId] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RecetasOpcionModificador_OpcionModificadorId_IngredienteId_ProductoId",
                table: "RecetasOpcionModificador",
                columns: new[] { "OpcionModificadorId", "IngredienteId", "ProductoId" },
                unique: true,
                filter: "[ProductoId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RecetasOpcionModificador_ProductoId",
                table: "RecetasOpcionModificador",
                column: "ProductoId");

            migrationBuilder.AddForeignKey(
                name: "FK_RecetasOpcionModificador_Productos_ProductoId",
                table: "RecetasOpcionModificador",
                column: "ProductoId",
                principalTable: "Productos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RecetasOpcionModificador_Productos_ProductoId",
                table: "RecetasOpcionModificador");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RecetasOpcionModificador",
                table: "RecetasOpcionModificador");

            migrationBuilder.DropIndex(
                name: "IX_RecetasOpcionModificador_OpcionModificadorId_IngredienteId",
                table: "RecetasOpcionModificador");

            migrationBuilder.DropIndex(
                name: "IX_RecetasOpcionModificador_OpcionModificadorId_IngredienteId_ProductoId",
                table: "RecetasOpcionModificador");

            migrationBuilder.DropIndex(
                name: "IX_RecetasOpcionModificador_ProductoId",
                table: "RecetasOpcionModificador");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "RecetasOpcionModificador");

            migrationBuilder.DropColumn(
                name: "ProductoId",
                table: "RecetasOpcionModificador");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RecetasOpcionModificador",
                table: "RecetasOpcionModificador",
                columns: new[] { "OpcionModificadorId", "IngredienteId" });
        }
    }
}
