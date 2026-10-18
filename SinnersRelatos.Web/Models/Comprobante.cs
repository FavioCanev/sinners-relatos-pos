namespace SinnersRelatos.Web.Models;

// Comprobante electrónico (boleta o factura) emitido al cerrar la cuenta de un pedido. Los montos
// y el XML firmado quedan guardados tal como se emitieron: no se recalculan aunque después cambien
// los precios o el nombre de un producto.
public class Comprobante
{
    public int Id { get; set; }
    public TipoComprobante Tipo { get; set; }
    public string Serie { get; set; } = "";
    public int Numero { get; set; }
    public DateTime FechaEmision { get; set; }

    // Catálogo 06 de SUNAT: "1" DNI, "6" RUC, "0" sin documento (boleta a "clientes varios").
    public string ClienteTipoDocumento { get; set; } = "0";
    public string ClienteNumeroDocumento { get; set; } = "-";
    public string ClienteNombre { get; set; } = "";
    public string? ClienteDireccion { get; set; }

    // true = una sola línea "CONSUMO DE ALIMENTOS Y BEBIDAS" por el total, en vez del detalle de productos.
    public bool PorConsumo { get; set; }

    public decimal TasaIgv { get; set; }
    public decimal OperacionesGravadas { get; set; }
    public decimal Igv { get; set; }
    public decimal Total { get; set; }

    public EstadoComprobante Estado { get; set; } = EstadoComprobante.Pendiente;

    // XML firmado listo para enviar y su "valor resumen" (DigestValue), que va impreso en el
    // ticket. Nulos solo si la generación falló; el envío en segundo plano lo vuelve a intentar.
    public string? XmlFirmado { get; set; }
    public string? Hash { get; set; }

    // Última respuesta de SUNAT (código y descripción de la constancia, o el error del envío).
    public string? CodigoRespuesta { get; set; }
    public string? MensajeRespuesta { get; set; }
    public byte[]? Cdr { get; set; }

    public string? MotivoAnulacion { get; set; }

    public int PedidoId { get; set; }
    public Pedido Pedido { get; set; } = null!;

    public string SerieNumero => $"{Serie}-{Numero}";
}

public enum TipoComprobante
{
    Factura = 1,
    Boleta = 3
}

public enum EstadoComprobante
{
    // Emitido localmente, todavía sin respuesta de SUNAT (boletas: aún no informadas en un resumen).
    Pendiente,
    // Boleta incluida en un resumen diario cuyo ticket aún no tiene respuesta.
    EnResumen,
    Aceptado,
    Observado,
    Rechazado,
    // Se pidió anular: se informa a SUNAT (resumen con estado 3 o comunicación de baja).
    PorAnular,
    EnAnulacion,
    Anulado
}
