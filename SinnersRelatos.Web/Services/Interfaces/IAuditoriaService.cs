using SinnersRelatos.Web.Models;

namespace SinnersRelatos.Web.Services.Interfaces;

public interface IAuditoriaService
{
    Task RegistrarAsync(int? usuarioId, string tipoAccion, string detalle);

    Task<List<LogAuditoria>> ListarAsync(
        DateTime? desde = null,
        DateTime? hasta = null,
        int? usuarioId = null,
        string? tipoAccion = null);

    Task<List<string>> ListarTiposAccionAsync();
}
