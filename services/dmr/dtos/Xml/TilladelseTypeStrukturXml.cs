using System.Xml.Serialization;

namespace DMR.Services.Dmr.Dtos.Xml;

/// <summary>Maps &lt;ns:TilladelseTypeStruktur&gt; — a permit type code/name (e.g. "Vognmandskørsel"),
/// nested inside <see cref="TilladelseStrukturXml"/>.</summary>
[XmlType(Namespace = DmrXmlNamespace.Value)]
public sealed class TilladelseTypeStrukturXml
{
    [XmlElement("TilladelseTypeNummer")]
    public long? Nummer { get; set; }

    [XmlElement("TilladelseTypeNavn")]
    public string? Navn { get; set; }
}
