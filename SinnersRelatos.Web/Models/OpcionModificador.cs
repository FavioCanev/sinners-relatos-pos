namespace SinnersRelatos.Web.Models;

public class OpcionModificador
{
    public int Id { get; set; }
    public required string Nombre { get; set; }
    public decimal PrecioAdicional { get; set; }
    public bool Activo { get; set; } = true;

    public int GrupoModificadorId { get; set; }
    public GrupoModificador GrupoModificador { get; set; } = null!;
}
