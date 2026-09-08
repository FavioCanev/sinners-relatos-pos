using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SinnersRelatos.Web.Data;
using SinnersRelatos.Web.Hubs;
using SinnersRelatos.Web.Models;
using SinnersRelatos.Web.Services.Interfaces;

namespace SinnersRelatos.Web.Services;

public class HerramientasDesarrolloService(
    AppDbContext context,
    IPedidoService pedidoService,
    IHubContext<ComandaHub> hub,
    IAuditoriaService auditoria) : IHerramientasDesarrolloService
{
    private const string NombreUsuarioMeseroPruebas = "mesero.pruebas";

    // Valores de referencia para poder recargar rápido un inventario razonable
    // durante pruebas, sin depender de la planilla de costeo original.
    private static readonly (string Nombre, decimal StockActual, decimal StockMinimo)[] StockDePrueba =
    [
        ("Aceite", 5000m, 800m),
        ("Agua Personal con Gas", 12000m, 1000m),
        ("Agua Tónica", 6000m, 800m),
        ("Agua", 50000m, 0m),
        ("Algarrobina (Licor)", 1500m, 300m),
        ("Amaretto", 1500m, 300m),
        ("Angostura (Amargo)", 400m, 100m),
        ("Arándano", 2000m, 400m),
        ("Azúcar Blanca", 3000m, 500m),
        ("Azúcar", 4000m, 500m),
        ("Baileys", 2250m, 300m),
        ("Barquillos", 60m, 15m),
        ("Cabanossi", 500m, 100m),
        ("Café en Grano", 5000m, 1000m),
        ("Campari", 1500m, 300m),
        ("Canela", 300m, 60m),
        ("Carne de Hamburguesa", 3000m, 500m),
        ("Cedrón", 300m, 60m),
        ("Chimichurri", 500m, 100m),
        ("Chocolate de Taza", 1000m, 200m),
        ("Chorizo Artesanal (Argentino)", 2000m, 400m),
        ("Chorizo Artesanal (Español)", 1800m, 350m),
        ("Chorizo Industrial", 2000m, 400m),
        ("Crema de Cacao Blanca", 1000m, 200m),
        ("Crema de Coco", 1000m, 200m),
        ("Crema de Leche", 1500m, 300m),
        ("Durazno", 1500m, 300m),
        ("Empanada de Carne (Prefabricada)", 50m, 12m),
        ("Empanada de Jamón y Queso (Prefabricada)", 50m, 12m),
        ("Fernet", 2250m, 300m),
        ("Fresa", 2000m, 400m),
        ("Frutos Rojos", 1500m, 300m),
        ("Galleta tipo Oreo", 60m, 15m),
        ("Gaseosa", 12000m, 1500m),
        ("Gin", 2250m, 300m),
        ("Ginger Ale", 6000m, 800m),
        ("Helado de Vainilla", 2000m, 400m),
        ("Hielo", 20000m, 3000m),
        ("Huevos", 60m, 15m),
        ("Jamaica (Flor/Infusión)", 400m, 80m),
        ("Jamón", 2000m, 400m),
        ("Jarabe de Goma", 3000m, 500m),
        ("Jarabe de Vainilla", 1500m, 300m),
        ("Jugo de Limón", 1000m, 200m),
        ("Kahlúa (Licor de Café)", 2250m, 300m),
        ("Leche Condensada", 2000m, 400m),
        ("Leche Deslactosada", 4000m, 700m),
        ("Leche Entera", 6000m, 1000m),
        ("Leche en Polvo", 1000m, 200m),
        ("Lechuga", 1500m, 300m),
        ("Licor de Menta", 1500m, 300m),
        ("Limón", 150m, 30m),
        ("Maracuyá", 2000m, 400m),
        ("Masa de Pizza", 40m, 10m),
        ("Miel", 1500m, 300m),
        ("Mix de Frutas", 2000m, 400m),
        ("Naranja", 60m, 15m),
        ("Pan Ciabatta", 40m, 10m),
        ("Pan de Hamburguesa", 30m, 8m),
        ("Panceta", 2000m, 400m),
        ("Papa Fresca (Fritura)", 10000m, 1500m),
        ("Papas", 3000m, 500m),
        ("Papaya", 2000m, 400m),
        ("Pimiento", 1000m, 200m),
        ("Pisco", 3000m, 500m),
        ("Piña (Fresca)", 2000m, 400m),
        ("Piña (Lata)", 1500m, 300m),
        ("Plátano", 2000m, 400m),
        ("Queso Mozzarella", 3000m, 500m),
        ("Red Bull", 3000m, 500m),
        ("Ron Blanco", 2250m, 300m),
        ("Ron Jamaiquino", 2250m, 300m),
        ("Salsa de Tomate", 2000m, 400m),
        ("Syrup de Caramelo", 1000m, 200m),
        ("Syrup de Chocolate", 1500m, 300m),
        ("Tequila", 2250m, 300m),
        ("Tomate", 2000m, 400m),
        ("Triple Sec", 1500m, 300m),
        ("Té a Elegir (Base)", 100m, 0m),
        ("Vermut Rojo", 1500m, 300m),
        ("Vodka", 2250m, 300m),
        ("Whisky Bourbon", 1500m, 300m),
        ("Whisky", 2250m, 300m),
    ];

    public async Task<bool> ObtenerModoPruebasAsync()
    {
        var config = await ObtenerConfiguracionAsync();
        return config.ModoPruebasActivo;
    }

    public async Task CambiarModoPruebasAsync(bool activo, int actorUsuarioId)
    {
        var config = await ObtenerConfiguracionAsync();
        config.ModoPruebasActivo = activo;
        config.ModoPruebasActivadoEn = activo ? DateTime.Now : null;
        config.ModoPruebasActivadoPorUsuarioId = activo ? actorUsuarioId : null;
        await context.SaveChangesAsync();

        await auditoria.RegistrarAsync(actorUsuarioId, TiposAccionAuditoria.CambiarModoPruebas,
            $"{(activo ? "Activó" : "Desactivó")} el modo de pruebas.");
    }

    public async Task VaciarStockAsync(int actorUsuarioId)
    {
        var ingredientes = await context.Ingredientes.ToListAsync();
        foreach (var ingrediente in ingredientes)
            ingrediente.StockActual = 0;
        await context.SaveChangesAsync();

        await auditoria.RegistrarAsync(actorUsuarioId, TiposAccionAuditoria.VaciarStockPruebas,
            $"Vació el stock de los {ingredientes.Count} ingredientes (modo pruebas).");

        await hub.Clients.All.SendAsync(ComandaEventos.AlertaStockActualizada);
    }

    public async Task CargarStockDePruebaAsync(int actorUsuarioId)
    {
        var ingredientesPorNombre = await context.Ingredientes.ToDictionaryAsync(i => i.Nombre);

        var actualizados = 0;
        foreach (var (nombre, stockActual, stockMinimo) in StockDePrueba)
        {
            if (!ingredientesPorNombre.TryGetValue(nombre, out var ingrediente))
                continue;

            ingrediente.StockActual = stockActual;
            ingrediente.StockMinimo = stockMinimo;
            actualizados++;
        }
        await context.SaveChangesAsync();

        await auditoria.RegistrarAsync(actorUsuarioId, TiposAccionAuditoria.CargarStockPruebas,
            $"Cargó stock de prueba para {actualizados} ingredientes (modo pruebas).");

        await hub.Clients.All.SendAsync(ComandaEventos.AlertaStockActualizada);
    }

    public async Task<int> SimularPedidosAsync(int cantidad, int actorUsuarioId)
    {
        cantidad = Math.Clamp(cantidad, 1, 100);

        var meseroId = await ObtenerIdMeseroPruebasAsync()
            ?? throw new InvalidOperationException(
                "No se encontró el usuario 'Mesero de Pruebas'. Reinicia la aplicación para que se cree automáticamente.");

        var mesas = await context.Mesas.Where(m => m.Activo).ToListAsync();
        if (mesas.Count == 0)
            throw new InvalidOperationException("No hay mesas activas para simular pedidos.");

        var productos = await context.Productos
            .Include(p => p.GruposModificadores).ThenInclude(pg => pg.GrupoModificador).ThenInclude(g => g.Opciones)
            .Where(p => p.Activo)
            .ToListAsync();

        var random = Random.Shared;
        var pedidosCreados = 0;

        for (var i = 0; i < cantidad; i++)
        {
            var mesa = mesas[random.Next(mesas.Count)];
            var productosDisponibles = productos.Where(p => p.Marca == mesa.Marca).ToList();
            if (productosDisponibles.Count == 0)
                continue;

            var pedido = await pedidoService.ObtenerOCrearPedidoAsync(mesa.Id, meseroId);

            var items = new List<ItemCarrito>();
            var cantidadItems = random.Next(1, 4);
            for (var j = 0; j < cantidadItems; j++)
            {
                var producto = productosDisponibles[random.Next(productosDisponibles.Count)];
                items.Add(new ItemCarrito
                {
                    ProductoId = producto.Id,
                    Cantidad = random.Next(1, 3),
                    OpcionModificadorIds = ElegirOpcionesAlAzar(producto, random),
                    ForzarVenta = true
                });
            }

            await pedidoService.ConfirmarItemsAsync(pedido.Id, items, meseroId);

            // El 60% de las mesas simuladas se cierran de inmediato para variar
            // entre mesas "ocupadas" y pedidos ya facturados al probar.
            if (random.Next(100) < 60)
                await pedidoService.CerrarAsync(pedido.Id, meseroId);

            pedidosCreados++;
        }

        await auditoria.RegistrarAsync(actorUsuarioId, TiposAccionAuditoria.SimularPedidosPruebas,
            $"Simuló {pedidosCreados} pedidos con el mesero ficticio (modo pruebas).");

        return pedidosCreados;
    }

    public async Task<int> EliminarPedidosDePruebaAsync(int actorUsuarioId)
    {
        var meseroId = await ObtenerIdMeseroPruebasAsync();
        if (meseroId is null)
            return 0;

        var pedidos = await context.Pedidos.Where(p => p.UsuarioId == meseroId).ToListAsync();
        context.Pedidos.RemoveRange(pedidos);
        await context.SaveChangesAsync();

        await auditoria.RegistrarAsync(actorUsuarioId, TiposAccionAuditoria.EliminarPedidosPruebas,
            $"Eliminó {pedidos.Count} pedidos de prueba (modo pruebas). El stock consumido no se restituye automáticamente.");

        await hub.Clients.All.SendAsync(ComandaEventos.PedidoActualizado);

        return pedidos.Count;
    }

    private static List<int> ElegirOpcionesAlAzar(Producto producto, Random random)
    {
        var opcionIds = new List<int>();

        foreach (var vinculo in producto.GruposModificadores)
        {
            var grupo = vinculo.GrupoModificador;
            var opcionesActivas = grupo.Opciones.Where(o => o.Activo).ToList();
            if (opcionesActivas.Count == 0)
                continue;

            // Los grupos opcionales solo se eligen la mitad de las veces, para variar los pedidos.
            if (!grupo.EsObligatorio && random.Next(2) == 0)
                continue;

            var cantidadAElegir = grupo.PermiteMultiple
                ? random.Next(1, Math.Min(2, opcionesActivas.Count) + 1)
                : 1;

            opcionIds.AddRange(opcionesActivas.OrderBy(_ => random.Next()).Take(cantidadAElegir).Select(o => o.Id));
        }

        return opcionIds;
    }

    private async Task<int?> ObtenerIdMeseroPruebasAsync()
    {
        var id = await context.Usuarios
            .Where(u => u.NombreUsuario == NombreUsuarioMeseroPruebas)
            .Select(u => u.Id)
            .FirstOrDefaultAsync();

        return id == 0 ? null : id;
    }

    private async Task<ConfiguracionSistema> ObtenerConfiguracionAsync()
    {
        var config = await context.ConfiguracionSistema.FirstOrDefaultAsync();
        if (config is null)
        {
            config = new ConfiguracionSistema();
            context.ConfiguracionSistema.Add(config);
            await context.SaveChangesAsync();
        }
        return config;
    }
}
