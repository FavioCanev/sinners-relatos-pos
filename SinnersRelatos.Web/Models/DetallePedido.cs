namespace SinnersRelatos.Web.Models;

public class DetallePedido
{
    public int Id { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public EstadoDetallePedido Estado { get; set; } = EstadoDetallePedido.Pendiente;
    public DateTime FechaCreacion { get; set; } = DateTime.Now;

    public int PedidoId { get; set; }
    public Pedido Pedido { get; set; } = null!;

    public int ProductoId { get; set; }
    public Producto Producto { get; set; } = null!;

    public ICollection<DetallePedidoModificador> Modificadores { get; set; } = [];
}
