using SinnersRelatos.Web.Models;

namespace SinnersRelatos.Web.Services.Interfaces;

public class MesaEstado
{
    public required Mesa Mesa { get; init; }
    public bool Ocupada { get; init; }
    public int? PedidoId { get; init; }

    // Hora del primer ítem confirmado del pedido activo (no de cuándo se abrió la pantalla de
    // la mesa), para mostrar cuánto tiempo lleva realmente esperando. Nulo si está libre.
    public DateTime? OcupadaDesde { get; init; }
}

public interface IMesaService
{
    Task<List<MesaEstado>> ListarPorMarcaAsync(Marca marca);
    Task<Mesa?> ObtenerPorIdAsync(int id);
}
