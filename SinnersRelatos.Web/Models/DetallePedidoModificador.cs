namespace SinnersRelatos.Web.Models;

public class DetallePedidoModificador
{
    public int DetallePedidoId { get; set; }
    public DetallePedido DetallePedido { get; set; } = null!;

    public int OpcionModificadorId { get; set; }
    public OpcionModificador OpcionModificador { get; set; } = null!;

    public decimal PrecioAdicional { get; set; }
}
