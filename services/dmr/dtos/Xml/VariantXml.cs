using System.Xml.Serialization;

namespace DMR.Services.Dmr.Dtos.Xml;

/// <summary>Maps &lt;ns:Variant&gt; — the vehicle variant code/name, nested inside
/// <see cref="KoeretoejBetegnelseStrukturXml"/>.</summary>
[XmlType(Namespace = DmrXmlNamespace.Value)]
public sealed class VariantXml
{
    [XmlElement("KoeretoejVariantTypeNummer")]
    public long? Nummer { get; set; }

    [XmlElement("KoeretoejVariantTypeNavn")]
    public string? Navn { get; set; }
}
