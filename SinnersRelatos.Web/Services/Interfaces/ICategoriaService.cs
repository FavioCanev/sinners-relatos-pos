using SinnersRelatos.Web.Models;

namespace SinnersRelatos.Web.Services.Interfaces;

public interface ICategoriaService
{
    Task<List<Categoria>> ListarAsync(bool incluirInactivas = false);
    Task<Categoria?> ObtenerPorIdAsync(int id);
    Task<Categoria> CrearAsync(string nombre, int actorUsuarioId);
    Task ActualizarAsync(int id, string nombre, int actorUsuarioId);
    Task CambiarEstadoAsync(int id, bool activo, int actorUsuarioId);
}
