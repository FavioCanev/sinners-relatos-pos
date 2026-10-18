using Microsoft.EntityFrameworkCore;
using SinnersRelatos.Web.Data;
using SinnersRelatos.Web.Models;
using SinnersRelatos.Web.Services.Interfaces;
using SinnersRelatos.Web.Services.Sunat;

namespace SinnersRelatos.Web.Services;

public class ComprobanteService(
    IDbContextFactory<AppDbContext> contextFactory,
    SunatOptions opciones,
    FirmaDigital firma,
    IAuditoriaService auditoria) : IComprobanteService
{
    // Desde este monto la boleta debe identificar al cliente (resumen diario, guía SUNAT).
    private const decimal MontoBoletaConDocumento = 700m;

    // Plazo para anular: comunicación de baja / resumen con estado 3 hasta 7 días después.
    private const int DiasParaAnular = 7;

    public bool Habilitado => opciones.Habilitado;
    public decimal TasaIgv => opciones.TasaIgv;

    public string? Validar(DatosComprobante datos, decimal total)
    {
        var documento = datos.NumeroDocumento?.Trim() ?? "";
        var nombre = datos.Nombre?.Trim() ?? "";

        if (datos.Tipo == TipoComprobante.Factura)
        {
            if (documento.Length != 11 || !documento.All(char.IsDigit))
                return "La factura necesita un RUC de 11 dígitos.";
            if (nombre.Length == 0)
                return "La factura necesita la razón social del cliente.";
            return null;
        }

        if (documento.Length == 0)
            return total > MontoBoletaConDocumento
                ? $"Las boletas de más de S/ {MontoBoletaConDocumento:0} deben llevar el DNI y nombre del cliente."
                : null;

        if (!documento.All(char.IsDigit) || (documento.Length != 8 && documento.Length != 11))
            return "El documento debe ser un DNI (8 dígitos) o un RUC (11 dígitos).";
        if (nombre.Length == 0)
            return "Si indicas el documento del cliente, indica también su nombre.";
        return null;
    }

    public Comprobante Crear(AppDbContext context, Pedido pedido, DatosComprobante datos)
    {
        var serie = datos.Tipo == TipoComprobante.Factura ? opciones.SerieFactura : opciones.SerieBoleta;
        // Correlativo: siguiente al último de la serie. El índice único (Serie, Numero) evita que dos
        // cierres simultáneos se queden con el mismo número (el segundo falla y se reintenta).
        var ultimo = context.Comprobantes.Where(c => c.Serie == serie).Max(c => (int?)c.Numero) ?? 0;

        var lineas = LineasDe(pedido, datos.PorConsumo);
        var montos = lineas.Select(l => UblXml.CalcularLinea(l, opciones.TasaIgv)).ToList();
        var documento = datos.NumeroDocumento?.Trim() ?? "";

        var comprobante = new Comprobante
        {
            Tipo = datos.Tipo,
            Serie = serie,
            Numero = ultimo + 1,
            FechaEmision = DateTime.Now,
            ClienteTipoDocumento = documento.Length switch { 8 => "1", 11 => "6", _ => "0" },
            ClienteNumeroDocumento = documento.Length == 0 ? "-" : documento,
            ClienteNombre = documento.Length == 0 ? "CLIENTES VARIOS" : datos.Nombre!.Trim().ToUpperInvariant(),
            ClienteDireccion = string.IsNullOrWhiteSpace(datos.Direccion) ? null : datos.Direccion.Trim(),
            PorConsumo = datos.PorConsumo,
            TasaIgv = opciones.TasaIgv,
            OperacionesGravadas = montos.Sum(m => m.ValorVenta),
            Igv = montos.Sum(m => m.Igv),
            Total = montos.Sum(m => m.Total),
            Pedido = pedido
        };

        context.Comprobantes.Add(comprobante);
        return comprobante;
    }

    public async Task GenerarXmlAsync(int comprobanteId)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var comprobante = await context.Comprobantes
            .Include(c => c.Pedido).ThenInclude(p => p.Detalles).ThenInclude(d => d.Producto)
            .Include(c => c.Pedido).ThenInclude(p => p.Detalles).ThenInclude(d => d.Modificadores).ThenInclude(m => m.OpcionModificador)
            .FirstAsync(c => c.Id == comprobanteId);

        var documento = UblXml.Comprobante(comprobante, LineasDe(comprobante.Pedido, comprobante.PorConsumo), opciones);
        var firmado = firma.Firmar(documento);
        comprobante.XmlFirmado = firmado.Xml;
        comprobante.Hash = firmado.Hash;
        await context.SaveChangesAsync();
    }

    public async Task<Comprobante?> ObtenerDePedidoAsync(int pedidoId)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        return await context.Comprobantes.AsNoTracking()
            .Where(c => c.PedidoId == pedidoId)
            .OrderByDescending(c => c.Id)
            .FirstOrDefaultAsync();
    }

    public async Task<List<Comprobante>> ListarAsync(DateOnly desde, DateOnly hasta)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var inicio = desde.ToDateTime(TimeOnly.MinValue);
        var fin = hasta.AddDays(1).ToDateTime(TimeOnly.MinValue);
        return await context.Comprobantes.AsNoTracking()
            .Where(c => c.FechaEmision >= inicio && c.FechaEmision < fin)
            .OrderByDescending(c => c.FechaEmision)
            .ToListAsync();
    }

    public async Task SolicitarAnulacionAsync(int comprobanteId, string motivo, int actorUsuarioId)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var comprobante = await context.Comprobantes.FirstAsync(c => c.Id == comprobanteId);

        // Solo se anula lo que SUNAT ya aceptó: la baja (o el estado 3 del resumen) referencia un
        // comprobante que SUNAT debe conocer.
        if (comprobante.Estado is not (EstadoComprobante.Aceptado or EstadoComprobante.Observado))
            throw new InvalidOperationException("Solo se puede anular un comprobante que SUNAT ya aceptó. Si está pendiente, espera a que se envíe.");
        if (comprobante.FechaEmision.Date < DateTime.Today.AddDays(-DiasParaAnular))
            throw new InvalidOperationException($"Pasaron más de {DiasParaAnular} días desde la emisión: ya no se puede anular, corresponde una nota de crédito.");
        if (string.IsNullOrWhiteSpace(motivo))
            throw new InvalidOperationException("Indica el motivo de la anulación.");

        comprobante.Estado = EstadoComprobante.PorAnular;
        comprobante.MotivoAnulacion = motivo.Trim();
        await context.SaveChangesAsync();

        await auditoria.RegistrarAsync(actorUsuarioId, TiposAccionAuditoria.AnularComprobante,
            $"Pidió anular {comprobante.SerieNumero} (S/ {comprobante.Total:0.00}): {comprobante.MotivoAnulacion}");
    }

    public async Task ReintentarAsync(int comprobanteId, int actorUsuarioId)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var comprobante = await context.Comprobantes.FirstAsync(c => c.Id == comprobanteId);
        if (comprobante.Estado != EstadoComprobante.Rechazado)
            throw new InvalidOperationException("Solo se reintenta un comprobante rechazado.");

        comprobante.Estado = EstadoComprobante.Pendiente;
        comprobante.XmlFirmado = null;
        comprobante.Hash = null;
        await context.SaveChangesAsync();

        await auditoria.RegistrarAsync(actorUsuarioId, TiposAccionAuditoria.ReintentarComprobante,
            $"Reintentó el envío a SUNAT de {comprobante.SerieNumero} (rechazado: {comprobante.CodigoRespuesta}).");
    }

    // Una línea por ítem del pedido, con sus modificadores en la descripción y su precio sumado;
    // "por consumo" las resume en una sola línea por el total.
    // ponytail: ítems con precio 0 no se incluyen (serían "operaciones gratuitas", con otros
    // códigos de SUNAT); agregarlos si algún día se regalan productos desde el sistema.
    private static List<LineaComprobante> LineasDe(Pedido pedido, bool porConsumo)
    {
        var lineas = LineasDetalladas(pedido);
        return porConsumo
            ? [new LineaComprobante("CONSUMO DE ALIMENTOS Y BEBIDAS", 1, lineas.Sum(l => l.PrecioUnitarioConIgv * l.Cantidad))]
            : lineas;
    }

    private static List<LineaComprobante> LineasDetalladas(Pedido pedido) =>
        pedido.Detalles
            .OrderBy(d => d.Id)
            .Select(d => new LineaComprobante(
                d.Modificadores.Count > 0
                    ? $"{d.Producto.Nombre} ({string.Join(", ", d.Modificadores.Select(m => m.OpcionModificador.Nombre))})"
                    : d.Producto.Nombre,
                d.Cantidad,
                d.PrecioUnitario + d.Modificadores.Sum(m => m.PrecioAdicional)))
            .Where(l => l.PrecioUnitarioConIgv > 0)
            .ToList();
}
