namespace SinnersRelatos.Web.Models;

public class RecetaOpcionModificador
{
    public int OpcionModificadorId { get; set; }
    public OpcionModificador OpcionModificador { get; set; } = null!;

    public int IngredienteId { get; set; }
    public Ingrediente Ingrediente { get; set; } = null!;

    public decimal CantidadRequerida { get; set; }
}
