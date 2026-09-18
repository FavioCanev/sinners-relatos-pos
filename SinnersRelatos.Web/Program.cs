using Microsoft.EntityFrameworkCore;
using SinnersRelatos.Web.Components;
using SinnersRelatos.Web.Data;
using SinnersRelatos.Web.Hubs;
using SinnersRelatos.Web.Services;
using SinnersRelatos.Web.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSignalR();

// Se usa una fábrica de contextos (en vez de un DbContext con ciclo de vida Scoped) porque en
// Blazor Server el ámbito "Scoped" dura todo el circuito del usuario, no una sola operación. Con
// un único DbContext compartido, un evento de SignalR (ej. otro mesero confirmando un pedido)
// puede intentar usarlo al mismo tiempo que la página actual, y EF Core no admite operaciones
// concurrentes sobre la misma instancia. Cada servicio crea ahora su propio DbContext de corta
// vida por operación a través de esta fábrica.
builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IEmpleadoService, EmpleadoService>();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
builder.Services.AddScoped<ICategoriaService, CategoriaService>();
builder.Services.AddScoped<IProductoService, ProductoService>();
builder.Services.AddScoped<IModificadorService, ModificadorService>();
builder.Services.AddScoped<IMesaService, MesaService>();
builder.Services.AddScoped<IIngredienteService, IngredienteService>();
builder.Services.AddScoped<IPedidoService, PedidoService>();
builder.Services.AddScoped<ISesionService, SesionService>();
builder.Services.AddScoped<IAuditoriaService, AuditoriaService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IHerramientasDesarrolloService, HerramientasDesarrolloService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
else
{
    using var scope = app.Services.CreateScope();
    var contextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
    await using var context = await contextFactory.CreateDbContextAsync();
    var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    await context.Database.MigrateAsync();
    await DbSeeder.SeedAsync(context, passwordHasher);
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
app.MapHub<ComandaHub>("/hubs/comanda");

app.MapGet("/print/comprobante/{pedidoId:int}", async (int pedidoId, bool autoprint, IPedidoService pedidoService) =>
{
    var pedido = await pedidoService.ObtenerConDetalleAsync(pedidoId);
    return pedido is null
        ? Results.NotFound()
        : Results.Content(ComprobanteHtml.Construir(pedido, autoprint), "text/html");
});

app.Run();
