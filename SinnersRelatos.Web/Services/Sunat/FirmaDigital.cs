using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Xml;
using System.Xml.Linq;

namespace SinnersRelatos.Web.Services.Sunat;

public record DocumentoFirmado(string Xml, string Hash);

// Firma XMLDSig "enveloped" sobre todo el documento, insertada en ext:ExtensionContent como pide
// SUNAT. El Hash (DigestValue) es el "valor resumen" que va impreso en la representación impresa.
public class FirmaDigital(SunatOptions opciones)
{
    private const string NsExt = "urn:oasis:names:specification:ubl:schema:xsd:CommonExtensionComponents-2";

    private readonly Lazy<X509Certificate2> certificado = new(() => CargarCertificado(opciones));

    public DocumentoFirmado Firmar(XDocument documento)
    {
        var xml = new XmlDocument { PreserveWhitespace = true };
        xml.LoadXml(documento.ToString(SaveOptions.DisableFormatting));

        var firma = new SignedXml(xml) { SigningKey = certificado.Value.GetRSAPrivateKey() };
        firma.SignedInfo!.SignatureMethod = SignedXml.XmlDsigRSASHA256Url;

        var referencia = new Reference("") { DigestMethod = SignedXml.XmlDsigSHA256Url };
        referencia.AddTransform(new XmlDsigEnvelopedSignatureTransform());
        firma.AddReference(referencia);

        var keyInfo = new KeyInfo();
        keyInfo.AddClause(new KeyInfoX509Data(certificado.Value));
        firma.KeyInfo = keyInfo;
        firma.Signature.Id = UblXml.IdFirma;

        firma.ComputeSignature();

        var contenedor = xml.GetElementsByTagName("ExtensionContent", NsExt)[0]
            ?? throw new InvalidOperationException("El XML no tiene ext:ExtensionContent para la firma.");
        contenedor.AppendChild(xml.ImportNode(firma.GetXml(), true));

        var hash = Convert.ToBase64String(referencia.DigestValue!);
        return new DocumentoFirmado("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"no\"?>" + xml.DocumentElement!.OuterXml, hash);
    }

    private static X509Certificate2 CargarCertificado(SunatOptions opciones)
    {
        if (!string.IsNullOrWhiteSpace(opciones.CertificadoRuta))
        {
            var ruta = Path.IsPathRooted(opciones.CertificadoRuta)
                ? opciones.CertificadoRuta
                : Path.Combine(AppContext.BaseDirectory, opciones.CertificadoRuta);
            return X509CertificateLoader.LoadPkcs12FromFile(ruta, opciones.CertificadoClave);
        }

        if (opciones.Produccion)
            throw new InvalidOperationException("Falta configurar el certificado digital (Sunat:CertificadoRuta) para emitir en producción.");

        // Beta: SUNAT no valida el certificado, así que basta uno de prueba generado al vuelo.
        var rsa = RSA.Create(2048);
        var solicitud = new CertificateRequest($"CN={opciones.Ruc}, O=PRUEBAS", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return solicitud.CreateSelfSigned(DateTimeOffset.Now.AddDays(-1), DateTimeOffset.Now.AddYears(5));
    }
}
