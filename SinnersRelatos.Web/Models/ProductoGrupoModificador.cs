namespace SinnersRelatos.Web.Models;

public class ProductoGrupoModificador
{
    public int ProductoId { get; set; }
    public Producto Producto { get; set; } = null!;

    public int GrupoModificadorId { get; set; }
    public GrupoModificador GrupoModificador { get; set; } = null!;
}
