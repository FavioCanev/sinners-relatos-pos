namespace SinnersRelatos.Web.Models;

public class Ingrediente
{
    public int Id { get; set; }
    public required string Nombre { get; set; }
    public required string UnidadMedida { get; set; }
    public decimal StockActual { get; set; }
    public decimal StockMinimo { get; set; }
    public bool Activo { get; set; } = true;

    public ICollection<RecetaProducto> Recetas { get; set; } = [];
}
