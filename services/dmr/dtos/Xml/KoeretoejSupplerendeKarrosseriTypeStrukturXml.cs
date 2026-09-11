using System.Xml.Serialization;

namespace DMR.Services.Dmr.Dtos.Xml;

/// <summary>Maps &lt;ns:KoeretoejSupplerendeKarrosseriTypeStruktur&gt; — one supplementary body type code/name,
/// nested inside <see cref="KoeretoejSupplerendeKarrosseriSamlingItemXml"/>.</summary>
[XmlType(Namespace = DmrXmlNamespace.Value)]
public sealed class KoeretoejSupplerendeKarrosseriTypeStrukturXml
{
    [XmlElement("SupplerendeKarrosseriTypeNummer")]
    public long? Nummer { get; set; }

    [XmlElement("SupplerendeKarrosseriTypeNavn")]
    public string? Navn { get; set; }
}
