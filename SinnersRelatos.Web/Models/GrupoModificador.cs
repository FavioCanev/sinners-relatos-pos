namespace SinnersRelatos.Web.Models;

public class GrupoModificador
{
    public int Id { get; set; }
    public required string Nombre { get; set; }
    public bool EsObligatorio { get; set; }
    public bool PermiteMultiple { get; set; }

    public ICollection<OpcionModificador> Opciones { get; set; } = [];
    public ICollection<ProductoGrupoModificador> Productos { get; set; } = [];
}
