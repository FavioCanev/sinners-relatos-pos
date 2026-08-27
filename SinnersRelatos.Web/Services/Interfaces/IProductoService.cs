using SinnersRelatos.Web.Models;

namespace SinnersRelatos.Web.Services.Interfaces;

public interface IProductoService
{
    Task<List<Producto>> ListarAsync(Marca? marca = null, int? categoriaId = null, bool incluirInactivos = false);
    Task<Producto?> ObtenerPorIdAsync(int id);
    Task<Producto> CrearAsync(Producto producto, IEnumerable<int> grupoModificadorIds, IReadOnlyDictionary<int, decimal> receta, int actorUsuarioId);
    Task ActualizarAsync(Producto producto, IEnumerable<int> grupoModificadorIds, IReadOnlyDictionary<int, decimal> receta, int actorUsuarioId);
    Task CambiarEstadoAsync(int id, bool activo, int actorUsuarioId);
}
