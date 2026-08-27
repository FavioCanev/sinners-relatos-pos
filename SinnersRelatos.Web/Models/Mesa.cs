namespace SinnersRelatos.Web.Models;

public class Mesa
{
    public int Id { get; set; }
    public Marca Marca { get; set; }
    public TipoMesa Tipo { get; set; } = TipoMesa.Mesa;
    public int Numero { get; set; }
    public bool Activo { get; set; } = true;

    public ICollection<PedidoMesa> Pedidos { get; set; } = [];
}
