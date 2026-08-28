using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SinnersRelatos.Web.Migrations
{
    /// <inheritdoc />
    public partial class AgregarRecetaOpcionModificador : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RecetasOpcionModificador",
                columns: table => new
                {
                    OpcionModificadorId = table.Column<int>(type: "int", nullable: false),
                    IngredienteId = table.Column<int>(type: "int", nullable: false),
                    CantidadRequerida = table.Column<decimal>(type: "decimal(10,3)", precision: 10, scale: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecetasOpcionModificador", x => new { x.OpcionModificadorId, x.IngredienteId });
                    table.ForeignKey(
                        name: "FK_RecetasOpcionModificador_Ingredientes_IngredienteId",
                        column: x => x.IngredienteId,
                        principalTable: "Ingredientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RecetasOpcionModificador_OpcionesModificadores_OpcionModificadorId",
                        column: x => x.OpcionModificadorId,
                        principalTable: "OpcionesModificadores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RecetasOpcionModificador_IngredienteId",
                table: "RecetasOpcionModificador",
                column: "IngredienteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RecetasOpcionModificador");
        }
    }
}
