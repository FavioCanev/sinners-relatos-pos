namespace SinnersRelatos.Web.Models;

public class Pedido
{
    public int Id { get; set; }
    public EstadoPedido Estado { get; set; } = EstadoPedido.Pendiente;
    public DateTime FechaCreacion { get; set; } = DateTime.Now;
    public DateTime? FechaCierre { get; set; }

    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;

    public ICollection<PedidoMesa> Mesas { get; set; } = [];
    public ICollection<DetallePedido> Detalles { get; set; } = [];
}
