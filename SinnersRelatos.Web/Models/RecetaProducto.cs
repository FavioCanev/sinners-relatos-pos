namespace SinnersRelatos.Web.Models;

public class RecetaProducto
{
    public int ProductoId { get; set; }
    public Producto Producto { get; set; } = null!;

    public int IngredienteId { get; set; }
    public Ingrediente Ingrediente { get; set; } = null!;

    public decimal CantidadRequerida { get; set; }
}
