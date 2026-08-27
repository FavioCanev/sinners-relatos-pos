namespace SinnersRelatos.Web.Models;

public class Usuario
{
    public int Id { get; set; }
    public required string NombreUsuario { get; set; }
    public required string PasswordHash { get; set; }
    public RolUsuario Rol { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.Now;

    public string? PreguntaSeguridad { get; set; }
    public string? RespuestaSeguridadHash { get; set; }

    public int EmpleadoId { get; set; }
    public Empleado Empleado { get; set; } = null!;
}
