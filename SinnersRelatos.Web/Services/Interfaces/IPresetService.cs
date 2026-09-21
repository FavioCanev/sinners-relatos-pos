using SinnersRelatos.Web.Models;

namespace SinnersRelatos.Web.Services.Interfaces;

public interface IPresetService
{
    Task<List<Preset>> ListarAsync(bool incluirInactivos = false);
    Task<Preset?> ObtenerPorIdAsync(int id);

    // productosCantidades: ProductoId -> Cantidad dentro del combo. Los productos pueden ser de
    // cualquier marca (un combo puede mezclar productos de Sinners y Relatos).
    Task<Preset> CrearAsync(string nombre, IReadOnlyDictionary<int, int> productosCantidades, int actorUsuarioId);
    Task ActualizarAsync(int id, string nombre, IReadOnlyDictionary<int, int> productosCantidades, int actorUsuarioId);
    Task CambiarEstadoAsync(int id, bool activo, int actorUsuarioId);
}
