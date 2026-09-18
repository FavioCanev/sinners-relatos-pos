using Microsoft.AspNetCore.Components;
using SinnersRelatos.Web.Models;
using SinnersRelatos.Web.Services.Interfaces;

namespace SinnersRelatos.Web.Components;

// Base para toda página que requiere sesión iniciada (y, en PaginaDeAdministrador, un rol
// concreto). Antes cada página copiaba a mano el mismo bloque "¿hay sesión? ¿tiene el rol? si
// no, redirigir" — y ese copy-paste ya se había olvidado en varias páginas (/mesas, /kds/barra,
// /kds/cocina), dejándolas accesibles sin haber iniciado sesión.
//
// No se usa [Authorize]/AuthorizeRouteView porque en un Blazor Web App el atributo [Authorize]
// en una página también se aplica al middleware de autorización HTTP normal, que exige un
// esquema de autenticación real (cookies, JWT, etc.) para poblar HttpContext.User — esta app no
// tiene uno: la sesión vive en ProtectedSessionStorage dentro del circuito interactivo. Por eso
// el chequeo se hace acá, dentro de OnInitializedAsync, igual que ya lo hacía cada página.
//
// Las páginas heredan de esta clase (@inherits PaginaConSesion o PaginaDeAdministrador) y
// sobrescriben OnInitializedConSesionAsync en vez de OnInitializedAsync.
public abstract class PaginaConSesion : ComponentBase
{
    [Inject] protected ISesionService SesionService { get; set; } = null!;
    [Inject] protected NavigationManager Navigation { get; set; } = null!;

    protected UsuarioSesion? usuarioActor;

    // PaginaDeAdministrador sobrescribe esto para exigir RolUsuario.Administrador.
    protected virtual RolUsuario? RolRequerido => null;

    protected sealed override async Task OnInitializedAsync()
    {
        usuarioActor = await SesionService.ObtenerUsuarioActualAsync();
        if (usuarioActor is null)
        {
            var returnUrl = Uri.EscapeDataString(new Uri(Navigation.Uri).PathAndQuery);
            Navigation.NavigateTo($"/login?returnUrl={returnUrl}");
            return;
        }

        if (RolRequerido is { } rol && usuarioActor.Rol != rol)
        {
            Navigation.NavigateTo("/mesas");
            return;
        }

        await OnInitializedConSesionAsync();
    }

    protected virtual Task OnInitializedConSesionAsync() => Task.CompletedTask;
}

public abstract class PaginaDeAdministrador : PaginaConSesion
{
    protected override RolUsuario? RolRequerido => RolUsuario.Administrador;
}
