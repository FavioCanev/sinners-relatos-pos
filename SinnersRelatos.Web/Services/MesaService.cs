using Microsoft.EntityFrameworkCore;
using SinnersRelatos.Web.Data;
using SinnersRelatos.Web.Models;
using SinnersRelatos.Web.Services.Interfaces;

namespace SinnersRelatos.Web.Services;

public class MesaService(IDbContextFactory<AppDbContext> contextFactory) : IMesaService
{
    public async Task<List<MesaEstado>> ListarPorMarcaAsync(Marca marca)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        var mesas = await context.Mesas
            .Where(m => m.Marca == marca && m.Activo)
            .OrderBy(m => m.Tipo)
            .ThenBy(m => m.Numero)
            .ToListAsync();

        var pedidosActivosPorMesa = await context.PedidosMesas
            .Where(pm => pm.Mesa.Marca == marca && pm.Pedido.Estado == EstadoPedido.Pendiente)
            .Select(pm => new
            {
                pm.MesaId,
                pm.PedidoId,
                TieneItems = pm.Pedido.Detalles.Any(),
                OcupadaDesde = pm.Pedido.Detalles.Any() ? pm.Pedido.Detalles.Min(d => d.FechaCreacion) : (DateTime?)null
            })
            .ToListAsync();

        var mapaPedidos = pedidosActivosPorMesa
            .GroupBy(x => x.MesaId)
            .ToDictionary(g => g.Key, g => g.First());

        return mesas.Select(m =>
        {
            mapaPedidos.TryGetValue(m.Id, out var info);
            return new MesaEstado
            {
                Mesa = m,
                Ocupada = info?.TieneItems ?? false,
                PedidoId = info?.PedidoId,
                OcupadaDesde = info?.OcupadaDesde
            };
        }).ToList();
    }

    public async Task<Mesa?> ObtenerPorIdAsync(int id)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        return await context.Mesas.FirstOrDefaultAsync(m => m.Id == id);
    }
}
