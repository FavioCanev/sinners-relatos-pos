using Microsoft.EntityFrameworkCore;
using SinnersRelatos.Web.Data;
using SinnersRelatos.Web.Models;
using SinnersRelatos.Web.Services.Interfaces;

namespace SinnersRelatos.Web.Services.Sunat;

// Envía a SUNAT en segundo plano, para que cerrar una cuenta nunca dependa de internet:
//  1. Firma los comprobantes que quedaron sin XML (si falló al cerrar la cuenta).
//  2. Envía las facturas una por una (SUNAT responde al instante).
//  3. Informa las boletas en un resumen diario cada cierto tiempo (SUNAT responde con un ticket).
//  4. Informa las anulaciones (boletas: resumen con estado 3; facturas: comunicación de baja).
//  5. Consulta los tickets pendientes y actualiza el estado de cada comprobante.
// Corre cada minuto, o antes si alguien lo despierta (ej. al emitir una factura).
public class EnvioSunatWorker(
    IDbContextFactory<AppDbContext> contextFactory,
    IServiceScopeFactory scopeFactory,
    SunatOptions opciones,
    FirmaDigital firma,
    IHttpClientFactory httpFactory,
    ILogger<EnvioSunatWorker> logger) : BackgroundService
{
    private readonly SemaphoreSlim senal = new(0);

    // Una sola vuelta de envío a la vez (la del ciclo y la del botón "Enviar pendientes ahora").
    private readonly SemaphoreSlim enCurso = new(1, 1);

    public void Despertar()
    {
        if (senal.CurrentCount == 0)
            senal.Release();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!opciones.Habilitado)
            return;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcesarAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error en el envío de comprobantes a SUNAT");
            }

            try
            {
                await senal.WaitAsync(TimeSpan.FromMinutes(1), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    public async Task ProcesarAsync()
    {
        await enCurso.WaitAsync();
        try
        {
            var cliente = new SunatClient(httpFactory.CreateClient(nameof(SunatClient)), opciones);

            await FirmarPendientesAsync();
            await EnviarFacturasAsync(cliente);
            await EnviarResumenesAsync(cliente, anulaciones: false);
            await EnviarResumenesAsync(cliente, anulaciones: true);
            await EnviarBajasAsync(cliente);
            await ConsultarTicketsAsync(cliente);
        }
        finally
        {
            enCurso.Release();
        }
    }

    private async Task FirmarPendientesAsync()
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var ids = await context.Comprobantes
            .Where(c => c.XmlFirmado == null && c.Estado == EstadoComprobante.Pendiente)
            .Select(c => c.Id).ToListAsync();
        if (ids.Count == 0)
            return;

        using var scope = scopeFactory.CreateScope();
        var servicio = scope.ServiceProvider.GetRequiredService<IComprobanteService>();
        foreach (var id in ids)
            await servicio.GenerarXmlAsync(id);
    }

    private async Task EnviarFacturasAsync(SunatClient cliente)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var facturas = await context.Comprobantes
            .Where(c => c.Tipo == TipoComprobante.Factura && c.Estado == EstadoComprobante.Pendiente && c.XmlFirmado != null)
            .OrderBy(c => c.Id)
            .ToListAsync();

        foreach (var factura in facturas)
        {
            var respuesta = await cliente.EnviarComprobanteAsync($"{opciones.Ruc}-01-{factura.SerieNumero}", factura.XmlFirmado!);
            factura.CodigoRespuesta = respuesta.Codigo;
            factura.MensajeRespuesta = respuesta.Mensaje;
            factura.Cdr = respuesta.Cdr ?? factura.Cdr;

            if (respuesta.Aceptado)
                factura.Estado = respuesta.EsObservacion ? EstadoComprobante.Observado : EstadoComprobante.Aceptado;
            else if (respuesta.Codigo == "1033")
                // "Registrado previamente": un envío anterior llegó aunque no recibimos la respuesta.
                factura.Estado = EstadoComprobante.Aceptado;
            else if (respuesta.Recibido)
                factura.Estado = EstadoComprobante.Rechazado;

            await context.SaveChangesAsync();

            // Sin conexión: no tiene sentido seguir intentando con el resto en esta vuelta.
            if (!respuesta.Recibido && respuesta.Codigo is null)
                return;
        }
    }

    private async Task EnviarResumenesAsync(SunatClient cliente, bool anulaciones)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var estado = anulaciones ? EstadoComprobante.PorAnular : EstadoComprobante.Pendiente;
        var boletas = await context.Comprobantes
            .Where(c => c.Tipo == TipoComprobante.Boleta && c.Estado == estado && c.XmlFirmado != null)
            .OrderBy(c => c.Id)
            .ToListAsync();
        if (boletas.Count == 0)
            return;

        // Las boletas nuevas se juntan: se informan cada N minutos, o enseguida si son de un día
        // anterior. Las anulaciones se informan apenas se piden.
        if (!anulaciones)
        {
            var ultimoResumen = await context.EnviosSunat
                .Where(e => e.Tipo == TipoEnvioSunat.ResumenDiario)
                .MaxAsync(e => (DateTime?)e.FechaGeneracion);
            var hayDeDiasAnteriores = boletas.Any(b => b.FechaEmision.Date < DateTime.Today);
            if (!hayDeDiasAnteriores && ultimoResumen is { } ultimo && DateTime.Now - ultimo < TimeSpan.FromMinutes(opciones.MinutosEntreResumenes))
                return;
        }

        // Un resumen por fecha de emisión (SUNAT exige que todas sean del mismo día), máx. 500 líneas.
        foreach (var grupo in boletas.GroupBy(b => DateOnly.FromDateTime(b.FechaEmision)))
        {
            foreach (var lote in grupo.Chunk(500))
            {
                var ahora = DateTime.Now;
                var envio = await NuevoEnvioAsync(context, TipoEnvioSunat.ResumenDiario, "RC", ahora, grupo.Key);
                var xml = UblXml.ResumenDiario(envio.Identificador, ahora, grupo.Key,
                    lote.Select(b => (b, anulaciones)).ToList(), opciones);

                if (!await EnviarLoteAsync(cliente, context, envio, xml, lote, anulaciones))
                    return;
            }
        }
    }

    private async Task EnviarBajasAsync(SunatClient cliente)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var facturas = await context.Comprobantes
            .Where(c => c.Tipo == TipoComprobante.Factura && c.Estado == EstadoComprobante.PorAnular)
            .OrderBy(c => c.Id)
            .ToListAsync();

        foreach (var grupo in facturas.GroupBy(f => DateOnly.FromDateTime(f.FechaEmision)))
        {
            var ahora = DateTime.Now;
            var envio = await NuevoEnvioAsync(context, TipoEnvioSunat.ComunicacionBaja, "RA", ahora, grupo.Key);
            var xml = UblXml.ComunicacionBaja(envio.Identificador, ahora, grupo.Key, grupo.ToList(), opciones);

            if (!await EnviarLoteAsync(cliente, context, envio, xml, grupo.ToList(), esAnulacion: true))
                return;
        }
    }

    // Firma y envía un resumen o baja. Devuelve false si no hubo conexión (se reintenta luego).
    private async Task<bool> EnviarLoteAsync(SunatClient cliente, AppDbContext context, EnvioSunat envio,
        System.Xml.Linq.XDocument xml, IReadOnlyList<Comprobante> comprobantes, bool esAnulacion)
    {
        var firmado = firma.Firmar(xml);
        var respuesta = await cliente.EnviarResumenAsync($"{opciones.Ruc}-{envio.Identificador}", firmado.Xml);

        if (!respuesta.Recibido)
        {
            // No llegó: no se guarda el envío, así el mismo correlativo se reusa en el próximo intento.
            foreach (var c in comprobantes)
                c.MensajeRespuesta = respuesta.Mensaje;
            await context.SaveChangesAsync();
            return respuesta.Codigo is not null;
        }

        envio.Ticket = respuesta.Ticket;
        envio.Estado = respuesta.EnProceso ? EstadoEnvioSunat.Enviado : EstadoEnvioSunat.Rechazado;
        envio.CodigoRespuesta = respuesta.Codigo;
        envio.MensajeRespuesta = respuesta.Mensaje;
        foreach (var c in comprobantes)
        {
            envio.Detalles.Add(new EnvioSunatDetalle { Comprobante = c, EsAnulacion = esAnulacion });
            AplicarResultado(c, esAnulacion, respuesta);
        }

        context.EnviosSunat.Add(envio);
        await context.SaveChangesAsync();
        return true;
    }

    private async Task ConsultarTicketsAsync(SunatClient cliente)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var envios = await context.EnviosSunat
            .Include(e => e.Detalles).ThenInclude(d => d.Comprobante)
            .Where(e => e.Estado == EstadoEnvioSunat.Enviado && e.Ticket != null)
            .ToListAsync();

        foreach (var envio in envios)
        {
            var respuesta = await cliente.ConsultarTicketAsync(envio.Ticket!);
            if (respuesta.EnProceso || !respuesta.Recibido)
                continue;

            envio.Estado = respuesta.Aceptado ? EstadoEnvioSunat.Aceptado : EstadoEnvioSunat.Rechazado;
            envio.CodigoRespuesta = respuesta.Codigo;
            envio.MensajeRespuesta = respuesta.Mensaje;
            envio.Cdr = respuesta.Cdr;
            foreach (var detalle in envio.Detalles)
                AplicarResultado(detalle.Comprobante, detalle.EsAnulacion, respuesta);

            await context.SaveChangesAsync();
        }
    }

    // Traslada la respuesta de SUNAT a un comprobante incluido en un resumen o baja.
    private static void AplicarResultado(Comprobante c, bool esAnulacion, RespuestaSunat respuesta)
    {
        c.CodigoRespuesta = respuesta.Codigo;
        c.MensajeRespuesta = respuesta.Mensaje;

        if (respuesta.EnProceso)
            c.Estado = esAnulacion ? EstadoComprobante.EnAnulacion : EstadoComprobante.EnResumen;
        else if (respuesta.Aceptado)
            c.Estado = esAnulacion ? EstadoComprobante.Anulado
                : respuesta.EsObservacion ? EstadoComprobante.Observado : EstadoComprobante.Aceptado;
        else
            // Si la anulación fue rechazada, el comprobante sigue vigente (aceptado).
            c.Estado = esAnulacion ? EstadoComprobante.Aceptado : EstadoComprobante.Rechazado;
    }

    // Identificador "RC-AAAAMMDD-N" / "RA-AAAAMMDD-N": N es correlativo dentro del día de generación.
    private static async Task<EnvioSunat> NuevoEnvioAsync(AppDbContext context, TipoEnvioSunat tipo, string prefijo,
        DateTime ahora, DateOnly fechaComprobantes)
    {
        var baseId = $"{prefijo}-{ahora:yyyyMMdd}-";
        var existentes = await context.EnviosSunat.CountAsync(e => e.Identificador.StartsWith(baseId));
        return new EnvioSunat
        {
            Tipo = tipo,
            Identificador = baseId + (existentes + 1),
            FechaGeneracion = ahora,
            FechaComprobantes = fechaComprobantes
        };
    }
}
