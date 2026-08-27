using Microsoft.EntityFrameworkCore;
using SinnersRelatos.Web.Data;
using SinnersRelatos.Web.Models;
using SinnersRelatos.Web.Services.Interfaces;

namespace SinnersRelatos.Web.Services;

public class MesaService(AppDbContext context) : IMesaService
{
    public async Task<List<MesaEstado>> ListarPorMarcaAsync(Marca marca)
    {
        var mesas = await context.Mesas
            .Where(m => m.Marca == marca && m.Activo)
            .OrderBy(m => m.Tipo)
            .ThenBy(m => m.Numero)
            .ToListAsync();

        var pedidosActivosPorMesa = await context.PedidosMesas
            .Where(pm => pm.Mesa.Marca == marca && pm.Pedido.Estado == EstadoPedido.Pendiente)
            .Select(pm => new { pm.MesaId, pm.PedidoId })
            .ToListAsync();

        var mapaPedidos = pedidosActivosPorMesa
            .GroupBy(x => x.MesaId)
            .ToDictionary(g => g.Key, g => g.First().PedidoId);

        return mesas.Select(m => new MesaEstado
        {
            Mesa = m,
            Ocupada = mapaPedidos.ContainsKey(m.Id),
            PedidoId = mapaPedidos.GetValueOrDefault(m.Id)
        }).ToList();
    }

    public async Task<Mesa?> ObtenerPorIdAsync(int id) =>
        await context.Mesas.FirstOrDefaultAsync(m => m.Id == id);
}
