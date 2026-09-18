using Microsoft.EntityFrameworkCore;
using SinnersRelatos.Web.Data;
using SinnersRelatos.Web.Models;
using SinnersRelatos.Web.Services.Interfaces;

namespace SinnersRelatos.Web.Services;

public class EmpleadoService(IDbContextFactory<AppDbContext> contextFactory) : IEmpleadoService
{
    public async Task<Empleado?> ObtenerPorIdAsync(int id)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        return await context.Empleados.FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task ActualizarAsync(Empleado empleado)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        context.Empleados.Update(empleado);
        await context.SaveChangesAsync();
    }
}
