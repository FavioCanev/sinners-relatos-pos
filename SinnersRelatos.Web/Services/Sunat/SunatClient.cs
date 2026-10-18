using System.IO.Compression;
using System.Net;
using System.Text;
using System.Xml.Linq;

namespace SinnersRelatos.Web.Services.Sunat;

// Respuesta ya interpretada de SUNAT.
//   Recibido = false: SUNAT no procesó el envío (error de conexión o excepción 0100-1999) y hay
//                     que reintentar; Codigo/Mensaje dicen por qué.
//   EnProceso = true: el ticket todavía no tiene resultado (consultar de nuevo más tarde).
//   Codigo: "0" aceptado, 2000-3999 rechazado, 4000+ aceptado con observaciones.
public record RespuestaSunat(bool Recibido, string? Codigo, string? Mensaje, byte[]? Cdr = null, string? Ticket = null, bool EnProceso = false)
{
    public bool Aceptado => Recibido && !EnProceso && (Codigo == "0" || EsObservacion);
    public bool EsObservacion => int.TryParse(Codigo, out var c) && c >= 4000;
}

// Cliente SOAP del servicio "billService" de SUNAT (Manual del Programador, SEE del Contribuyente).
public class SunatClient(HttpClient http, SunatOptions opciones)
{
    private static readonly XNamespace Soap = "http://schemas.xmlsoap.org/soap/envelope/";
    private static readonly XNamespace Cbc = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";
    private static readonly XNamespace Cac = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";

    // Facturas: SUNAT responde al instante con la constancia (CDR).
    public async Task<RespuestaSunat> EnviarComprobanteAsync(string nombreArchivo, string xml)
    {
        var respuesta = await LlamarAsync("sendBill",
            $"<fileName>{nombreArchivo}.zip</fileName><contentFile>{Convert.ToBase64String(Comprimir(nombreArchivo, xml))}</contentFile>");
        if (respuesta.Error is not null)
            return respuesta.Error;

        var cdr = Convert.FromBase64String(respuesta.Cuerpo!.Descendants("applicationResponse").Single().Value);
        return LeerCdr(cdr);
    }

    // Resumen diario y comunicación de baja: SUNAT devuelve un ticket para consultar luego.
    public async Task<RespuestaSunat> EnviarResumenAsync(string nombreArchivo, string xml)
    {
        var respuesta = await LlamarAsync("sendSummary",
            $"<fileName>{nombreArchivo}.zip</fileName><contentFile>{Convert.ToBase64String(Comprimir(nombreArchivo, xml))}</contentFile>");
        if (respuesta.Error is not null)
            return respuesta.Error;

        var ticket = respuesta.Cuerpo!.Descendants("ticket").Single().Value;
        return new RespuestaSunat(true, null, null, Ticket: ticket, EnProceso: true);
    }

    public async Task<RespuestaSunat> ConsultarTicketAsync(string ticket)
    {
        var respuesta = await LlamarAsync("getStatus", $"<ticket>{WebUtility.HtmlEncode(ticket)}</ticket>");
        if (respuesta.Error is not null)
            return respuesta.Error;

        var estado = respuesta.Cuerpo!.Descendants("statusCode").Single().Value;
        if (estado == "98")
            return new RespuestaSunat(true, null, "En proceso", Ticket: ticket, EnProceso: true);

        // 0 = procesó correctamente, 99 = procesó con errores; en ambos casos adjunta la constancia.
        var contenido = respuesta.Cuerpo.Descendants("content").SingleOrDefault()?.Value;
        return string.IsNullOrEmpty(contenido)
            ? new RespuestaSunat(true, estado, "SUNAT no devolvió constancia para el ticket.")
            : LeerCdr(Convert.FromBase64String(contenido)) with { Ticket = ticket };
    }

    private async Task<(XElement? Cuerpo, RespuestaSunat? Error)> LlamarAsync(string metodo, string parametros)
    {
        var usuario = WebUtility.HtmlEncode(opciones.Ruc + opciones.UsuarioSol);
        var clave = WebUtility.HtmlEncode(opciones.ClaveSol);
        var sobre = $"""
            <soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/" xmlns:ser="http://service.sunat.gob.pe" xmlns:wsse="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd">
            <soapenv:Header><wsse:Security><wsse:UsernameToken><wsse:Username>{usuario}</wsse:Username><wsse:Password>{clave}</wsse:Password></wsse:UsernameToken></wsse:Security></soapenv:Header>
            <soapenv:Body><ser:{metodo}>{parametros}</ser:{metodo}></soapenv:Body>
            </soapenv:Envelope>
            """;

        HttpStatusCode estadoHttp;
        string texto;
        try
        {
            using var solicitud = new HttpRequestMessage(HttpMethod.Post, opciones.UrlEnvio)
            {
                Content = new StringContent(sobre, Encoding.UTF8, "text/xml")
            };
            solicitud.Headers.Add("SOAPAction", "\"urn:" + metodo + "\"");
            using var respuestaHttp = await http.SendAsync(solicitud);
            estadoHttp = respuestaHttp.StatusCode;
            texto = await respuestaHttp.Content.ReadAsStringAsync();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return (null, new RespuestaSunat(false, null, "Sin conexión con SUNAT: " + ex.Message));
        }

        XDocument documento;
        try
        {
            documento = XDocument.Parse(texto);
        }
        catch (System.Xml.XmlException)
        {
            return (null, new RespuestaSunat(false, ((int)estadoHttp).ToString(), "Respuesta inesperada de SUNAT (HTTP " + (int)estadoHttp + ")."));
        }

        var cuerpo = documento.Root!.Element(Soap + "Body")!;
        var falla = cuerpo.Element(Soap + "Fault");
        if (falla is not null)
        {
            // El código viene como "soap-env:Client.0111" o directamente "0111" en faultstring.
            var codigo = (falla.Element("faultcode")?.Value ?? "").Split('.').Last();
            var mensaje = falla.Element("faultstring")?.Value ?? "Error de SUNAT";
            if (!codigo.All(char.IsDigit) || codigo.Length == 0)
                codigo = new string(mensaje.TakeWhile(char.IsDigit).ToArray());

            // 2000-3999: el documento es inválido (rechazo). Lo demás: no se procesó, reintentar.
            var rechazo = int.TryParse(codigo, out var n) && n >= 2000 && n < 4000;
            return (null, new RespuestaSunat(rechazo, codigo, mensaje));
        }

        return (cuerpo, null);
    }

    private static RespuestaSunat LeerCdr(byte[] zip)
    {
        using var archivo = new ZipArchive(new MemoryStream(zip), ZipArchiveMode.Read);
        var entrada = archivo.Entries.First(e => e.Name.EndsWith(".xml", StringComparison.OrdinalIgnoreCase));
        using var lector = entrada.Open();
        var cdr = XDocument.Load(lector);

        var respuesta = cdr.Descendants(Cac + "DocumentResponse").First().Element(Cac + "Response")!;
        var codigo = respuesta.Element(Cbc + "ResponseCode")!.Value.Trim();
        var descripcion = respuesta.Element(Cbc + "Description")?.Value.Trim();
        var observaciones = cdr.Root!.Elements(Cbc + "Note").Select(n => n.Value.Trim()).ToList();
        if (observaciones.Count > 0)
            descripcion += " Observaciones: " + string.Join(" | ", observaciones);

        return new RespuestaSunat(true, codigo, descripcion, zip);
    }

    private static byte[] Comprimir(string nombreArchivo, string xml)
    {
        using var memoria = new MemoryStream();
        using (var archivo = new ZipArchive(memoria, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entrada = archivo.CreateEntry(nombreArchivo + ".xml");
            using var escritor = entrada.Open();
            var bytes = Encoding.UTF8.GetBytes(xml);
            escritor.Write(bytes);
        }
        return memoria.ToArray();
    }
}
