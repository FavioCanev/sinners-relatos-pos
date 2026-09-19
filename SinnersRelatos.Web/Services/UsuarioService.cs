using Microsoft.EntityFrameworkCore;
using SinnersRelatos.Web.Data;
using SinnersRelatos.Web.Models;
using SinnersRelatos.Web.Services.Interfaces;

namespace SinnersRelatos.Web.Services;

public class UsuarioService(IDbContextFactory<AppDbContext> contextFactory, IPasswordHasher passwordHasher, IAuditoriaService auditoria) : IUsuarioService
{
    public async Task<List<Usuario>> ListarAsync(bool incluirInactivos = false)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        var query = context.Usuarios.Include(u => u.Empleado).AsQueryable();
        if (!incluirInactivos)
            query = query.Where(u => u.Activo);

        return await query.OrderBy(u => u.Empleado.Apellidos).ToListAsync();
    }

    public async Task<Usuario?> ObtenerPorIdAsync(int id)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        return await context.Usuarios.Include(u => u.Empleado).FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<Usuario> CrearAsync(Empleado empleado, string nombreUsuario, string password, RolUsuario rol, int actorUsuarioId)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        var nombreEnUso = await context.Usuarios.AnyAsync(u => u.NombreUsuario == nombreUsuario);
        if (nombreEnUso)
            throw new InvalidOperationException($"El nombre de usuario '{nombreUsuario}' ya está en uso.");

        var usuario = new Usuario
        {
            NombreUsuario = nombreUsuario,
            PasswordHash = passwordHasher.Hash(password),
            Rol = rol,
            Empleado = empleado
        };

        context.Usuarios.Add(usuario);
        await context.SaveChangesAsync();

        await auditoria.RegistrarAsync(actorUsuarioId, TiposAccionAuditoria.CrearUsuario,
            $"Creó el usuario '{nombreUsuario}' ({empleado.Nombres} {empleado.Apellidos}) con rol {rol}.");

        return usuario;
    }

    public async Task ActualizarRolAsync(int usuarioId, RolUsuario rol, int actorUsuarioId)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        var usuario = await context.Usuarios.FindAsync(usuarioId)
            ?? throw new InvalidOperationException($"Usuario {usuarioId} no encontrado.");

        var rolAnterior = usuario.Rol;
        usuario.Rol = rol;
        await context.SaveChangesAsync();

        var detalle = rolAnterior != rol
            ? $"Actualizó el usuario '{usuario.NombreUsuario}' y cambió su rol de {rolAnterior} a {rol}."
            : $"Actualizó el usuario '{usuario.NombreUsuario}'.";
        await auditoria.RegistrarAsync(actorUsuarioId, TiposAccionAuditoria.ActualizarUsuario, detalle);
    }

    public async Task CambiarPasswordAsync(int usuarioId, string nuevaPassword, int actorUsuarioId)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        var usuario = await context.Usuarios.FindAsync(usuarioId)
            ?? throw new InvalidOperationException($"Usuario {usuarioId} no encontrado.");

        usuario.PasswordHash = passwordHasher.Hash(nuevaPassword);
        await context.SaveChangesAsync();

        await auditoria.RegistrarAsync(actorUsuarioId, TiposAccionAuditoria.CambiarPassword,
            $"Cambió la contraseña de '{usuario.NombreUsuario}'.");
    }

    public async Task CambiarEstadoAsync(int usuarioId, bool activo, int actorUsuarioId)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        var usuario = await context.Usuarios.FindAsync(usuarioId)
            ?? throw new InvalidOperationException($"Usuario {usuarioId} no encontrado.");

        usuario.Activo = activo;
        await context.SaveChangesAsync();

        await auditoria.RegistrarAsync(actorUsuarioId, TiposAccionAuditoria.CambiarEstadoUsuario,
            $"{(activo ? "Activó" : "Desactivó")} el usuario '{usuario.NombreUsuario}'.");
    }

    public async Task<Usuario?> ValidarCredencialesAsync(string nombreUsuario, string password)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        var usuario = await context.Usuarios
            .Include(u => u.Empleado)
            .FirstOrDefaultAsync(u => u.NombreUsuario == nombreUsuario && u.Activo);

        if (usuario is null || !passwordHasher.Verify(password, usuario.PasswordHash))
        {
            await auditoria.RegistrarAsync(usuario?.Id, TiposAccionAuditoria.LoginFallido,
                $"Intento de inicio de sesión fallido para '{nombreUsuario}'.");
            return null;
        }

        await auditoria.RegistrarAsync(usuario.Id, TiposAccionAuditoria.Login,
            $"'{usuario.NombreUsuario}' inició sesión.");

        return usuario;
    }

    public async Task<bool> CambiarPasswordPropioAsync(int usuarioId, string passwordActual, string nuevaPassword)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        var usuario = await context.Usuarios.FindAsync(usuarioId)
            ?? throw new InvalidOperationException($"Usuario {usuarioId} no encontrado.");

        if (!passwordHasher.Verify(passwordActual, usuario.PasswordHash))
            return false;

        usuario.PasswordHash = passwordHasher.Hash(nuevaPassword);
        await context.SaveChangesAsync();

        await auditoria.RegistrarAsync(usuarioId, TiposAccionAuditoria.CambiarPassword,
            $"'{usuario.NombreUsuario}' cambió su propia contraseña.");

        return true;
    }
}
