using Microsoft.EntityFrameworkCore;
using SinnersRelatos.Web.Data;
using SinnersRelatos.Web.Models;
using SinnersRelatos.Web.Services.Interfaces;

namespace SinnersRelatos.Web.Services;

public class DashboardService(AppDbContext context) : IDashboardService
{
    public async Task<ResumenVentas> ObtenerResumenAsync(DateTime desde, DateTime hasta, Marca? marca = null)
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

        // Un mismo pedido puede incluir productos de ambas marcas (ej. una mesa de Sinners
        // que también pide algo de Relatos), así que el filtro se aplica por línea de
        // detalle y no descartando el pedido completo.
        var detallesFiltrados = pedidos
            .SelectMany(p => p.Detalles.Select(d => (Pedido: p, Detalle: d)))
            .Where(x => marca is null || x.Detalle.Producto.Marca == marca)
            .ToList();

        var totalVentas = detallesFiltrados.Sum(x => TotalDetalle(x.Detalle));
        var cantidadPedidos = detallesFiltrados.Select(x => x.Pedido.Id).Distinct().Count();

        var ventasPorDia = detallesFiltrados
            .GroupBy(x => DateOnly.FromDateTime(x.Pedido.FechaCreacion))
            .Select(g => new VentaPorDia { Fecha = g.Key, Total = g.Sum(x => TotalDetalle(x.Detalle)) })
            .OrderBy(v => v.Fecha)
            .ToList();

        var topProductos = detallesFiltrados
            .Select(x => x.Detalle)
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

        // Se calcula siempre sobre todas las marcas (sin aplicar el filtro) para que la
        // comparación Sinners vs. Relatos siga siendo útil aunque se esté filtrando.
        var ventasPorMarca = pedidos
            .SelectMany(p => p.Detalles)
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

    public async Task<List<CategoriaStock>> ObtenerStockPorCategoriaAsync(Marca? marca = null)
    {
        var productos = await context.Productos
            .Where(p => p.Activo && p.Receta.Any() && (marca == null || p.Marca == marca))
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
