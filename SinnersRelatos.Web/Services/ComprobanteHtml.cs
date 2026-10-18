using System.Net;
using System.Text;
using SinnersRelatos.Web.Models;
using SinnersRelatos.Web.Services.Sunat;

namespace SinnersRelatos.Web.Services;

// Ticket de 80 mm. Sin comprobante electrónico es la precuenta ("Resumen de consumo"); con uno, es
// su representación impresa: datos del emisor, serie-número, IGV desglosado y el valor resumen.
public static class ComprobanteHtml
{
    public static string Construir(Pedido pedido, bool autoprint, Comprobante? comprobante = null, SunatOptions? emisor = null)
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

        // La representación impresa debe coincidir con lo informado a SUNAT: una sola línea.
        if (comprobante is { PorConsumo: true })
            filas.Clear().Append($"<tr><td class=\"cant\">1x</td><td class=\"nombre\">Consumo de alimentos y bebidas</td><td class=\"precio\">S/ {comprobante.Total:0.00}</td></tr>");

        var script = autoprint ? "<script>window.onload = function () { window.print(); };</script>" : "";
        var botonManual = autoprint ? "" : "<button class=\"no-print\" onclick=\"window.print()\">Imprimir / Guardar PDF</button>";

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
            .nota { font-size: 0.68rem; color: #777; text-align: center; margin-top: 1rem; overflow-wrap: anywhere; }
            .tipo-comprobante { text-align: center; font-weight: 700; font-size: 0.9rem; margin: 0.6rem 0 0; }
            .datos .cliente { text-align: right; margin-left: 1rem; }
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
                    {Encabezado(comprobante, emisor)}
                    <div class="linea"></div>
                    <div class="datos">
                        <div><span>Fecha</span><span>{(comprobante?.FechaEmision ?? pedido.FechaCreacion):dd/MM/yyyy HH:mm}</span></div>
                        {DatosCliente(comprobante)}
                        <div><span>Mesa</span><span>{Enc(mesaEtiqueta)}</span></div>
                        <div><span>Atendido por</span><span>{Enc(pedido.Usuario.NombreUsuario)}</span></div>
                    </div>
                    <div class="linea"></div>
                    <table><tbody>{filas}</tbody></table>
                    <div class="linea"></div>
                    {Totales(comprobante, total)}
                    {Pie(comprobante)}
                </div>
                {script}
            </body>
            </html>
            """;
    }

    private static string Encabezado(Comprobante? c, SunatOptions? emisor)
    {
        if (c is null || emisor is null)
            return "<h1>Sinners &amp; Relatos</h1><p class=\"subtitulo\">Resumen de Consumo</p>";

        return $"""
            <h1>{Enc(emisor.NombreComercial)}</h1>
            <p class="subtitulo">{Enc(emisor.RazonSocial)}<br />RUC {Enc(emisor.Ruc)}<br />{Enc(emisor.Direccion)}</p>
            <p class="tipo-comprobante">{NombreTipo(c.Tipo).ToUpperInvariant()}<br />{Enc(c.Serie)}-{c.Numero:D8}</p>
            {(c.Estado == EstadoComprobante.Anulado ? "<p class=\"tipo-comprobante\">*** ANULADO ***</p>" : "")}
            """;
    }

    private static string DatosCliente(Comprobante? c)
    {
        if (c is null || c.ClienteNumeroDocumento == "-")
            return "";

        var tipoDocumento = c.ClienteTipoDocumento switch { "1" => "DNI", "6" => "RUC", _ => "Doc." };
        return $"<div><span>{tipoDocumento}</span><span>{Enc(c.ClienteNumeroDocumento)}</span></div>" +
               $"<div><span>Cliente</span><span class=\"cliente\">{Enc(c.ClienteNombre)}</span></div>" +
               (c.ClienteDireccion is null ? "" : $"<div><span>Dirección</span><span class=\"cliente\">{Enc(c.ClienteDireccion)}</span></div>");
    }

    private static string Totales(Comprobante? c, decimal total)
    {
        if (c is null)
            return $"<div class=\"total\"><span>Total</span><span>S/ {total:0.00}</span></div>";

        return $"""
            <div class="datos">
                <div><span>Op. gravada</span><span>S/ {c.OperacionesGravadas:0.00}</span></div>
                <div><span>IGV ({c.TasaIgv:0.##}%)</span><span>S/ {c.Igv:0.00}</span></div>
            </div>
            <div class="total"><span>Total</span><span>S/ {c.Total:0.00}</span></div>
            """;
    }

    private static string Pie(Comprobante? c)
    {
        if (c is null)
            return "<p class=\"nota\">Este resumen es un registro interno de consumo y no reemplaza su comprobante de pago oficial.</p>";

        return $"<p class=\"nota\">Representación impresa de la {NombreTipo(c.Tipo)}." +
               (c.Hash is null ? "" : $"<br />Valor resumen: {Enc(c.Hash)}") + "</p>";
    }

    private static string NombreTipo(TipoComprobante tipo) =>
        tipo == TipoComprobante.Factura ? "Factura Electrónica" : "Boleta de Venta Electrónica";

    private static string Enc(string texto) => WebUtility.HtmlEncode(texto);
}
