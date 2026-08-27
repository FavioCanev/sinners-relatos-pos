using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using SinnersRelatos.Web.Models;
using SinnersRelatos.Web.Services.Interfaces;

namespace SinnersRelatos.Web.Services;

public class SesionService(ProtectedSessionStorage sessionStorage) : ISesionService
{
    private const string Clave = "usuarioActual";

    public async Task<UsuarioSesion?> ObtenerUsuarioActualAsync()
    {
        var resultado = await sessionStorage.GetAsync<UsuarioSesion>(Clave);
        return resultado.Success ? resultado.Value : null;
    }

    public async Task IniciarSesionAsync(Usuario usuario)
    {
        var sesion = new UsuarioSesion
        {
            Id = usuario.Id,
            NombreCompleto = $"{usuario.Empleado.Nombres} {usuario.Empleado.Apellidos}",
            Rol = usuario.Rol
        };
        await sessionStorage.SetAsync(Clave, sesion);
    }

    public async Task CerrarSesionAsync() => await sessionStorage.DeleteAsync(Clave);
}
