namespace SinnersRelatos.Web.Models;

public class DetallePedido
{
    public int Id { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public EstadoDetallePedido Estado { get; set; } = EstadoDetallePedido.Recibido;
    public DateTime FechaCreacion { get; set; } = DateTime.Now;

    // Momento del último cambio de estado (Recibido -> En preparación -> Listo). Nulo mientras
    // sigue en "Recibido": ahí se usa FechaCreacion, que ya marca cuándo entró a ese estado. Sirve
    // para ordenar cada columna del KDS por lo más reciente primero.
    public DateTime? FechaCambioEstado { get; set; }

    public int PedidoId { get; set; }
    public Pedido Pedido { get; set; } = null!;

    public int ProductoId { get; set; }
    public Producto Producto { get; set; } = null!;

    public ICollection<DetallePedidoModificador> Modificadores { get; set; } = [];
}
