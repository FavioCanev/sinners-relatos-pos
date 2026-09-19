namespace SinnersRelatos.Web.Models;

// Nombre de usuario del mesero ficticio al que se le atribuyen los pedidos generados desde
// Herramientas de Desarrollo (ver DbSeeder y HerramientasDesarrolloService). Centralizado acá
// para que cualquier reporte (ej. el Dashboard) pueda excluir sus pedidos de forma consistente.
public static class UsuariosSistema
{
    public const string MeseroPruebas = "mesero.pruebas";
}
