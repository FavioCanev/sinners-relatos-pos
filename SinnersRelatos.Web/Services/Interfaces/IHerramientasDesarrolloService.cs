namespace SinnersRelatos.Web.Services.Interfaces;

public interface IHerramientasDesarrolloService
{
    Task<bool> ObtenerModoPruebasAsync();
    Task CambiarModoPruebasAsync(bool activo, int actorUsuarioId);
    Task VaciarStockAsync(int actorUsuarioId);
    Task CargarStockDePruebaAsync(int actorUsuarioId);
    Task<int> SimularPedidosAsync(int cantidad, int actorUsuarioId);
    Task<int> EliminarPedidosDePruebaAsync(int actorUsuarioId);
}
