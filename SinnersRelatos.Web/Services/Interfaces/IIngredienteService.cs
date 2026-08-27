using SinnersRelatos.Web.Models;

namespace SinnersRelatos.Web.Services.Interfaces;

public interface IIngredienteService
{
    Task<List<Ingrediente>> ListarAsync(bool incluirInactivos = false);
    Task<Ingrediente?> ObtenerPorIdAsync(int id);
    Task<Ingrediente> CrearAsync(string nombre, string unidadMedida, decimal stockInicial, decimal stockMinimo, int actorUsuarioId);
    Task ActualizarAsync(int id, string nombre, string unidadMedida, decimal stockMinimo, int actorUsuarioId);
    Task AjustarStockAsync(int id, decimal cantidad, int actorUsuarioId);
    Task CambiarEstadoAsync(int id, bool activo, int actorUsuarioId);
    Task<List<Ingrediente>> ListarBajoStockAsync();
}
