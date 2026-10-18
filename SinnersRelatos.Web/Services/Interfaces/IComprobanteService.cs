using SinnersRelatos.Web.Data;
using SinnersRelatos.Web.Models;

namespace SinnersRelatos.Web.Services.Interfaces;

// Lo que se pide al cerrar la cuenta. Boleta: documento opcional (obligatorio desde S/ 700).
// Factura: RUC y razón social obligatorios. PorConsumo: una sola línea por el total.
public record DatosComprobante(TipoComprobante Tipo, string? NumeroDocumento, string? Nombre, string? Direccion, bool PorConsumo = false);

public interface IComprobanteService
{
    bool Habilitado { get; }
    decimal TasaIgv { get; }

    // Devuelve el motivo por el que los datos no sirven, o null si están bien.
    string? Validar(DatosComprobante datos, decimal total);

    // Agrega al contexto el comprobante del pedido (con correlativo y montos), sin guardar: así se
    // guarda en la misma operación que el cierre de la cuenta. El pedido debe venir con sus detalles.
    Comprobante Crear(AppDbContext context, Pedido pedido, DatosComprobante datos);

    // Arma y firma el XML de un comprobante ya guardado.
    Task GenerarXmlAsync(int comprobanteId);

    Task<Comprobante?> ObtenerDePedidoAsync(int pedidoId);
    Task<List<Comprobante>> ListarAsync(DateOnly desde, DateOnly hasta);
    Task SolicitarAnulacionAsync(int comprobanteId, string motivo, int actorUsuarioId);

    // Vuelve a dejar pendiente un comprobante rechazado (se regenera su XML y se reenvía).
    Task ReintentarAsync(int comprobanteId, int actorUsuarioId);
}
