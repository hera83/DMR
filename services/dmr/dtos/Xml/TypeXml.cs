using System.Xml.Serialization;

namespace DMR.Services.Dmr.Dtos.Xml;

/// <summary>Maps &lt;ns:Type&gt; — the vehicle (type-approval) type code/name, nested inside
/// <see cref="KoeretoejBetegnelseStrukturXml"/>.</summary>
[XmlType(Namespace = DmrXmlNamespace.Value)]
public sealed class TypeXml
{
    [XmlElement("KoeretoejTypeTypeNummer")]
    public long? Nummer { get; set; }

    [XmlElement("KoeretoejTypeTypeNavn")]
    public string? Navn { get; set; }
}
