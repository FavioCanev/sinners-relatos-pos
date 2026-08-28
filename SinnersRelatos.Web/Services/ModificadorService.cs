using Microsoft.EntityFrameworkCore;
using SinnersRelatos.Web.Data;
using SinnersRelatos.Web.Models;
using SinnersRelatos.Web.Services.Interfaces;

namespace SinnersRelatos.Web.Services;

public class ModificadorService(AppDbContext context, IAuditoriaService auditoria) : IModificadorService
{
    public async Task<List<GrupoModificador>> ListarGruposAsync() =>
        await context.GruposModificadores
            .Include(g => g.Opciones).ThenInclude(o => o.Recetas).ThenInclude(r => r.Ingrediente)
            .OrderBy(g => g.Nombre)
            .ToListAsync();

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

    public async Task AsignarIngredienteAsync(int opcionId, int ingredienteId, decimal cantidadRequerida, int actorUsuarioId)
    {
        var opcion = await context.OpcionesModificadores.FindAsync(opcionId)
            ?? throw new InvalidOperationException($"Opción {opcionId} no encontrada.");
        var ingrediente = await context.Ingredientes.FindAsync(ingredienteId)
            ?? throw new InvalidOperationException($"Ingrediente {ingredienteId} no encontrado.");

        var receta = await context.RecetasOpcionModificador
            .FirstOrDefaultAsync(r => r.OpcionModificadorId == opcionId && r.IngredienteId == ingredienteId);

        if (receta is null)
        {
            context.RecetasOpcionModificador.Add(new RecetaOpcionModificador
            {
                OpcionModificadorId = opcionId,
                IngredienteId = ingredienteId,
                CantidadRequerida = cantidadRequerida
            });
        }
        else
        {
            receta.CantidadRequerida = cantidadRequerida;
        }

        await context.SaveChangesAsync();

        await auditoria.RegistrarAsync(actorUsuarioId, TiposAccionAuditoria.AsignarRecetaOpcionModificador,
            $"Asignó {cantidadRequerida} {ingrediente.UnidadMedida} de '{ingrediente.Nombre}' a la opción '{opcion.Nombre}'.");
    }

    public async Task QuitarIngredienteAsync(int opcionId, int ingredienteId, int actorUsuarioId)
    {
        var receta = await context.RecetasOpcionModificador
            .Include(r => r.Ingrediente)
            .Include(r => r.OpcionModificador)
            .FirstOrDefaultAsync(r => r.OpcionModificadorId == opcionId && r.IngredienteId == ingredienteId)
            ?? throw new InvalidOperationException("La receta no existe.");

        var nombreIngrediente = receta.Ingrediente.Nombre;
        var nombreOpcion = receta.OpcionModificador.Nombre;

        context.RecetasOpcionModificador.Remove(receta);
        await context.SaveChangesAsync();

        await auditoria.RegistrarAsync(actorUsuarioId, TiposAccionAuditoria.QuitarRecetaOpcionModificador,
            $"Quitó '{nombreIngrediente}' de la opción '{nombreOpcion}'.");
    }
}
