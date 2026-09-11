using System.Xml.Serialization;

namespace DMR.Services.Dmr.Dtos.Xml;

/// <summary>Maps &lt;ns:NormTypeStruktur&gt; — the vehicle's emission norm code/name (e.g. "Euro V"), nested
/// inside <see cref="KoeretoejNormStrukturXml"/>.</summary>
[XmlType(Namespace = DmrXmlNamespace.Value)]
public sealed class NormTypeStrukturXml
{
    [XmlElement("NormTypeNummer")]
    public long? Nummer { get; set; }

    [XmlElement("NormTypeNavn")]
    public string? Navn { get; set; }
}
