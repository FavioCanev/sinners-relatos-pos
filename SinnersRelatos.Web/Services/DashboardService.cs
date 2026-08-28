using Microsoft.EntityFrameworkCore;
using SinnersRelatos.Web.Data;
using SinnersRelatos.Web.Models;
using SinnersRelatos.Web.Services.Interfaces;

namespace SinnersRelatos.Web.Services;

public class DashboardService(AppDbContext context) : IDashboardService
{
    public async Task<ResumenVentas> ObtenerResumenAsync(DateTime desde, DateTime hasta)
    {
        var pedidos = await context.Pedidos
            .Where(p => p.Estado == EstadoPedido.Cerrado && p.FechaCreacion >= desde && p.FechaCreacion <= hasta)
            .Include(p => p.Detalles).ThenInclude(d => d.Producto)
            .Include(p => p.Detalles).ThenInclude(d => d.Modificadores)
            .AsSplitQuery()
            .ToListAsync();

        var ingredientesBajoStock = await context.Ingredientes
            .CountAsync(i => i.Activo && i.StockMinimo > 0 && i.StockActual <= i.StockMinimo);

        decimal TotalDetalle(DetallePedido d) =>
            d.Cantidad * (d.PrecioUnitario + d.Modificadores.Sum(m => m.PrecioAdicional));

        var totalVentas = pedidos.Sum(p => p.Detalles.Sum(TotalDetalle));
        var cantidadPedidos = pedidos.Count;

        var ventasPorDia = pedidos
            .GroupBy(p => DateOnly.FromDateTime(p.FechaCreacion))
            .Select(g => new VentaPorDia { Fecha = g.Key, Total = g.Sum(p => p.Detalles.Sum(TotalDetalle)) })
            .OrderBy(v => v.Fecha)
            .ToList();

        var todosLosDetalles = pedidos.SelectMany(p => p.Detalles).ToList();

        var topProductos = todosLosDetalles
            .GroupBy(d => d.Producto)
            .Select(g => new ProductoMasPedido
            {
                Nombre = g.Key.Nombre,
                Marca = g.Key.Marca,
                CantidadVendida = g.Sum(d => d.Cantidad)
            })
            .OrderByDescending(p => p.CantidadVendida)
            .Take(8)
            .ToList();

        var ventasPorMarca = todosLosDetalles
            .GroupBy(d => d.Producto.Marca)
            .Select(g => new VentaPorMarca { Marca = g.Key, Total = g.Sum(TotalDetalle) })
            .OrderBy(v => v.Marca)
            .ToList();

        return new ResumenVentas
        {
            TotalVentas = totalVentas,
            CantidadPedidos = cantidadPedidos,
            TicketPromedio = cantidadPedidos == 0 ? 0 : totalVentas / cantidadPedidos,
            IngredientesBajoStock = ingredientesBajoStock,
            VentasPorDia = ventasPorDia,
            TopProductos = topProductos,
            VentasPorMarca = ventasPorMarca
        };
    }

    public async Task<List<CategoriaStock>> ObtenerStockPorCategoriaAsync()
    {
        var productos = await context.Productos
            .Where(p => p.Activo && p.Receta.Any())
            .Include(p => p.Categoria)
            .Include(p => p.Receta).ThenInclude(r => r.Ingrediente)
            .ToListAsync();

        return productos
            .Select(p => new
            {
                p.Nombre,
                p.Marca,
                Categoria = p.Categoria.Nombre,
                Disponible = p.Receta.Min(r => (int)Math.Floor(r.Ingrediente.StockActual / r.CantidadRequerida))
            })
            .GroupBy(p => new { p.Categoria, p.Marca })
            .Select(g => new CategoriaStock
            {
                CategoriaNombre = g.Key.Categoria,
                Marca = g.Key.Marca,
                Productos = g.Select(p => new StockProducto { Nombre = p.Nombre, CantidadDisponible = p.Disponible })
                    .OrderBy(p => p.Nombre)
                    .ToList()
            })
            .OrderBy(c => c.Marca)
            .ThenBy(c => c.CategoriaNombre)
            .ToList();
    }
}
