using SinnersRelatos.Web.Models;

namespace SinnersRelatos.Web.Services.Interfaces;

public class ItemCarrito
{
    public required int ProductoId { get; init; }
    public int Cantidad { get; init; } = 1;
    public List<int> OpcionModificadorIds { get; init; } = [];
    public bool ForzarVenta { get; init; }
}

public class ItemKds
{
    public required int DetallePedidoId { get; init; }
    public required int PedidoId { get; init; }
    public required string MesaEtiqueta { get; init; }
    public required string ProductoNombre { get; init; }
    public required int Cantidad { get; init; }
    public required List<string> Modificadores { get; init; }
    public required DateTime FechaCreacion { get; init; }
}

public class SolicitudImpresion
{
    public required int PedidoId { get; init; }
    public required string MesaEtiqueta { get; init; }
}

public interface IPedidoService
{
    Task<Pedido> ObtenerOCrearPedidoAsync(int mesaId, int usuarioId);
    Task<Pedido?> ObtenerConDetalleAsync(int pedidoId);
    Task<Dictionary<int, bool>> VerificarDisponibilidadAsync(IEnumerable<int> productoIds);
    Task ConfirmarItemsAsync(int pedidoId, IEnumerable<ItemCarrito> items, int actorUsuarioId);
    Task AnularAsync(int pedidoId, int actorUsuarioId);
    Task CerrarAsync(int pedidoId, int actorUsuarioId);
    Task<Pedido> FusionarAsync(IEnumerable<int> mesaIds, int usuarioId);
    Task<List<ItemKds>> ListarParaKdsAsync(DestinoPreparacion destino);
    Task MarcarEntregadoAsync(int detallePedidoId);
    Task SolicitarImpresionAsync(int pedidoId);
}
