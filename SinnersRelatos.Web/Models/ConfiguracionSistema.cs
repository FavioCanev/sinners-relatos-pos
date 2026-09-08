namespace SinnersRelatos.Web.Models;

// Fila única (Id = 1) con interruptores globales de la aplicación.
public class ConfiguracionSistema
{
    public int Id { get; set; }
    public bool ModoPruebasActivo { get; set; }
    public DateTime? ModoPruebasActivadoEn { get; set; }
    public int? ModoPruebasActivadoPorUsuarioId { get; set; }
}
