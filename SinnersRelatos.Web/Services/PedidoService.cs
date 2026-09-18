using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SinnersRelatos.Web.Data;
using SinnersRelatos.Web.Hubs;
using SinnersRelatos.Web.Models;
using SinnersRelatos.Web.Services.Interfaces;

namespace SinnersRelatos.Web.Services;

public class PedidoService(IDbContextFactory<AppDbContext> contextFactory, IHubContext<ComandaHub> hub, IAuditoriaService auditoria) : IPedidoService
{
    public async Task<Pedido> ObtenerOCrearPedidoAsync(int mesaId, int usuarioId)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        var existente = await context.PedidosMesas
            .Where(pm => pm.MesaId == mesaId && pm.Pedido.Estado == EstadoPedido.Pendiente)
            .Select(pm => pm.Pedido)
            .FirstOrDefaultAsync();

        if (existente is not null)
            return existente;

        var pedido = new Pedido { UsuarioId = usuarioId };
        pedido.Mesas.Add(new PedidoMesa { MesaId = mesaId });

        context.Pedidos.Add(pedido);
        await context.SaveChangesAsync();
        return pedido;
    }

    public async Task<Pedido?> ObtenerConDetalleAsync(int pedidoId)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        return await context.Pedidos
            .AsNoTracking()
            .Include(p => p.Usuario)
            .Include(p => p.Mesas).ThenInclude(pm => pm.Mesa)
            .Include(p => p.Detalles).ThenInclude(d => d.Producto)
            .Include(p => p.Detalles).ThenInclude(d => d.Modificadores).ThenInclude(m => m.OpcionModificador)
            .FirstOrDefaultAsync(p => p.Id == pedidoId);
    }

    public async Task<Dictionary<int, bool>> VerificarDisponibilidadAsync(IEnumerable<int> productoIds)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        var ids = productoIds.ToList();
        var recetas = await context.RecetasProducto
            .Include(r => r.Ingrediente)
            .Where(r => ids.Contains(r.ProductoId))
            .ToListAsync();

        return ids.ToDictionary(
            id => id,
            id => recetas.Where(r => r.ProductoId == id).All(r => r.Ingrediente.StockActual >= r.CantidadRequerida));
    }

    public async Task<Dictionary<int, bool>> VerificarDisponibilidadOpcionesAsync(int productoId, IEnumerable<int> opcionIds)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        var ids = opcionIds.ToList();
        var opciones = await context.OpcionesModificadores
            .Include(o => o.Recetas).ThenInclude(r => r.Ingrediente)
            .Where(o => ids.Contains(o.Id))
            .ToListAsync();

        var resultado = new Dictionary<int, bool>();
        foreach (var id in ids)
        {
            var opcion = opciones.FirstOrDefault(o => o.Id == id);
            var aplicables = opcion is null ? [] : RecetasAplicables(opcion, productoId);
            resultado[id] = aplicables.All(r => r.Ingrediente.StockActual >= r.CantidadRequerida);
        }
        return resultado;
    }

    // Una opción puede tener una receta específica para este producto (ej. "Leche Entera"
    // descuenta 240ml en Capuccino pero 180ml en Moccacino); si no hay ninguna específica,
    // se usa la receta por defecto (ProductoId nulo, ej. el fruto de "Tipo de Fruto").
    private static List<RecetaOpcionModificador> RecetasAplicables(OpcionModificador opcion, int productoId)
    {
        var especificas = opcion.Recetas.Where(r => r.ProductoId == productoId).ToList();
        return especificas.Count > 0 ? especificas : opcion.Recetas.Where(r => r.ProductoId is null).ToList();
    }

    public async Task ConfirmarItemsAsync(int pedidoId, IEnumerable<ItemCarrito> items, int actorUsuarioId)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        // Toda la confirmación corre en una única transacción: si un ítem falla a mitad del
        // carrito (ej. sin stock), se revierten también los ítems anteriores ya escritos en
        // este mismo carrito, en vez de dejar un pedido a medio confirmar con su stock ya
        // descontado mientras el mesero ve un mensaje de error.
        await using var transaction = await context.Database.BeginTransactionAsync();

        var pedido = await context.Pedidos.FindAsync(pedidoId)
            ?? throw new InvalidOperationException($"Pedido {pedidoId} no encontrado.");

        var huboDeduccion = false;
        var resumen = new List<string>();
        var forzados = new List<string>();

        foreach (var item in items)
        {
            var producto = await context.Productos
                .Include(p => p.Receta).ThenInclude(r => r.Ingrediente)
                .FirstOrDefaultAsync(p => p.Id == item.ProductoId)
                ?? throw new InvalidOperationException($"Producto {item.ProductoId} no encontrado.");

            var opciones = await context.OpcionesModificadores
                .Include(o => o.Recetas).ThenInclude(r => r.Ingrediente)
                .Where(o => item.OpcionModificadorIds.Contains(o.Id))
                .ToListAsync();

            if (!item.ForzarVenta)
            {
                var faltante = producto.Receta.FirstOrDefault(r => r.Ingrediente.StockActual < r.CantidadRequerida * item.Cantidad);
                if (faltante is not null)
                    throw new InvalidOperationException($"Stock insuficiente de '{faltante.Ingrediente.Nombre}' para '{producto.Nombre}'.");

                RecetaOpcionModificador? faltanteOpcion = null;
                OpcionModificador? opcionConFaltante = null;
                foreach (var opcion in opciones)
                {
                    faltanteOpcion = RecetasAplicables(opcion, producto.Id)
                        .FirstOrDefault(r => r.Ingrediente.StockActual < r.CantidadRequerida * item.Cantidad);
                    if (faltanteOpcion is not null)
                    {
                        opcionConFaltante = opcion;
                        break;
                    }
                }
                if (faltanteOpcion is not null)
                    throw new InvalidOperationException($"Stock insuficiente de '{faltanteOpcion.Ingrediente.Nombre}' para la opción '{opcionConFaltante!.Nombre}' de '{producto.Nombre}'.");
            }
            else
            {
                forzados.Add($"{item.Cantidad}x {producto.Nombre}");
            }

            resumen.Add($"{item.Cantidad}x {producto.Nombre}");

            var detalle = new DetallePedido
            {
                PedidoId = pedidoId,
                ProductoId = producto.Id,
                Cantidad = item.Cantidad,
                PrecioUnitario = producto.Precio
            };
            context.DetallesPedido.Add(detalle);
            await context.SaveChangesAsync();

            foreach (var opcionId in item.OpcionModificadorIds)
            {
                var opcion = opciones.FirstOrDefault(o => o.Id == opcionId)
                    ?? throw new InvalidOperationException($"Opción de modificador {opcionId} no encontrada.");

                context.DetallesPedidoModificadores.Add(new DetallePedidoModificador
                {
                    DetallePedidoId = detalle.Id,
                    OpcionModificadorId = opcionId,
                    PrecioAdicional = opcion.PrecioAdicional
                });
            }

            foreach (var receta in producto.Receta)
            {
                receta.Ingrediente.StockActual -= receta.CantidadRequerida * item.Cantidad;
                huboDeduccion = true;
            }

            foreach (var opcion in opciones)
            {
                foreach (var receta in RecetasAplicables(opcion, producto.Id))
                {
                    receta.Ingrediente.StockActual -= receta.CantidadRequerida * item.Cantidad;
                    huboDeduccion = true;
                }
            }
        }

        await context.SaveChangesAsync();
        await transaction.CommitAsync();

        await auditoria.RegistrarAsync(actorUsuarioId, TiposAccionAuditoria.ConfirmarPedido,
            $"Confirmó en el pedido #{pedidoId}: {string.Join(", ", resumen)}.");

        if (forzados.Count > 0)
            await auditoria.RegistrarAsync(actorUsuarioId, TiposAccionAuditoria.ForzarVenta,
                $"Forzó venta sin stock suficiente en el pedido #{pedidoId}: {string.Join(", ", forzados)}.");

        await hub.Clients.All.SendAsync(ComandaEventos.PedidoActualizado);
        if (huboDeduccion)
            await hub.Clients.All.SendAsync(ComandaEventos.AlertaStockActualizada);
    }

    public async Task AnularAsync(int pedidoId, int actorUsuarioId)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        var pedido = await context.Pedidos
            .Include(p => p.Mesas).ThenInclude(pm => pm.Mesa)
            .Include(p => p.Detalles).ThenInclude(d => d.Producto).ThenInclude(p => p.Receta).ThenInclude(r => r.Ingrediente)
            .Include(p => p.Detalles).ThenInclude(d => d.Modificadores).ThenInclude(m => m.OpcionModificador).ThenInclude(o => o.Recetas).ThenInclude(r => r.Ingrediente)
            .FirstOrDefaultAsync(p => p.Id == pedidoId)
            ?? throw new InvalidOperationException($"Pedido {pedidoId} no encontrado.");

        var etiqueta = string.Join(" + ", pedido.Mesas.Select(pm =>
            $"{(pm.Mesa.Tipo == TipoMesa.Barra ? "Barra" : "Mesa")} {pm.Mesa.Numero}"));

        // Al anular se devuelve al inventario todo lo que se había descontado al confirmar,
        // para que el stock del sistema no quede permanentemente por debajo del real.
        RestaurarStockDePedido(pedido);

        pedido.Estado = EstadoPedido.Anulado;
        pedido.FechaCierre = DateTime.Now;
        await context.SaveChangesAsync();

        await auditoria.RegistrarAsync(actorUsuarioId, TiposAccionAuditoria.AnularPedido,
            $"Anuló el pedido #{pedidoId} ({etiqueta}) y repuso el stock consumido.");

        await hub.Clients.All.SendAsync(ComandaEventos.PedidoActualizado);
        if (pedido.Detalles.Count > 0)
            await hub.Clients.All.SendAsync(ComandaEventos.AlertaStockActualizada);
    }

    // Suma de vuelta al stock lo que ConfirmarItemsAsync había descontado para cada línea del
    // pedido: la receta del producto y las recetas aplicables de cada opción de modificador.
    private static void RestaurarStockDePedido(Pedido pedido)
    {
        foreach (var detalle in pedido.Detalles)
        {
            foreach (var receta in detalle.Producto.Receta)
                receta.Ingrediente.StockActual += receta.CantidadRequerida * detalle.Cantidad;

            foreach (var modificador in detalle.Modificadores)
            {
                foreach (var receta in RecetasAplicables(modificador.OpcionModificador, detalle.ProductoId))
                    receta.Ingrediente.StockActual += receta.CantidadRequerida * detalle.Cantidad;
            }
        }
    }

    public async Task EliminarItemAsync(int detallePedidoId, int actorUsuarioId)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        var detalle = await context.DetallesPedido
            .Include(d => d.Pedido)
            .Include(d => d.Producto).ThenInclude(p => p.Receta).ThenInclude(r => r.Ingrediente)
            .Include(d => d.Modificadores).ThenInclude(m => m.OpcionModificador).ThenInclude(o => o.Recetas).ThenInclude(r => r.Ingrediente)
            .FirstOrDefaultAsync(d => d.Id == detallePedidoId)
            ?? throw new InvalidOperationException($"Ítem {detallePedidoId} no encontrado.");

        if (detalle.Pedido.Estado != EstadoPedido.Pendiente)
            throw new InvalidOperationException("No se puede quitar un ítem de un pedido que ya está cerrado o anulado.");

        // Si la cocina/barra ya lo marcó "Listo", el ingrediente ya se usó físicamente aunque
        // el cliente no se lo lleve: se quita del pedido pero no se repone el stock. Si todavía
        // estaba pendiente de preparar, nunca se llegó a consumir y sí se repone.
        var yaPreparado = detalle.Estado == EstadoDetallePedido.Listo;
        if (!yaPreparado)
        {
            foreach (var receta in detalle.Producto.Receta)
                receta.Ingrediente.StockActual += receta.CantidadRequerida * detalle.Cantidad;

            foreach (var modificador in detalle.Modificadores)
            {
                foreach (var receta in RecetasAplicables(modificador.OpcionModificador, detalle.ProductoId))
                    receta.Ingrediente.StockActual += receta.CantidadRequerida * detalle.Cantidad;
            }
        }

        var descripcion = $"{detalle.Cantidad}x {detalle.Producto.Nombre}";
        context.DetallesPedido.Remove(detalle); // cascada: también borra sus DetallePedidoModificador

        await context.SaveChangesAsync();

        await auditoria.RegistrarAsync(actorUsuarioId, TiposAccionAuditoria.EliminarItemPedido,
            $"Quitó '{descripcion}' del pedido #{detalle.PedidoId}" +
            (yaPreparado ? " (ya estaba preparado; no se repuso stock)." : " y repuso su stock."));

        await hub.Clients.All.SendAsync(ComandaEventos.PedidoActualizado);
        if (!yaPreparado)
            await hub.Clients.All.SendAsync(ComandaEventos.AlertaStockActualizada);
    }

    public async Task CerrarAsync(int pedidoId, int actorUsuarioId)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        var pedido = await context.Pedidos
            .Include(p => p.Mesas).ThenInclude(pm => pm.Mesa)
            .FirstOrDefaultAsync(p => p.Id == pedidoId)
            ?? throw new InvalidOperationException($"Pedido {pedidoId} no encontrado.");

        var etiqueta = string.Join(" + ", pedido.Mesas.Select(pm =>
            $"{(pm.Mesa.Tipo == TipoMesa.Barra ? "Barra" : "Mesa")} {pm.Mesa.Numero}"));

        pedido.Estado = EstadoPedido.Cerrado;
        pedido.FechaCierre = DateTime.Now;
        await context.SaveChangesAsync();

        await auditoria.RegistrarAsync(actorUsuarioId, TiposAccionAuditoria.CerrarPedido,
            $"Cerró la cuenta y liberó el pedido #{pedidoId} ({etiqueta}).");

        await hub.Clients.All.SendAsync(ComandaEventos.PedidoActualizado);
    }

    public async Task<Pedido> FusionarAsync(IEnumerable<int> mesaIds, int usuarioId)
    {
        var ids = mesaIds.Distinct().ToList();
        if (ids.Count < 2)
            throw new InvalidOperationException("Selecciona al menos dos mesas para fusionar.");

        await using var context = await contextFactory.CreateDbContextAsync();

        var pedidosPorMesa = new Dictionary<int, Pedido>();
        foreach (var mesaId in ids)
        {
            var existente = await context.PedidosMesas
                .Where(pm => pm.MesaId == mesaId && pm.Pedido.Estado == EstadoPedido.Pendiente)
                .Select(pm => pm.Pedido)
                .FirstOrDefaultAsync();

            if (existente is null)
            {
                existente = new Pedido { UsuarioId = usuarioId };
                existente.Mesas.Add(new PedidoMesa { MesaId = mesaId });
                context.Pedidos.Add(existente);
                await context.SaveChangesAsync();
            }

            pedidosPorMesa[mesaId] = existente;
        }

        var pedidoMaestro = pedidosPorMesa[ids[0]];

        foreach (var mesaId in ids.Skip(1))
        {
            var pedidoOtro = pedidosPorMesa[mesaId];
            if (pedidoOtro.Id == pedidoMaestro.Id)
                continue;

            var detalles = await context.DetallesPedido.Where(d => d.PedidoId == pedidoOtro.Id).ToListAsync();
            foreach (var detalle in detalles)
                detalle.PedidoId = pedidoMaestro.Id;

            var vinculoExistente = await context.PedidosMesas
                .AnyAsync(pm => pm.PedidoId == pedidoMaestro.Id && pm.MesaId == mesaId);
            if (!vinculoExistente)
                context.PedidosMesas.Add(new PedidoMesa { PedidoId = pedidoMaestro.Id, MesaId = mesaId });

            var pedidoAEliminar = await context.Pedidos.FindAsync(pedidoOtro.Id);
            if (pedidoAEliminar is not null)
                context.Pedidos.Remove(pedidoAEliminar);
        }

        await context.SaveChangesAsync();

        await auditoria.RegistrarAsync(usuarioId, TiposAccionAuditoria.FusionarMesas,
            $"Fusionó {ids.Count} mesas en el pedido #{pedidoMaestro.Id}.");

        await hub.Clients.All.SendAsync(ComandaEventos.PedidoActualizado);
        return pedidoMaestro;
    }

    public async Task<List<ItemKds>> ListarParaKdsAsync(DestinoPreparacion destino)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        var detalles = await context.DetallesPedido
            .Include(d => d.Producto)
            .Include(d => d.Pedido).ThenInclude(p => p.Mesas).ThenInclude(pm => pm.Mesa)
            .Include(d => d.Modificadores).ThenInclude(m => m.OpcionModificador)
            .Where(d => d.Estado == EstadoDetallePedido.Pendiente
                && d.Producto.DestinoPreparacion == destino
                && d.Pedido.Estado == EstadoPedido.Pendiente)
            .OrderBy(d => d.FechaCreacion)
            .ToListAsync();

        return detalles.Select(d => new ItemKds
        {
            DetallePedidoId = d.Id,
            PedidoId = d.PedidoId,
            MesaEtiqueta = string.Join(" + ", d.Pedido.Mesas.Select(pm =>
                $"{(pm.Mesa.Tipo == TipoMesa.Barra ? "Barra" : "Mesa")} {pm.Mesa.Numero}")),
            ProductoNombre = d.Producto.Nombre,
            Cantidad = d.Cantidad,
            Modificadores = d.Modificadores.Select(m => m.OpcionModificador.Nombre).ToList(),
            FechaCreacion = d.FechaCreacion
        }).ToList();
    }

    public async Task MarcarListoAsync(int detallePedidoId)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        var detalle = await context.DetallesPedido.FindAsync(detallePedidoId)
            ?? throw new InvalidOperationException($"Ítem {detallePedidoId} no encontrado.");

        detalle.Estado = EstadoDetallePedido.Listo;
        await context.SaveChangesAsync();

        await hub.Clients.All.SendAsync(ComandaEventos.PedidoActualizado);
    }

    public async Task SolicitarImpresionAsync(int pedidoId)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        var pedido = await context.Pedidos
            .Include(p => p.Mesas).ThenInclude(pm => pm.Mesa)
            .FirstOrDefaultAsync(p => p.Id == pedidoId)
            ?? throw new InvalidOperationException($"Pedido {pedidoId} no encontrado.");

        var etiqueta = string.Join(" + ", pedido.Mesas.Select(pm =>
            $"{(pm.Mesa.Tipo == TipoMesa.Barra ? "Barra" : "Mesa")} {pm.Mesa.Numero}"));

        await hub.Clients.All.SendAsync(ComandaEventos.SolicitudImpresion, new SolicitudImpresion
        {
            PedidoId = pedidoId,
            MesaEtiqueta = etiqueta
        });
    }
}
