using Microsoft.EntityFrameworkCore;
using SinnersRelatos.Web.Data;
using SinnersRelatos.Web.Models;
using SinnersRelatos.Web.Services.Interfaces;

namespace SinnersRelatos.Web.Services;

public class AuditoriaService(IDbContextFactory<AppDbContext> contextFactory) : IAuditoriaService
{
    public async Task RegistrarAsync(int? usuarioId, string tipoAccion, string detalle)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        context.LogsAuditoria.Add(new LogAuditoria
        {
            UsuarioId = usuarioId,
            TipoAccion = tipoAccion,
            Detalle = detalle
        });
        await context.SaveChangesAsync();
    }

    public async Task<List<LogAuditoria>> ListarAsync(
        DateTime? desde = null,
        DateTime? hasta = null,
        int? usuarioId = null,
        string? tipoAccion = null)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        var query = context.LogsAuditoria
            .Include(l => l.Usuario).ThenInclude(u => u!.Empleado)
            .AsQueryable();

        if (desde is not null)
            query = query.Where(l => l.FechaHora >= desde);
        if (hasta is not null)
            query = query.Where(l => l.FechaHora <= hasta);
        if (usuarioId is not null)
            query = query.Where(l => l.UsuarioId == usuarioId);
        if (!string.IsNullOrWhiteSpace(tipoAccion))
            query = query.Where(l => l.TipoAccion == tipoAccion);

        return await query.OrderByDescending(l => l.FechaHora).Take(500).ToListAsync();
    }

    public async Task<List<string>> ListarTiposAccionAsync()
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        return await context.LogsAuditoria.Select(l => l.TipoAccion).Distinct().OrderBy(t => t).ToListAsync();
    }
}
