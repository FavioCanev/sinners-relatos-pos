using Microsoft.EntityFrameworkCore;
using SinnersRelatos.Web.Data;
using SinnersRelatos.Web.Models;
using SinnersRelatos.Web.Services.Interfaces;

namespace SinnersRelatos.Web.Services;

public class EmpleadoService(AppDbContext context) : IEmpleadoService
{
    public async Task<Empleado?> ObtenerPorIdAsync(int id) =>
        await context.Empleados.FirstOrDefaultAsync(e => e.Id == id);

    public async Task ActualizarAsync(Empleado empleado)
    {
        context.Empleados.Update(empleado);
        await context.SaveChangesAsync();
    }
}
