using Microsoft.EntityFrameworkCore;
using SinnersRelatos.Web.Data;
using SinnersRelatos.Web.Models;
using SinnersRelatos.Web.Services.Interfaces;

namespace SinnersRelatos.Web.Services;

public class ProductoService(AppDbContext context, IAuditoriaService auditoria) : IProductoService
{
    public async Task<List<Producto>> ListarAsync(Marca? marca = null, int? categoriaId = null, bool incluirInactivos = false)
    {
        var query = context.Productos
            .Include(p => p.Categoria)
            .Include(p => p.GruposModificadores).ThenInclude(pg => pg.GrupoModificador).ThenInclude(g => g.Opciones)
            .AsQueryable();

        if (!incluirInactivos)
            query = query.Where(p => p.Activo);
        if (marca is not null)
            query = query.Where(p => p.Marca == marca);
        if (categoriaId is not null)
            query = query.Where(p => p.CategoriaId == categoriaId);

        return await query.OrderBy(p => p.Categoria.Nombre).ThenBy(p => p.Nombre).ToListAsync();
    }

    public async Task<Producto?> ObtenerPorIdAsync(int id) =>
        await context.Productos
            .Include(p => p.Categoria)
            .Include(p => p.GruposModificadores).ThenInclude(pg => pg.GrupoModificador)
            .Include(p => p.Receta).ThenInclude(r => r.Ingrediente)
            .FirstOrDefaultAsync(p => p.Id == id);

    public async Task<Producto> CrearAsync(Producto producto, IEnumerable<int> grupoModificadorIds, IReadOnlyDictionary<int, decimal> receta, int actorUsuarioId)
    {
        context.Productos.Add(producto);
        await context.SaveChangesAsync();

        foreach (var grupoId in grupoModificadorIds)
        {
            context.ProductosGruposModificadores.Add(new ProductoGrupoModificador
            {
                ProductoId = producto.Id,
                GrupoModificadorId = grupoId
            });
        }

        foreach (var (ingredienteId, cantidad) in receta)
        {
            context.RecetasProducto.Add(new RecetaProducto
            {
                ProductoId = producto.Id,
                IngredienteId = ingredienteId,
                CantidadRequerida = cantidad
            });
        }

        await context.SaveChangesAsync();

        await auditoria.RegistrarAsync(actorUsuarioId, TiposAccionAuditoria.CrearProducto,
            $"Creó el producto '{producto.Nombre}' (S/ {producto.Precio:0.00}, {producto.Marca}).");

        return producto;
    }

    public async Task ActualizarAsync(Producto producto, IEnumerable<int> grupoModificadorIds, IReadOnlyDictionary<int, decimal> receta, int actorUsuarioId)
    {
        var existente = await context.Productos
            .Include(p => p.GruposModificadores)
            .Include(p => p.Receta)
            .FirstOrDefaultAsync(p => p.Id == producto.Id)
            ?? throw new InvalidOperationException($"Producto {producto.Id} no encontrado.");

        existente.Nombre = producto.Nombre;
        existente.Descripcion = producto.Descripcion;
        existente.Precio = producto.Precio;
        existente.Marca = producto.Marca;
        existente.CategoriaId = producto.CategoriaId;

        var idsDeseados = grupoModificadorIds.ToHashSet();
        var idsActuales = existente.GruposModificadores.Select(pg => pg.GrupoModificadorId).ToHashSet();

        foreach (var vinculo in existente.GruposModificadores.Where(pg => !idsDeseados.Contains(pg.GrupoModificadorId)).ToList())
            context.ProductosGruposModificadores.Remove(vinculo);

        foreach (var grupoId in idsDeseados.Where(id => !idsActuales.Contains(id)))
            context.ProductosGruposModificadores.Add(new ProductoGrupoModificador { ProductoId = existente.Id, GrupoModificadorId = grupoId });

        foreach (var linea in existente.Receta.Where(r => !receta.ContainsKey(r.IngredienteId)).ToList())
            context.RecetasProducto.Remove(linea);

        foreach (var linea in existente.Receta)
        {
            if (receta.TryGetValue(linea.IngredienteId, out var cantidad))
                linea.CantidadRequerida = cantidad;
        }

        var idsRecetaActuales = existente.Receta.Select(r => r.IngredienteId).ToHashSet();
        foreach (var (ingredienteId, cantidad) in receta.Where(kv => !idsRecetaActuales.Contains(kv.Key)))
        {
            context.RecetasProducto.Add(new RecetaProducto
            {
                ProductoId = existente.Id,
                IngredienteId = ingredienteId,
                CantidadRequerida = cantidad
            });
        }

        await context.SaveChangesAsync();

        await auditoria.RegistrarAsync(actorUsuarioId, TiposAccionAuditoria.ActualizarProducto,
            $"Actualizó el producto '{existente.Nombre}'.");
    }

    public async Task CambiarEstadoAsync(int id, bool activo, int actorUsuarioId)
    {
        var producto = await context.Productos.FindAsync(id)
            ?? throw new InvalidOperationException($"Producto {id} no encontrado.");

        producto.Activo = activo;
        await context.SaveChangesAsync();

        await auditoria.RegistrarAsync(actorUsuarioId, TiposAccionAuditoria.CambiarEstadoProducto,
            $"{(activo ? "Activó" : "Desactivó")} el producto '{producto.Nombre}'.");
    }
}
