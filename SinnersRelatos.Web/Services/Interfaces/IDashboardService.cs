using SinnersRelatos.Web.Models;

namespace SinnersRelatos.Web.Services.Interfaces;

public class ResumenVentas
{
    public decimal TotalVentas { get; set; }
    public int CantidadPedidos { get; set; }
    public decimal TicketPromedio { get; set; }
    public int IngredientesBajoStock { get; set; }

    public List<VentaPorDia> VentasPorDia { get; set; } = [];
    public List<ProductoMasPedido> TopProductos { get; set; } = [];
    public List<VentaPorMarca> VentasPorMarca { get; set; } = [];
}

public class VentaPorDia
{
    public DateOnly Fecha { get; set; }
    public decimal Total { get; set; }
}

public class ProductoMasPedido
{
    public required string Nombre { get; set; }
    public Marca Marca { get; set; }
    public int CantidadVendida { get; set; }
}

public class VentaPorMarca
{
    public Marca Marca { get; set; }
    public decimal Total { get; set; }
}

public class StockProducto
{
    public required string Nombre { get; set; }
    public int CantidadDisponible { get; set; }
}

public class CategoriaStock
{
    public required string CategoriaNombre { get; set; }
    public Marca Marca { get; set; }
    public List<StockProducto> Productos { get; set; } = [];
}

public interface IDashboardService
{
    Task<ResumenVentas> ObtenerResumenAsync(DateTime desde, DateTime hasta);
    Task<List<CategoriaStock>> ObtenerStockPorCategoriaAsync();
}
