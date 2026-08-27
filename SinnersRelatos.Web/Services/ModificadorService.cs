using Microsoft.EntityFrameworkCore;
using SinnersRelatos.Web.Data;
using SinnersRelatos.Web.Models;
using SinnersRelatos.Web.Services.Interfaces;

namespace SinnersRelatos.Web.Services;

public class ModificadorService(AppDbContext context, IAuditoriaService auditoria) : IModificadorService
{
    public async Task<List<GrupoModificador>> ListarGruposAsync() =>
        await context.GruposModificadores.Include(g => g.Opciones).OrderBy(g => g.Nombre).ToListAsync();

    public async Task<GrupoModificador?> ObtenerGrupoPorIdAsync(int id) =>
        await context.GruposModificadores.Include(g => g.Opciones).FirstOrDefaultAsync(g => g.Id == id);

    public async Task<GrupoModificador> CrearGrupoAsync(string nombre, bool esObligatorio, bool permiteMultiple, int actorUsuarioId)
    {
        var enUso = await context.GruposModificadores.AnyAsync(g => g.Nombre == nombre);
        if (enUso)
            throw new InvalidOperationException($"El grupo modificador '{nombre}' ya existe.");

        var grupo = new GrupoModificador { Nombre = nombre, EsObligatorio = esObligatorio, PermiteMultiple = permiteMultiple };
        context.GruposModificadores.Add(grupo);
        await context.SaveChangesAsync();

        await auditoria.RegistrarAsync(actorUsuarioId, TiposAccionAuditoria.CrearGrupoModificador, $"Creó el grupo modificador '{nombre}'.");
        return grupo;
    }

    public async Task ActualizarGrupoAsync(int id, string nombre, bool esObligatorio, bool permiteMultiple, int actorUsuarioId)
    {
        var grupo = await context.GruposModificadores.FindAsync(id)
            ?? throw new InvalidOperationException($"Grupo modificador {id} no encontrado.");

        var enUso = await context.GruposModificadores.AnyAsync(g => g.Nombre == nombre && g.Id != id);
        if (enUso)
            throw new InvalidOperationException($"El grupo modificador '{nombre}' ya existe.");

        grupo.Nombre = nombre;
        grupo.EsObligatorio = esObligatorio;
        grupo.PermiteMultiple = permiteMultiple;
        await context.SaveChangesAsync();

        await auditoria.RegistrarAsync(actorUsuarioId, TiposAccionAuditoria.ActualizarGrupoModificador, $"Actualizó el grupo modificador '{nombre}'.");
    }

    public async Task EliminarGrupoAsync(int id, int actorUsuarioId)
    {
        var enUso = await context.ProductosGruposModificadores.AnyAsync(pg => pg.GrupoModificadorId == id);
        if (enUso)
            throw new InvalidOperationException("No se puede eliminar: el grupo está asignado a uno o más productos.");

        var grupo = await context.GruposModificadores.FindAsync(id)
            ?? throw new InvalidOperationException($"Grupo modificador {id} no encontrado.");

        context.GruposModificadores.Remove(grupo);
        await context.SaveChangesAsync();

        await auditoria.RegistrarAsync(actorUsuarioId, TiposAccionAuditoria.EliminarGrupoModificador, $"Eliminó el grupo modificador '{grupo.Nombre}'.");
    }

    public async Task<OpcionModificador> AgregarOpcionAsync(int grupoId, string nombre, decimal precioAdicional, int actorUsuarioId)
    {
        var opcion = new OpcionModificador { GrupoModificadorId = grupoId, Nombre = nombre, PrecioAdicional = precioAdicional };
        context.OpcionesModificadores.Add(opcion);
        await context.SaveChangesAsync();

        await auditoria.RegistrarAsync(actorUsuarioId, TiposAccionAuditoria.CrearOpcionModificador, $"Agregó la opción '{nombre}'.");
        return opcion;
    }

    public async Task ActualizarOpcionAsync(int opcionId, string nombre, decimal precioAdicional, int actorUsuarioId)
    {
        var opcion = await context.OpcionesModificadores.FindAsync(opcionId)
            ?? throw new InvalidOperationException($"Opción {opcionId} no encontrada.");

        opcion.Nombre = nombre;
        opcion.PrecioAdicional = precioAdicional;
        await context.SaveChangesAsync();

        await auditoria.RegistrarAsync(actorUsuarioId, TiposAccionAuditoria.ActualizarOpcionModificador, $"Actualizó la opción '{nombre}'.");
    }

    public async Task CambiarEstadoOpcionAsync(int opcionId, bool activo, int actorUsuarioId)
    {
        var opcion = await context.OpcionesModificadores.FindAsync(opcionId)
            ?? throw new InvalidOperationException($"Opción {opcionId} no encontrada.");

        opcion.Activo = activo;
        await context.SaveChangesAsync();

        await auditoria.RegistrarAsync(actorUsuarioId, TiposAccionAuditoria.CambiarEstadoOpcionModificador,
            $"{(activo ? "Activó" : "Desactivó")} la opción '{opcion.Nombre}'.");
    }
}
