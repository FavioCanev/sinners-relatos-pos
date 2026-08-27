using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SinnersRelatos.Web.Data;
using SinnersRelatos.Web.Hubs;
using SinnersRelatos.Web.Models;
using SinnersRelatos.Web.Services.Interfaces;

namespace SinnersRelatos.Web.Services;

public class PedidoService(AppDbContext context, IHubContext<ComandaHub> hub, IAuditoriaService auditoria) : IPedidoService
{
    public async Task<Pedido> ObtenerOCrearPedidoAsync(int mesaId, int usuarioId)
    {
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

    public async Task<Pedido?> ObtenerConDetalleAsync(int pedidoId) =>
        await context.Pedidos
            .Include(p => p.Usuario)
            .Include(p => p.Mesas).ThenInclude(pm => pm.Mesa)
            .Include(p => p.Detalles).ThenInclude(d => d.Producto)
            .Include(p => p.Detalles).ThenInclude(d => d.Modificadores).ThenInclude(m => m.OpcionModificador)
            .FirstOrDefaultAsync(p => p.Id == pedidoId);

    public async Task<Dictionary<int, bool>> VerificarDisponibilidadAsync(IEnumerable<int> productoIds)
    {
        var ids = productoIds.ToList();
        var recetas = await context.RecetasProducto
            .Include(r => r.Ingrediente)
            .Where(r => ids.Contains(r.ProductoId))
            .ToListAsync();

        return ids.ToDictionary(
            id => id,
            id => recetas.Where(r => r.ProductoId == id).All(r => r.Ingrediente.StockActual >= r.CantidadRequerida));
    }

    public async Task ConfirmarItemsAsync(int pedidoId, IEnumerable<ItemCarrito> items, int actorUsuarioId)
    {
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

            if (!item.ForzarVenta)
            {
                var faltante = producto.Receta.FirstOrDefault(r => r.Ingrediente.StockActual < r.CantidadRequerida * item.Cantidad);
                if (faltante is not null)
                    throw new InvalidOperationException($"Stock insuficiente de '{faltante.Ingrediente.Nombre}' para '{producto.Nombre}'.");
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
                var opcion = await context.OpcionesModificadores.FindAsync(opcionId)
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
        }

        await context.SaveChangesAsync();

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
        var pedido = await context.Pedidos
            .Include(p => p.Mesas).ThenInclude(pm => pm.Mesa)
            .FirstOrDefaultAsync(p => p.Id == pedidoId)
            ?? throw new InvalidOperationException($"Pedido {pedidoId} no encontrado.");

        var etiqueta = string.Join(" + ", pedido.Mesas.Select(pm =>
            $"{(pm.Mesa.Tipo == TipoMesa.Barra ? "Barra" : "Mesa")} {pm.Mesa.Numero}"));

        pedido.Estado = EstadoPedido.Anulado;
        pedido.FechaCierre = DateTime.Now;
        await context.SaveChangesAsync();

        await auditoria.RegistrarAsync(actorUsuarioId, TiposAccionAuditoria.AnularPedido,
            $"Anuló el pedido #{pedidoId} ({etiqueta}).");

        await hub.Clients.All.SendAsync(ComandaEventos.PedidoActualizado);
    }

    public async Task CerrarAsync(int pedidoId, int actorUsuarioId)
    {
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

        var pedidosPorMesa = new Dictionary<int, Pedido>();
        foreach (var mesaId in ids)
            pedidosPorMesa[mesaId] = await ObtenerOCrearPedidoAsync(mesaId, usuarioId);

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

    public async Task MarcarEntregadoAsync(int detallePedidoId)
    {
        var detalle = await context.DetallesPedido.FindAsync(detallePedidoId)
            ?? throw new InvalidOperationException($"Ítem {detallePedidoId} no encontrado.");

        detalle.Estado = EstadoDetallePedido.Entregado;
        await context.SaveChangesAsync();

        await hub.Clients.All.SendAsync(ComandaEventos.PedidoActualizado);
    }

    public async Task SolicitarImpresionAsync(int pedidoId)
    {
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
