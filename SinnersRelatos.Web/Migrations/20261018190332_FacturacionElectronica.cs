using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SinnersRelatos.Web.Migrations
{
    /// <inheritdoc />
    public partial class FacturacionElectronica : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Comprobantes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Tipo = table.Column<int>(type: "INTEGER", nullable: false),
                    Serie = table.Column<string>(type: "TEXT", nullable: false),
                    Numero = table.Column<int>(type: "INTEGER", nullable: false),
                    FechaEmision = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ClienteTipoDocumento = table.Column<string>(type: "TEXT", nullable: false),
                    ClienteNumeroDocumento = table.Column<string>(type: "TEXT", nullable: false),
                    ClienteNombre = table.Column<string>(type: "TEXT", nullable: false),
                    ClienteDireccion = table.Column<string>(type: "TEXT", nullable: true),
                    PorConsumo = table.Column<bool>(type: "INTEGER", nullable: false),
                    TasaIgv = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: false),
                    OperacionesGravadas = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: false),
                    Igv = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: false),
                    Total = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: false),
                    Estado = table.Column<int>(type: "INTEGER", nullable: false),
                    XmlFirmado = table.Column<string>(type: "TEXT", nullable: true),
                    Hash = table.Column<string>(type: "TEXT", nullable: true),
                    CodigoRespuesta = table.Column<string>(type: "TEXT", nullable: true),
                    MensajeRespuesta = table.Column<string>(type: "TEXT", nullable: true),
                    Cdr = table.Column<byte[]>(type: "BLOB", nullable: true),
                    MotivoAnulacion = table.Column<string>(type: "TEXT", nullable: true),
                    PedidoId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comprobantes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Comprobantes_Pedidos_PedidoId",
                        column: x => x.PedidoId,
                        principalTable: "Pedidos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EnviosSunat",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Tipo = table.Column<int>(type: "INTEGER", nullable: false),
                    Identificador = table.Column<string>(type: "TEXT", nullable: false),
                    FechaGeneracion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaComprobantes = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Ticket = table.Column<string>(type: "TEXT", nullable: true),
                    Estado = table.Column<int>(type: "INTEGER", nullable: false),
                    CodigoRespuesta = table.Column<string>(type: "TEXT", nullable: true),
                    MensajeRespuesta = table.Column<string>(type: "TEXT", nullable: true),
                    Cdr = table.Column<byte[]>(type: "BLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnviosSunat", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EnviosSunatDetalle",
                columns: table => new
                {
                    EnvioSunatId = table.Column<int>(type: "INTEGER", nullable: false),
                    ComprobanteId = table.Column<int>(type: "INTEGER", nullable: false),
                    EsAnulacion = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnviosSunatDetalle", x => new { x.EnvioSunatId, x.ComprobanteId });
                    table.ForeignKey(
                        name: "FK_EnviosSunatDetalle_Comprobantes_ComprobanteId",
                        column: x => x.ComprobanteId,
                        principalTable: "Comprobantes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EnviosSunatDetalle_EnviosSunat_EnvioSunatId",
                        column: x => x.EnvioSunatId,
                        principalTable: "EnviosSunat",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Comprobantes_Estado",
                table: "Comprobantes",
                column: "Estado");

            migrationBuilder.CreateIndex(
                name: "IX_Comprobantes_FechaEmision",
                table: "Comprobantes",
                column: "FechaEmision");

            migrationBuilder.CreateIndex(
                name: "IX_Comprobantes_PedidoId",
                table: "Comprobantes",
                column: "PedidoId");

            migrationBuilder.CreateIndex(
                name: "IX_Comprobantes_Serie_Numero",
                table: "Comprobantes",
                columns: new[] { "Serie", "Numero" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EnviosSunat_Estado",
                table: "EnviosSunat",
                column: "Estado");

            migrationBuilder.CreateIndex(
                name: "IX_EnviosSunat_Identificador",
                table: "EnviosSunat",
                column: "Identificador",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EnviosSunatDetalle_ComprobanteId",
                table: "EnviosSunatDetalle",
                column: "ComprobanteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EnviosSunatDetalle");

            migrationBuilder.DropTable(
                name: "Comprobantes");

            migrationBuilder.DropTable(
                name: "EnviosSunat");
        }
    }
}
