using SinnersRelatos.Web.Models;

namespace SinnersRelatos.Web.Services.Interfaces;

public interface IModificadorService
{
    Task<List<GrupoModificador>> ListarGruposAsync();
    Task<GrupoModificador?> ObtenerGrupoPorIdAsync(int id);
    Task<GrupoModificador> CrearGrupoAsync(string nombre, bool esObligatorio, bool permiteMultiple, int actorUsuarioId);
    Task ActualizarGrupoAsync(int id, string nombre, bool esObligatorio, bool permiteMultiple, int actorUsuarioId);
    Task EliminarGrupoAsync(int id, int actorUsuarioId);
    Task<OpcionModificador> AgregarOpcionAsync(int grupoId, string nombre, decimal precioAdicional, int actorUsuarioId);
    Task ActualizarOpcionAsync(int opcionId, string nombre, decimal precioAdicional, int actorUsuarioId);
    Task CambiarEstadoOpcionAsync(int opcionId, bool activo, int actorUsuarioId);
    Task AsignarIngredienteAsync(int opcionId, int ingredienteId, decimal cantidadRequerida, int actorUsuarioId);
    Task QuitarIngredienteAsync(int opcionId, int ingredienteId, int actorUsuarioId);
}
