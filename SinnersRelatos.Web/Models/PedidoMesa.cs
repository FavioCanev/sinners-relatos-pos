namespace SinnersRelatos.Web.Models;

public class PedidoMesa
{
    public int PedidoId { get; set; }
    public Pedido Pedido { get; set; } = null!;

    public int MesaId { get; set; }
    public Mesa Mesa { get; set; } = null!;
}
