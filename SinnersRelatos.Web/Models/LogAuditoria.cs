namespace SinnersRelatos.Web.Models;

public class LogAuditoria
{
    public int Id { get; set; }
    public DateTime FechaHora { get; set; } = DateTime.Now;

    public int? UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }

    public required string TipoAccion { get; set; }
    public required string Detalle { get; set; }
}
