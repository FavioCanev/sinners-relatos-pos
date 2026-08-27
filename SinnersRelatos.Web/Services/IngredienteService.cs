using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SinnersRelatos.Web.Data;
using SinnersRelatos.Web.Hubs;
using SinnersRelatos.Web.Models;
using SinnersRelatos.Web.Services.Interfaces;

namespace SinnersRelatos.Web.Services;

public class IngredienteService(AppDbContext context, IHubContext<ComandaHub> hub, IAuditoriaService auditoria) : IIngredienteService
{
    public async Task<List<Ingrediente>> ListarAsync(bool incluirInactivos = false)
    {
        var query = context.Ingredientes.AsQueryable();
        if (!incluirInactivos)
            query = query.Where(i => i.Activo);

        return await query.OrderBy(i => i.Nombre).ToListAsync();
    }

    public async Task<Ingrediente?> ObtenerPorIdAsync(int id) =>
        await context.Ingredientes.FirstOrDefaultAsync(i => i.Id == id);

    public async Task<Ingrediente> CrearAsync(string nombre, string unidadMedida, decimal stockInicial, decimal stockMinimo, int actorUsuarioId)
    {
        var enUso = await context.Ingredientes.AnyAsync(i => i.Nombre == nombre);
        if (enUso)
            throw new InvalidOperationException($"El ingrediente '{nombre}' ya existe.");

        var ingrediente = new Ingrediente
        {
            Nombre = nombre,
            UnidadMedida = unidadMedida,
            StockActual = stockInicial,
            StockMinimo = stockMinimo
        };

        context.Ingredientes.Add(ingrediente);
        await context.SaveChangesAsync();

        await auditoria.RegistrarAsync(actorUsuarioId, TiposAccionAuditoria.CrearIngrediente,
            $"Creó el ingrediente '{nombre}' con stock inicial {stockInicial} {unidadMedida}.");

        return ingrediente;
    }

    public async Task ActualizarAsync(int id, string nombre, string unidadMedida, decimal stockMinimo, int actorUsuarioId)
    {
        var ingrediente = await context.Ingredientes.FindAsync(id)
            ?? throw new InvalidOperationException($"Ingrediente {id} no encontrado.");

        var enUso = await context.Ingredientes.AnyAsync(i => i.Nombre == nombre && i.Id != id);
        if (enUso)
            throw new InvalidOperationException($"El ingrediente '{nombre}' ya existe.");

        ingrediente.Nombre = nombre;
        ingrediente.UnidadMedida = unidadMedida;
        ingrediente.StockMinimo = stockMinimo;
        await context.SaveChangesAsync();

        await auditoria.RegistrarAsync(actorUsuarioId, TiposAccionAuditoria.ActualizarIngrediente, $"Actualizó el ingrediente '{nombre}'.");
    }

    public async Task AjustarStockAsync(int id, decimal cantidad, int actorUsuarioId)
    {
        var ingrediente = await context.Ingredientes.FindAsync(id)
            ?? throw new InvalidOperationException($"Ingrediente {id} no encontrado.");

        var stockAnterior = ingrediente.StockActual;
        ingrediente.StockActual += cantidad;
        await context.SaveChangesAsync();

        await auditoria.RegistrarAsync(actorUsuarioId, TiposAccionAuditoria.AjustarStock,
            $"Ajustó el stock de '{ingrediente.Nombre}' en {(cantidad >= 0 ? "+" : "")}{cantidad} {ingrediente.UnidadMedida} " +
            $"({stockAnterior} → {ingrediente.StockActual}).");

        await hub.Clients.All.SendAsync(ComandaEventos.AlertaStockActualizada);
    }

    public async Task CambiarEstadoAsync(int id, bool activo, int actorUsuarioId)
    {
        var ingrediente = await context.Ingredientes.FindAsync(id)
            ?? throw new InvalidOperationException($"Ingrediente {id} no encontrado.");

        ingrediente.Activo = activo;
        await context.SaveChangesAsync();

        await auditoria.RegistrarAsync(actorUsuarioId, TiposAccionAuditoria.CambiarEstadoIngrediente,
            $"{(activo ? "Activó" : "Desactivó")} el ingrediente '{ingrediente.Nombre}'.");
    }

    public async Task<List<Ingrediente>> ListarBajoStockAsync() =>
        await context.Ingredientes
            .Where(i => i.Activo && i.StockMinimo > 0 && i.StockActual <= i.StockMinimo)
            .OrderBy(i => i.Nombre)
            .ToListAsync();
}
