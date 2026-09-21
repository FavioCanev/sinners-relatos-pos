using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SinnersRelatos.Web.Migrations
{
    /// <inheritdoc />
    public partial class QuitarMarcaDePresets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Presets_Marca_Nombre",
                table: "Presets");

            migrationBuilder.DropColumn(
                name: "Marca",
                table: "Presets");

            migrationBuilder.CreateIndex(
                name: "IX_Presets_Nombre",
                table: "Presets",
                column: "Nombre",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Presets_Nombre",
                table: "Presets");

            migrationBuilder.AddColumn<int>(
                name: "Marca",
                table: "Presets",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Presets_Marca_Nombre",
                table: "Presets",
                columns: new[] { "Marca", "Nombre" },
                unique: true);
        }
    }
}
