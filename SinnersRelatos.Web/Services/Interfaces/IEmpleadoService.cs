using SinnersRelatos.Web.Models;

namespace SinnersRelatos.Web.Services.Interfaces;

public interface IEmpleadoService
{
    Task<Empleado?> ObtenerPorIdAsync(int id);
    Task ActualizarAsync(Empleado empleado);
}
