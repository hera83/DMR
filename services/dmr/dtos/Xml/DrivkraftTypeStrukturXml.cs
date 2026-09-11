using System.Xml.Serialization;

namespace DMR.Services.Dmr.Dtos.Xml;

/// <summary>Maps &lt;ns:DrivkraftTypeStruktur&gt; — a power-source code/name (e.g. "Diesel", "El"), nested
/// inside <see cref="DrivmiddelStrukturXml"/>.</summary>
[XmlType(Namespace = DmrXmlNamespace.Value)]
public sealed class DrivkraftTypeStrukturXml
{
    [XmlElement("DrivkraftTypeNummer")]
    public long? Nummer { get; set; }

    [XmlElement("DrivkraftTypeNavn")]
    public string? Navn { get; set; }
}
