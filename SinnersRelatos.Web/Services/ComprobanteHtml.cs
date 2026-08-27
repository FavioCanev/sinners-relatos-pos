using System.Net;
using System.Text;
using SinnersRelatos.Web.Models;

namespace SinnersRelatos.Web.Services;

public static class ComprobanteHtml
{
    public static string Construir(Pedido pedido, bool autoprint)
    {
        var mesaEtiqueta = string.Join(" + ", pedido.Mesas.Select(pm =>
            $"{(pm.Mesa.Tipo == TipoMesa.Barra ? "Barra" : "Mesa")} {pm.Mesa.Numero}"));

        var filas = new StringBuilder();
        decimal total = 0m;

        foreach (var detalle in pedido.Detalles)
        {
            var subtotal = detalle.Cantidad * (detalle.PrecioUnitario + detalle.Modificadores.Sum(m => m.PrecioAdicional));
            total += subtotal;

            var mods = detalle.Modificadores.Count > 0
                ? $"<div class=\"mods\">{Enc(string.Join(", ", detalle.Modificadores.Select(m => m.OpcionModificador.Nombre)))}</div>"
                : "";

            filas.Append(
                $"<tr><td class=\"cant\">{detalle.Cantidad}x</td>" +
                $"<td class=\"nombre\">{Enc(detalle.Producto.Nombre)}{mods}</td>" +
                $"<td class=\"precio\">S/ {subtotal:0.00}</td></tr>");
        }

        var script = autoprint ? "<script>window.onload = function () { window.print(); };</script>" : "";
        var botonManual = autoprint ? "" : "<button class=\"no-print\" onclick=\"window.print()\">🖨️ Imprimir / Guardar PDF</button>";

        const string estilos = """
            body { font-family: 'Courier New', monospace; margin: 0; padding: 16px; color: #111; background: #fff; }
            .comprobante { max-width: 320px; margin: 0 auto; }
            h1 { font-size: 1.1rem; text-align: center; margin: 0; }
            .subtitulo { text-align: center; font-size: 0.8rem; margin: 0.2rem 0 0; color: #555; }
            .linea { border-top: 1px dashed #999; margin: 0.7rem 0; }
            .datos div { display: flex; justify-content: space-between; font-size: 0.82rem; margin-bottom: 0.15rem; }
            table { width: 100%; border-collapse: collapse; font-size: 0.82rem; }
            td { vertical-align: top; padding: 0.25rem 0; }
            td.cant { width: 2.2rem; }
            td.precio { text-align: right; white-space: nowrap; }
            .mods { font-size: 0.72rem; color: #555; }
            .total { display: flex; justify-content: space-between; font-weight: 700; font-size: 1rem; }
            .nota { font-size: 0.68rem; color: #777; text-align: center; margin-top: 1rem; }
            .no-print { display: block; margin: 0 auto 1rem; padding: 0.5rem 1rem; }
            @media print {
                .no-print { display: none !important; }
                body { padding: 0; }
                @page { size: 80mm auto; margin: 4mm; }
            }
            """;

        return $"""
            <!DOCTYPE html>
            <html lang="es">
            <head>
            <meta charset="utf-8" />
            <title>Comprobante</title>
            <style>{estilos}</style>
            </head>
            <body>
                {botonManual}
                <div class="comprobante">
                    <h1>Sinners &amp; Relatos</h1>
                    <p class="subtitulo">Resumen de Consumo</p>
                    <div class="linea"></div>
                    <div class="datos">
                        <div><span>Fecha</span><span>{pedido.FechaCreacion:dd/MM/yyyy HH:mm}</span></div>
                        <div><span>Mesa</span><span>{Enc(mesaEtiqueta)}</span></div>
                        <div><span>Atendido por</span><span>{Enc(pedido.Usuario.NombreUsuario)}</span></div>
                    </div>
                    <div class="linea"></div>
                    <table><tbody>{filas}</tbody></table>
                    <div class="linea"></div>
                    <div class="total"><span>Total</span><span>S/ {total:0.00}</span></div>
                    <p class="nota">Este resumen es un registro interno de consumo y no reemplaza su comprobante de pago oficial.</p>
                </div>
                {script}
            </body>
            </html>
            """;
    }

    private static string Enc(string texto) => WebUtility.HtmlEncode(texto);
}
