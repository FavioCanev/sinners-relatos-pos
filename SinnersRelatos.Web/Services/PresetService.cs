using Microsoft.EntityFrameworkCore;
using SinnersRelatos.Web.Data;
using SinnersRelatos.Web.Models;
using SinnersRelatos.Web.Services.Interfaces;

namespace SinnersRelatos.Web.Services;

// A diferencia del resto del catálogo (Categorías, Productos, Modificadores), cualquier usuario
// logueado puede crear/editar combos, no solo el administrador: viven dentro de Tomar Pedido
// (pestaña "Combos"), pensados para que el propio mesero arme atajos mientras atiende.
public class PresetService(IDbContextFactory<AppDbContext> contextFactory, IAuditoriaService auditoria) : IPresetService
{
    public async Task<List<Preset>> ListarAsync(bool incluirInactivos = false)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        var query = context.Presets
            .Include(p => p.Productos).ThenInclude(pp => pp.Producto)
            .AsQueryable();

        if (!incluirInactivos)
            query = query.Where(p => p.Activo);

        return await query.OrderBy(p => p.Nombre).ToListAsync();
    }

    public async Task<Preset?> ObtenerPorIdAsync(int id)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        return await context.Presets
            .Include(p => p.Productos).ThenInclude(pp => pp.Producto)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Preset> CrearAsync(string nombre, IReadOnlyDictionary<int, int> productosCantidades, int actorUsuarioId)
    {
        if (productosCantidades.Count == 0)
            throw new InvalidOperationException("Selecciona al menos un producto para el combo.");

        await using var context = await contextFactory.CreateDbContextAsync();

        var enUso = await context.Presets.AnyAsync(p => p.Nombre == nombre);
        if (enUso)
            throw new InvalidOperationException($"Ya existe un combo llamado '{nombre}'.");

        var preset = new Preset { Nombre = nombre };
        foreach (var (productoId, cantidad) in productosCantidades)
            preset.Productos.Add(new PresetProducto { ProductoId = productoId, Cantidad = cantidad });

        context.Presets.Add(preset);
        await context.SaveChangesAsync();

        await auditoria.RegistrarAsync(actorUsuarioId, TiposAccionAuditoria.CrearPreset,
            $"Creó el combo '{nombre}' con {productosCantidades.Count} producto(s).");

        return preset;
    }

    public async Task ActualizarAsync(int id, string nombre, IReadOnlyDictionary<int, int> productosCantidades, int actorUsuarioId)
    {
        if (productosCantidades.Count == 0)
            throw new InvalidOperationException("Selecciona al menos un producto para el combo.");

        await using var context = await contextFactory.CreateDbContextAsync();

        var preset = await context.Presets
            .Include(p => p.Productos)
            .FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new InvalidOperationException($"Combo {id} no encontrado.");

        var enUso = await context.Presets.AnyAsync(p => p.Nombre == nombre && p.Id != id);
        if (enUso)
            throw new InvalidOperationException($"Ya existe un combo llamado '{nombre}'.");

        preset.Nombre = nombre;

        foreach (var linea in preset.Productos.Where(pp => !productosCantidades.ContainsKey(pp.ProductoId)).ToList())
            context.PresetProductos.Remove(linea);

        foreach (var linea in preset.Productos)
        {
            if (productosCantidades.TryGetValue(linea.ProductoId, out var cantidad))
                linea.Cantidad = cantidad;
        }

        var idsActuales = preset.Productos.Select(pp => pp.ProductoId).ToHashSet();
        foreach (var (productoId, cantidad) in productosCantidades.Where(kv => !idsActuales.Contains(kv.Key)))
            context.PresetProductos.Add(new PresetProducto { PresetId = preset.Id, ProductoId = productoId, Cantidad = cantidad });

        await context.SaveChangesAsync();

        await auditoria.RegistrarAsync(actorUsuarioId, TiposAccionAuditoria.ActualizarPreset, $"Actualizó el combo '{nombre}'.");
    }

    public async Task CambiarEstadoAsync(int id, bool activo, int actorUsuarioId)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        var preset = await context.Presets.FindAsync(id)
            ?? throw new InvalidOperationException($"Combo {id} no encontrado.");

        preset.Activo = activo;
        await context.SaveChangesAsync();

        await auditoria.RegistrarAsync(actorUsuarioId, TiposAccionAuditoria.CambiarEstadoPreset,
            $"{(activo ? "Activó" : "Desactivó")} el combo '{preset.Nombre}'.");
    }
}
