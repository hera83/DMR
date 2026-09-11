using System.Xml.Serialization;

namespace DMR.Services.Dmr.Dtos.Xml;

/// <summary>Maps &lt;ns:FarveTypeStruktur&gt; — a color code/name, nested inside
/// <see cref="KoeretoejFarveStrukturXml"/>. Confirmed 1:1 with its parent across a 6,000+ vehicle sample
/// (see services/dmr/docs), so it is not modeled as a repeatable child table.</summary>
[XmlType(Namespace = DmrXmlNamespace.Value)]
public sealed class FarveTypeStrukturXml
{
    [XmlElement("FarveTypeNummer")]
    public long? Nummer { get; set; }

    [XmlElement("FarveTypeNavn")]
    public string? Navn { get; set; }
}
