using SinnersRelatos.Web.Models;

namespace SinnersRelatos.Web.Services.Interfaces;

public interface IUsuarioService
{
    Task<List<Usuario>> ListarAsync(bool incluirInactivos = false);
    Task<Usuario?> ObtenerPorIdAsync(int id);
    Task<Usuario> CrearAsync(Empleado empleado, string nombreUsuario, string password, RolUsuario rol, int actorUsuarioId);
    Task ActualizarRolAsync(int usuarioId, RolUsuario rol, int actorUsuarioId);
    Task CambiarPasswordAsync(int usuarioId, string nuevaPassword, int actorUsuarioId);
    Task CambiarEstadoAsync(int usuarioId, bool activo, int actorUsuarioId);
    Task<Usuario?> ValidarCredencialesAsync(string nombreUsuario, string password);
    Task<bool> CambiarPasswordPropioAsync(int usuarioId, string passwordActual, string nuevaPassword);
    Task ConfigurarPreguntaSeguridadAsync(int usuarioId, string pregunta, string respuesta);
    Task<string?> ObtenerPreguntaSeguridadAsync(string nombreUsuario);
    Task<bool> RestablecerPasswordPorPreguntaAsync(string nombreUsuario, string respuesta, string nuevaPassword);
}
