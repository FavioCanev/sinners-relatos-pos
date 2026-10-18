namespace SinnersRelatos.Web.Models;

// Envío asíncrono a SUNAT: resumen diario de boletas (RC) o comunicación de baja de facturas (RA).
// SUNAT responde primero con un ticket y el resultado se consulta después con ese ticket.
public class EnvioSunat
{
    public int Id { get; set; }
    public TipoEnvioSunat Tipo { get; set; }

    // Identificador del documento, ej. "RC-20261018-1": fecha de generación + correlativo del día.
    public string Identificador { get; set; } = "";
    public DateTime FechaGeneracion { get; set; }
    public DateOnly FechaComprobantes { get; set; }

    public string? Ticket { get; set; }
    public EstadoEnvioSunat Estado { get; set; } = EstadoEnvioSunat.Enviado;
    public string? CodigoRespuesta { get; set; }
    public string? MensajeRespuesta { get; set; }
    public byte[]? Cdr { get; set; }

    public ICollection<EnvioSunatDetalle> Detalles { get; set; } = [];
}

public class EnvioSunatDetalle
{
    public int EnvioSunatId { get; set; }
    public EnvioSunat EnvioSunat { get; set; } = null!;

    public int ComprobanteId { get; set; }
    public Comprobante Comprobante { get; set; } = null!;

    // true = la línea informa la anulación del comprobante (estado 3 en el resumen, o una baja).
    public bool EsAnulacion { get; set; }
}

public enum TipoEnvioSunat
{
    ResumenDiario,
    ComunicacionBaja
}

public enum EstadoEnvioSunat
{
    Enviado,
    Aceptado,
    Rechazado
}
