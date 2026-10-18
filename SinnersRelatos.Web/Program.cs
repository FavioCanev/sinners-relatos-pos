using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SinnersRelatos.Web.Components;
using SinnersRelatos.Web.Data;
using SinnersRelatos.Web.Hubs;
using SinnersRelatos.Web.Services;
using SinnersRelatos.Web.Services.Interfaces;
using SinnersRelatos.Web.Services.Sunat;

var builder = WebApplication.CreateBuilder(args);

// Permite que este mismo ejecutable corra como Windows Service (arranca solo con la PC,
// Windows lo reinicia solo si crashea) sin cambiar nada cuando se lo sigue corriendo como
// consola normal (ej. "dotnet run" en desarrollo) — no hace nada distinto a menos que el
// Service Control Manager sea quien lo esté iniciando.
builder.Host.UseWindowsService();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSignalR();

// El archivo de la base se resuelve siempre relativo a la carpeta del ejecutable (no al
// directorio de trabajo actual), para que apunte al mismo archivo sin importar desde dónde se
// lance el proceso (ej. como Windows Service, que suele arrancar con otro working directory).
var sqliteConnectionStringBuilder = new SqliteConnectionStringBuilder(builder.Configuration.GetConnectionString("DefaultConnection"));
sqliteConnectionStringBuilder.DataSource = Path.Combine(AppContext.BaseDirectory, sqliteConnectionStringBuilder.DataSource);
var sqliteConnectionString = sqliteConnectionStringBuilder.ToString();

// Se usa una fábrica de contextos (en vez de un DbContext con ciclo de vida Scoped) porque en
// Blazor Server el ámbito "Scoped" dura todo el circuito del usuario, no una sola operación. Con
// un único DbContext compartido, un evento de SignalR (ej. otro mesero confirmando un pedido)
// puede intentar usarlo al mismo tiempo que la página actual, y EF Core no admite operaciones
// concurrentes sobre la misma instancia. Cada servicio crea ahora su propio DbContext de corta
// vida por operación a través de esta fábrica.
builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlite(sqliteConnectionString));

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
builder.Services.AddScoped<IPresetService, PresetService>();

// Facturación electrónica. Los datos sensibles (Clave SOL, certificado) van en "sunat.json" junto
// al ejecutable, en la PC del negocio: no está en el repositorio y el instalador no lo pisa.
builder.Configuration.AddJsonFile(Path.Combine(AppContext.BaseDirectory, "sunat.json"), optional: true, reloadOnChange: false);
builder.Services.AddSingleton(builder.Configuration.GetSection("Sunat").Get<SunatOptions>() ?? new SunatOptions());
builder.Services.AddSingleton<FirmaDigital>();
builder.Services.AddHttpClient(nameof(SunatClient), http => http.Timeout = TimeSpan.FromSeconds(60));
builder.Services.AddScoped<IComprobanteService, ComprobanteService>();
builder.Services.AddSingleton<EnvioSunatWorker>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<EnvioSunatWorker>());

var app = builder.Build();

// Migración y seed corren siempre, no solo en Development: ambas operaciones son idempotentes
// (el seed chequea qué falta antes de insertar) y en producción también hace falta que la base
// se cree y actualice sola en el primer arranque, sin depender de un paso manual.
using (var scope = app.Services.CreateScope())
{
    var contextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
    await using var context = await contextFactory.CreateDbContextAsync();
    var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

    // WAL: las lecturas (ver mesas, KDS) no bloquean ni son bloqueadas por una escritura en
    // curso. El "Default Timeout" del connection string ya cubre la otra mitad: si dos
    // escrituras coinciden, la segunda espera hasta esos segundos en vez de fallar al toque.
    await context.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");
    await context.Database.MigrateAsync();
    await DbSeeder.SeedAsync(context, passwordHasher);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

// Sin redirección a HTTPS a propósito: la app corre solo en la red local del negocio (ver
// Kestrel:Endpoints en appsettings.json), sin certificado, y forzar HTTPS ahí solo generaría
// advertencias de seguridad en las tablets sin ganar nada real.
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
app.MapHub<ComandaHub>("/hubs/comanda");

app.MapGet("/print/comprobante/{pedidoId:int}", async (int pedidoId, bool autoprint, IPedidoService pedidoService,
    IComprobanteService comprobanteService, SunatOptions sunat) =>
{
    var pedido = await pedidoService.ObtenerConDetalleAsync(pedidoId);
    if (pedido is null)
        return Results.NotFound();

    var comprobante = await comprobanteService.ObtenerDePedidoAsync(pedidoId);
    return Results.Content(ComprobanteHtml.Construir(pedido, autoprint, comprobante, sunat), "text/html");
});

app.Run();
