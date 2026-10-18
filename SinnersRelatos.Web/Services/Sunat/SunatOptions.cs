namespace SinnersRelatos.Web.Services.Sunat;

// Sección "Sunat" de la configuración. Los datos sensibles (Clave SOL, certificado) no van en
// appsettings.json sino en "sunat.json", junto al ejecutable en la PC del negocio: ese archivo no
// está en el repositorio y el instalador no lo pisa al actualizar.
public class SunatOptions
{
    // Apagado = el sistema funciona como antes (sin comprobantes electrónicos).
    public bool Habilitado { get; set; }

    // false = ambiente de pruebas (beta) de SUNAT: no tiene validez tributaria ni pide
    // certificado real; si no se configura uno, se firma con un certificado de prueba generado.
    public bool Produccion { get; set; }

    public string Ruc { get; set; } = "";
    public string RazonSocial { get; set; } = "";
    public string NombreComercial { get; set; } = "";
    public string Direccion { get; set; } = "";
    public string Ubigeo { get; set; } = "";

    public string SerieBoleta { get; set; } = "B001";
    public string SerieFactura { get; set; } = "F001";

    // Porcentaje de IGV que se aplica (10.5 = régimen MYPE de restaurantes; 18 = general). Los
    // precios del catálogo ya lo incluyen: el sistema lo separa al emitir.
    public decimal TasaIgv { get; set; } = 10.5m;

    // Clave SOL secundaria con perfil de envío de comprobantes. En beta se usa MODDATOS.
    public string UsuarioSol { get; set; } = "MODDATOS";
    public string ClaveSol { get; set; } = "moddatos";

    // Ruta al certificado .pfx (relativa a la carpeta del ejecutable o absoluta) y su contraseña.
    public string? CertificadoRuta { get; set; }
    public string? CertificadoClave { get; set; }

    // Cada cuánto se arma un resumen diario con las boletas nuevas del día.
    public int MinutosEntreResumenes { get; set; } = 30;

    public string UrlEnvio => Produccion
        ? "https://e-factura.sunat.gob.pe/ol-ti-itcpfegem/billService"
        : "https://e-beta.sunat.gob.pe/ol-ti-itcpfegem-beta/billService";
}
