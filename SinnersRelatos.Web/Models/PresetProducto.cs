namespace SinnersRelatos.Web.Models;

public class PresetProducto
{
    public int PresetId { get; set; }
    public Preset Preset { get; set; } = null!;

    public int ProductoId { get; set; }
    public Producto Producto { get; set; } = null!;

    public int Cantidad { get; set; } = 1;
}
