namespace SinnersRelatos.Web.Models;

public class Categoria
{
    public int Id { get; set; }
    public required string Nombre { get; set; }
    public bool Activo { get; set; } = true;

    public ICollection<Producto> Productos { get; set; } = [];
}
