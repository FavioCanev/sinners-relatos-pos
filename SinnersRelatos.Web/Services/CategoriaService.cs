using Microsoft.EntityFrameworkCore;
using SinnersRelatos.Web.Data;
using SinnersRelatos.Web.Models;
using SinnersRelatos.Web.Services.Interfaces;

namespace SinnersRelatos.Web.Services;

public class CategoriaService(AppDbContext context, IAuditoriaService auditoria) : ICategoriaService
{
    public async Task<List<Categoria>> ListarAsync(bool incluirInactivas = false)
    {
        var query = context.Categorias.AsQueryable();
        if (!incluirInactivas)
            query = query.Where(c => c.Activo);

        return await query.OrderBy(c => c.Nombre).ToListAsync();
    }

    public async Task<Categoria?> ObtenerPorIdAsync(int id) =>
        await context.Categorias.FirstOrDefaultAsync(c => c.Id == id);

    public async Task<Categoria> CrearAsync(string nombre, int actorUsuarioId)
    {
        var enUso = await context.Categorias.AnyAsync(c => c.Nombre == nombre);
        if (enUso)
            throw new InvalidOperationException($"La categoría '{nombre}' ya existe.");

        var categoria = new Categoria { Nombre = nombre };
        context.Categorias.Add(categoria);
        await context.SaveChangesAsync();

        await auditoria.RegistrarAsync(actorUsuarioId, TiposAccionAuditoria.CrearCategoria, $"Creó la categoría '{nombre}'.");
        return categoria;
    }

    public async Task ActualizarAsync(int id, string nombre, int actorUsuarioId)
    {
        var categoria = await context.Categorias.FindAsync(id)
            ?? throw new InvalidOperationException($"Categoría {id} no encontrada.");

        var enUso = await context.Categorias.AnyAsync(c => c.Nombre == nombre && c.Id != id);
        if (enUso)
            throw new InvalidOperationException($"La categoría '{nombre}' ya existe.");

        var nombreAnterior = categoria.Nombre;
        categoria.Nombre = nombre;
        await context.SaveChangesAsync();

        await auditoria.RegistrarAsync(actorUsuarioId, TiposAccionAuditoria.ActualizarCategoria,
            $"Renombró la categoría '{nombreAnterior}' a '{nombre}'.");
    }

    public async Task CambiarEstadoAsync(int id, bool activo, int actorUsuarioId)
    {
        var categoria = await context.Categorias.FindAsync(id)
            ?? throw new InvalidOperationException($"Categoría {id} no encontrada.");

        categoria.Activo = activo;
        await context.SaveChangesAsync();

        await auditoria.RegistrarAsync(actorUsuarioId, TiposAccionAuditoria.CambiarEstadoCategoria,
            $"{(activo ? "Activó" : "Desactivó")} la categoría '{categoria.Nombre}'.");
    }
}
