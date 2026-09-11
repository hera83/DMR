using System.Xml.Serialization;

namespace DMR.Services.Dmr.Dtos.Xml;

/// <summary>Maps &lt;ns:KarrosseriTypeStruktur&gt; — the vehicle's body type (e.g. "Stationcar"), nested
/// inside <see cref="KoeretoejOplysningGrundStrukturXml"/>. Unlike <see cref="KoeretoejSupplerendeKarrosseriTypeStrukturXml"/>
/// (supplementary body types, which can repeat) this one has only ever been observed with a name, no code.</summary>
[XmlType(Namespace = DmrXmlNamespace.Value)]
public sealed class KarrosseriTypeStrukturXml
{
    [XmlElement("KarrosseriTypeNavn")]
    public string? Navn { get; set; }
}
