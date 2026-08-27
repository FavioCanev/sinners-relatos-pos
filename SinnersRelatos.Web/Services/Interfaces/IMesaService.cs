using SinnersRelatos.Web.Models;

namespace SinnersRelatos.Web.Services.Interfaces;

public class MesaEstado
{
    public required Mesa Mesa { get; init; }
    public bool Ocupada { get; init; }
    public int? PedidoId { get; init; }
}

public interface IMesaService
{
    Task<List<MesaEstado>> ListarPorMarcaAsync(Marca marca);
    Task<Mesa?> ObtenerPorIdAsync(int id);
}
