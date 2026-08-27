namespace SinnersRelatos.Web.Models;

public class Producto
{
    public int Id { get; set; }
    public required string Nombre { get; set; }
    public string? Descripcion { get; set; }
    public decimal Precio { get; set; }
    public Marca Marca { get; set; }
    public DestinoPreparacion DestinoPreparacion { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.Now;

    public int CategoriaId { get; set; }
    public Categoria Categoria { get; set; } = null!;

    public ICollection<ProductoGrupoModificador> GruposModificadores { get; set; } = [];
    public ICollection<RecetaProducto> Receta { get; set; } = [];
}
