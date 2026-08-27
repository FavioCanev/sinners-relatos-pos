using SinnersRelatos.Web.Models;

namespace SinnersRelatos.Web.Services.Interfaces;

public class UsuarioSesion
{
    public int Id { get; set; }
    public string NombreCompleto { get; set; } = "";
    public RolUsuario Rol { get; set; }
}

public interface ISesionService
{
    Task<UsuarioSesion?> ObtenerUsuarioActualAsync();
    Task IniciarSesionAsync(Usuario usuario);
    Task CerrarSesionAsync();
}
