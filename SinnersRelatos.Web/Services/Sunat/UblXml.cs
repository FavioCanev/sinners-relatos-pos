using System.Globalization;
using System.Xml.Linq;
using SinnersRelatos.Web.Models;

namespace SinnersRelatos.Web.Services.Sunat;

public record LineaComprobante(string Descripcion, int Cantidad, decimal PrecioUnitarioConIgv);

public record MontosLinea(decimal Total, decimal ValorVenta, decimal Igv, decimal ValorUnitario);

// Arma los XML en el formato UBL que pide SUNAT (guías de elaboración de factura/boleta 2.1,
// resumen diario 1.1 y comunicación de baja 1.0). El nodo ext:ExtensionContent queda vacío: ahí
// FirmaDigital inserta la firma. Los precios del catálogo ya incluyen IGV, así que cada línea se
// descompone en valor de venta + IGV de forma que la suma cuadre exacto con lo cobrado.
public static class UblXml
{
    private static readonly XNamespace Cac = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";
    private static readonly XNamespace Cbc = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";
    private static readonly XNamespace Ext = "urn:oasis:names:specification:ubl:schema:xsd:CommonExtensionComponents-2";
    private static readonly XNamespace Ds = "http://www.w3.org/2000/09/xmldsig#";
    private static readonly XNamespace Sac = "urn:sunat:names:specification:ubl:peru:schema:xsd:SunatAggregateComponents-1";
    private static readonly XNamespace Invoice = "urn:oasis:names:specification:ubl:schema:xsd:Invoice-2";
    private static readonly XNamespace Summary = "urn:sunat:names:specification:ubl:peru:schema:xsd:SummaryDocuments-1";
    private static readonly XNamespace Voided = "urn:sunat:names:specification:ubl:peru:schema:xsd:VoidedDocuments-1";

    public const string IdFirma = "SignatureSR";

    public static MontosLinea CalcularLinea(LineaComprobante linea, decimal tasaIgv)
    {
        var factor = 1 + tasaIgv / 100m;
        var total = Math.Round(linea.PrecioUnitarioConIgv * linea.Cantidad, 2);
        var valorVenta = Math.Round(total / factor, 2);
        return new MontosLinea(total, valorVenta, total - valorVenta, Math.Round(linea.PrecioUnitarioConIgv / factor, 10));
    }

    public static XDocument Comprobante(Comprobante c, IReadOnlyList<LineaComprobante> lineas, SunatOptions emisor)
    {
        var raiz = new XElement(Invoice + "Invoice",
            new XAttribute(XNamespace.Xmlns + "cac", Cac),
            new XAttribute(XNamespace.Xmlns + "cbc", Cbc),
            new XAttribute(XNamespace.Xmlns + "ext", Ext),
            new XAttribute(XNamespace.Xmlns + "ds", Ds),
            ExtensionesVacias(),
            new XElement(Cbc + "UBLVersionID", "2.1"),
            new XElement(Cbc + "CustomizationID", "2.0"),
            new XElement(Cbc + "ID", c.SerieNumero),
            new XElement(Cbc + "IssueDate", c.FechaEmision.ToString("yyyy-MM-dd")),
            new XElement(Cbc + "IssueTime", c.FechaEmision.ToString("HH:mm:ss")),
            new XElement(Cbc + "InvoiceTypeCode", new XAttribute("listID", "0101"), ((int)c.Tipo).ToString("00")),
            new XElement(Cbc + "DocumentCurrencyCode", "PEN"),
            FirmaReferencia(emisor),
            new XElement(Cac + "AccountingSupplierParty",
                new XElement(Cac + "Party",
                    new XElement(Cac + "PartyIdentification", new XElement(Cbc + "ID", new XAttribute("schemeID", "6"), emisor.Ruc)),
                    new XElement(Cac + "PartyName", new XElement(Cbc + "Name", new XCData(emisor.NombreComercial))),
                    new XElement(Cac + "PartyLegalEntity",
                        new XElement(Cbc + "RegistrationName", new XCData(emisor.RazonSocial)),
                        new XElement(Cac + "RegistrationAddress",
                            new XElement(Cbc + "ID", emisor.Ubigeo),
                            new XElement(Cbc + "AddressTypeCode", "0000"),
                            new XElement(Cac + "AddressLine", new XElement(Cbc + "Line", new XCData(emisor.Direccion))),
                            new XElement(Cac + "Country", new XElement(Cbc + "IdentificationCode", "PE")))))),
            new XElement(Cac + "AccountingCustomerParty",
                new XElement(Cac + "Party",
                    new XElement(Cac + "PartyIdentification",
                        new XElement(Cbc + "ID", new XAttribute("schemeID", c.ClienteTipoDocumento), c.ClienteNumeroDocumento)),
                    new XElement(Cac + "PartyLegalEntity",
                        new XElement(Cbc + "RegistrationName", new XCData(c.ClienteNombre)),
                        string.IsNullOrWhiteSpace(c.ClienteDireccion)
                            ? null
                            : new XElement(Cac + "RegistrationAddress",
                                new XElement(Cac + "AddressLine", new XElement(Cbc + "Line", new XCData(c.ClienteDireccion))))))),
            // Forma de pago: obligatoria en facturas. El POS solo vende al contado.
            c.Tipo == TipoComprobante.Factura
                ? new XElement(Cac + "PaymentTerms",
                    new XElement(Cbc + "ID", "FormaPago"),
                    new XElement(Cbc + "PaymentMeansID", "Contado"))
                : null,
            TotalIgv(c.OperacionesGravadas, c.Igv),
            new XElement(Cac + "LegalMonetaryTotal",
                new XElement(Cbc + "LineExtensionAmount", Pen(), Monto(c.OperacionesGravadas)),
                new XElement(Cbc + "TaxInclusiveAmount", Pen(), Monto(c.Total)),
                new XElement(Cbc + "PayableAmount", Pen(), Monto(c.Total))),
            lineas.Select((linea, i) => Linea(i + 1, linea, c.TasaIgv)));

        return new XDocument(new XDeclaration("1.0", "UTF-8", "no"), raiz);
    }

    // Resumen diario: informa boletas emitidas un mismo día (estado 1 = adicionar, 3 = anular).
    public static XDocument ResumenDiario(string identificador, DateTime fechaGeneracion, DateOnly fechaComprobantes,
        IReadOnlyList<(Comprobante Boleta, bool EsAnulacion)> lineas, SunatOptions emisor)
    {
        var raiz = new XElement(Summary + "SummaryDocuments",
            new XAttribute(XNamespace.Xmlns + "cac", Cac),
            new XAttribute(XNamespace.Xmlns + "cbc", Cbc),
            new XAttribute(XNamespace.Xmlns + "ext", Ext),
            new XAttribute(XNamespace.Xmlns + "ds", Ds),
            new XAttribute(XNamespace.Xmlns + "sac", Sac),
            ExtensionesVacias(),
            new XElement(Cbc + "UBLVersionID", "2.0"),
            new XElement(Cbc + "CustomizationID", "1.1"),
            new XElement(Cbc + "ID", identificador),
            new XElement(Cbc + "ReferenceDate", fechaComprobantes.ToString("yyyy-MM-dd")),
            new XElement(Cbc + "IssueDate", fechaGeneracion.ToString("yyyy-MM-dd")),
            FirmaReferencia(emisor),
            EmisorResumen(emisor),
            lineas.Select((l, i) => new XElement(Sac + "SummaryDocumentsLine",
                new XElement(Cbc + "LineID", i + 1),
                new XElement(Cbc + "DocumentTypeCode", "03"),
                new XElement(Cbc + "ID", l.Boleta.SerieNumero),
                new XElement(Cac + "AccountingCustomerParty",
                    new XElement(Cbc + "CustomerAssignedAccountID", l.Boleta.ClienteNumeroDocumento),
                    new XElement(Cbc + "AdditionalAccountID", l.Boleta.ClienteTipoDocumento)),
                new XElement(Cac + "Status", new XElement(Cbc + "ConditionCode", l.EsAnulacion ? "3" : "1")),
                new XElement(Sac + "TotalAmount", Pen(), Monto(l.Boleta.Total)),
                new XElement(Sac + "BillingPayment",
                    new XElement(Cbc + "PaidAmount", Pen(), Monto(l.Boleta.OperacionesGravadas)),
                    new XElement(Cbc + "InstructionID", "01")),
                new XElement(Cac + "TaxTotal",
                    new XElement(Cbc + "TaxAmount", Pen(), Monto(l.Boleta.Igv)),
                    new XElement(Cac + "TaxSubtotal",
                        new XElement(Cbc + "TaxAmount", Pen(), Monto(l.Boleta.Igv)),
                        new XElement(Cac + "TaxCategory",
                            new XElement(Cbc + "Percent", l.Boleta.TasaIgv.ToString("0.00", CultureInfo.InvariantCulture)),
                            EsquemaIgv()))))));

        return new XDocument(new XDeclaration("1.0", "UTF-8", "no"), raiz);
    }

    // Comunicación de baja: anula facturas ya aceptadas por SUNAT.
    public static XDocument ComunicacionBaja(string identificador, DateTime fechaGeneracion, DateOnly fechaComprobantes,
        IReadOnlyList<Comprobante> facturas, SunatOptions emisor)
    {
        var raiz = new XElement(Voided + "VoidedDocuments",
            new XAttribute(XNamespace.Xmlns + "cac", Cac),
            new XAttribute(XNamespace.Xmlns + "cbc", Cbc),
            new XAttribute(XNamespace.Xmlns + "ext", Ext),
            new XAttribute(XNamespace.Xmlns + "ds", Ds),
            new XAttribute(XNamespace.Xmlns + "sac", Sac),
            ExtensionesVacias(),
            new XElement(Cbc + "UBLVersionID", "2.0"),
            new XElement(Cbc + "CustomizationID", "1.0"),
            new XElement(Cbc + "ID", identificador),
            new XElement(Cbc + "ReferenceDate", fechaComprobantes.ToString("yyyy-MM-dd")),
            new XElement(Cbc + "IssueDate", fechaGeneracion.ToString("yyyy-MM-dd")),
            FirmaReferencia(emisor),
            EmisorResumen(emisor),
            facturas.Select((f, i) => new XElement(Sac + "VoidedDocumentsLine",
                new XElement(Cbc + "LineID", i + 1),
                new XElement(Cbc + "DocumentTypeCode", "01"),
                new XElement(Sac + "DocumentSerialID", f.Serie),
                new XElement(Sac + "DocumentNumberID", f.Numero),
                new XElement(Sac + "VoidReasonDescription", new XCData(f.MotivoAnulacion ?? "Anulación de la operación")))));

        return new XDocument(new XDeclaration("1.0", "UTF-8", "no"), raiz);
    }

    private static XElement Linea(int numero, LineaComprobante linea, decimal tasaIgv)
    {
        var montos = CalcularLinea(linea, tasaIgv);
        return new XElement(Cac + "InvoiceLine",
            new XElement(Cbc + "ID", numero),
            new XElement(Cbc + "InvoicedQuantity", new XAttribute("unitCode", "NIU"), linea.Cantidad),
            new XElement(Cbc + "LineExtensionAmount", Pen(), Monto(montos.ValorVenta)),
            new XElement(Cac + "PricingReference",
                new XElement(Cac + "AlternativeConditionPrice",
                    new XElement(Cbc + "PriceAmount", Pen(), Monto(linea.PrecioUnitarioConIgv)),
                    new XElement(Cbc + "PriceTypeCode", "01"))),
            new XElement(Cac + "TaxTotal",
                new XElement(Cbc + "TaxAmount", Pen(), Monto(montos.Igv)),
                new XElement(Cac + "TaxSubtotal",
                    new XElement(Cbc + "TaxableAmount", Pen(), Monto(montos.ValorVenta)),
                    new XElement(Cbc + "TaxAmount", Pen(), Monto(montos.Igv)),
                    new XElement(Cac + "TaxCategory",
                        new XElement(Cbc + "Percent", tasaIgv.ToString("0.00", CultureInfo.InvariantCulture)),
                        // 10 = Gravado - Operación onerosa (catálogo 07).
                        new XElement(Cbc + "TaxExemptionReasonCode", "10"),
                        EsquemaIgv()))),
            new XElement(Cac + "Item", new XElement(Cbc + "Description", new XCData(linea.Descripcion))),
            new XElement(Cac + "Price",
                new XElement(Cbc + "PriceAmount", Pen(), montos.ValorUnitario.ToString("0.00########", CultureInfo.InvariantCulture))));
    }

    private static XElement TotalIgv(decimal gravadas, decimal igv) =>
        new(Cac + "TaxTotal",
            new XElement(Cbc + "TaxAmount", Pen(), Monto(igv)),
            new XElement(Cac + "TaxSubtotal",
                new XElement(Cbc + "TaxableAmount", Pen(), Monto(gravadas)),
                new XElement(Cbc + "TaxAmount", Pen(), Monto(igv)),
                new XElement(Cac + "TaxCategory", EsquemaIgv())));

    private static XElement EsquemaIgv() =>
        new(Cac + "TaxScheme",
            new XElement(Cbc + "ID", "1000"),
            new XElement(Cbc + "Name", "IGV"),
            new XElement(Cbc + "TaxTypeCode", "VAT"));

    private static XElement EmisorResumen(SunatOptions emisor) =>
        new(Cac + "AccountingSupplierParty",
            new XElement(Cbc + "CustomerAssignedAccountID", emisor.Ruc),
            new XElement(Cbc + "AdditionalAccountID", "6"),
            new XElement(Cac + "Party",
                new XElement(Cac + "PartyLegalEntity",
                    new XElement(Cbc + "RegistrationName", new XCData(emisor.RazonSocial)))));

    private static XElement ExtensionesVacias() =>
        new(Ext + "UBLExtensions",
            new XElement(Ext + "UBLExtension", new XElement(Ext + "ExtensionContent")));

    private static XElement FirmaReferencia(SunatOptions emisor) =>
        new(Cac + "Signature",
            new XElement(Cbc + "ID", IdFirma),
            new XElement(Cac + "SignatoryParty",
                new XElement(Cac + "PartyIdentification", new XElement(Cbc + "ID", emisor.Ruc)),
                new XElement(Cac + "PartyName", new XElement(Cbc + "Name", new XCData(emisor.RazonSocial)))),
            new XElement(Cac + "DigitalSignatureAttachment",
                new XElement(Cac + "ExternalReference", new XElement(Cbc + "URI", "#" + IdFirma))));

    private static XAttribute Pen() => new("currencyID", "PEN");

    private static string Monto(decimal valor) => valor.ToString("0.00", CultureInfo.InvariantCulture);
}
