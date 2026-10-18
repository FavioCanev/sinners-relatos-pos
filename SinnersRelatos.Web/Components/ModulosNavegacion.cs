namespace SinnersRelatos.Web.Components;

public record PaginaModulo(string Href, string Titulo);

public record ModuloNavegacion(
    string Clave,
    string Titulo,
    string IconoSvg,
    bool SoloAdministrador,
    IReadOnlyList<PaginaModulo> Paginas,
    IReadOnlyList<string>? RutasExtra = null);

// Fuente única de cómo se agrupan las páginas en el menú lateral (NavMenu) y en las pestañas
// de cada módulo (NavModulo). Las rutas de las páginas no cambian: esto solo decide cómo se llega.
public static class ModulosNavegacion
{
    private const string Svg = "<svg viewBox='0 0 24 24' aria-hidden='true'>";

    public static readonly IReadOnlyList<ModuloNavegacion> Todos =
    [
        new("mesas", "Mesas",
            Svg + "<path d='M3 10.5 12 3l9 7.5'/><path d='M5 9.5V20h14V9.5'/><path d='M10 20v-5h4v5'/></svg>",
            false,
            [new("mesas", "Mesas")],
            ["pedido"]),
        new("cocina", "Cocina",
            Svg + "<rect x='3' y='4' width='18' height='12' rx='2'/><path d='M8 20h8M12 16v4'/><path d='M7 9h4M7 12h7'/></svg>",
            false,
            [new("kds/barra", "Barra"), new("kds/cocina", "Cocina")]),
        new("catalogo", "Catálogo",
            Svg + "<path d='M20.6 13.4 13.4 20.6a2 2 0 0 1-2.8 0L3 13V3h10l7.6 7.6a2 2 0 0 1 0 2.8z'/><circle cx='7.5' cy='7.5' r='1.5'/></svg>",
            true,
            [new("productos", "Productos"), new("categorias", "Categorías"), new("modificadores", "Modificadores"), new("ingredientes", "Ingredientes")]),
        new("reportes", "Reportes",
            Svg + "<path d='M21 12A9 9 0 1 1 12 3v9z'/><path d='M15 3.5A9 9 0 0 1 20.5 9H15z'/></svg>",
            true,
            [new("dashboard", "Dashboard"), new("auditoria", "Auditoría")]),
        new("admin", "Admin",
            Svg + "<circle cx='9' cy='8' r='3.5'/><path d='M2.5 20a6.5 6.5 0 0 1 13 0'/><path d='M16 4.5a3.5 3.5 0 0 1 0 7'/><path d='M18 14a6 6 0 0 1 3.5 6'/></svg>",
            true,
            [new("usuarios", "Usuarios"), new("herramientas-desarrollo", "Herramientas")]),
    ];

    public const string IconoCuenta = Svg + "<circle cx='12' cy='8' r='4'/><path d='M4 21a8 8 0 0 1 16 0'/></svg>";
    public const string IconoSalir = Svg + "<path d='M15 4h3a2 2 0 0 1 2 2v12a2 2 0 0 1-2 2h-3'/><path d='M10 17l-5-5 5-5'/><path d='M5 12h11'/></svg>";

    // rutaRelativa = Navigation.ToBaseRelativePath(...), sin query (ej. "mesas/sinners").
    public static ModuloNavegacion? DeRuta(string rutaRelativa)
    {
        var ruta = rutaRelativa.Split('?', '#')[0].Trim('/').ToLowerInvariant();
        return Todos.FirstOrDefault(m =>
            m.Paginas.Select(p => p.Href).Concat(m.RutasExtra ?? []).Any(href => ruta == href || ruta.StartsWith(href + "/")));
    }

    public static ModuloNavegacion? PorClave(string clave) => Todos.FirstOrDefault(m => m.Clave == clave);
}
