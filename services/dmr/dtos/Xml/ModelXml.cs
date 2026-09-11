using System.Xml.Serialization;

namespace DMR.Services.Dmr.Dtos.Xml;

/// <summary>Maps &lt;ns:Model&gt; — the vehicle model code/name, nested inside
/// <see cref="KoeretoejBetegnelseStrukturXml"/>.</summary>
[XmlType(Namespace = DmrXmlNamespace.Value)]
public sealed class ModelXml
{
    [XmlElement("KoeretoejModelTypeNummer")]
    public long? Nummer { get; set; }

    [XmlElement("KoeretoejModelTypeNavn")]
    public string? Navn { get; set; }
}
