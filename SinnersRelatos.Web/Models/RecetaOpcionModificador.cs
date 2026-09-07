namespace SinnersRelatos.Web.Models;

public class RecetaOpcionModificador
{
    public int Id { get; set; }

    public int OpcionModificadorId { get; set; }
    public OpcionModificador OpcionModificador { get; set; } = null!;

    public int IngredienteId { get; set; }
    public Ingrediente Ingrediente { get; set; } = null!;

    // Null = cantidad por defecto para cualquier producto que use esta opción.
    // Con valor = sobrescribe la cantidad por defecto solo para ese producto
    // (ej. "Leche Entera" descuenta 240ml en Capuccino pero 180ml en Moccacino).
    public int? ProductoId { get; set; }
    public Producto? Producto { get; set; }

    public decimal CantidadRequerida { get; set; }
}
