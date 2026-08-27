namespace SinnersRelatos.Web.Models;

public class Empleado
{
    public int Id { get; set; }
    public required string Nombres { get; set; }
    public required string Apellidos { get; set; }
    public string? Telefono { get; set; }
    public DateTime FechaContratacion { get; set; } = DateTime.Now;
    public bool Activo { get; set; } = true;

    public Usuario? Usuario { get; set; }
}
