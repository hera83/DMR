using System.Xml.Serialization;

namespace DMR.Services.Dmr.Dtos.Xml;

/// <summary>Maps &lt;ns:KoeretoejSupplerendeKarrosseriSamlingStruktur&gt;, nested inside
/// <see cref="KoeretoejOplysningGrundStrukturXml"/>. Written to the <c>KoeretoejSupplerendeKarrosseri</c>
/// child table — see services/dmr/docs.</summary>
[XmlType(Namespace = DmrXmlNamespace.Value)]
public sealed class KoeretoejSupplerendeKarrosseriSamlingStrukturXml
{
    [XmlElement("KoeretoejSupplerendeKarrosseriSamling")]
    public List<KoeretoejSupplerendeKarrosseriSamlingItemXml>? KoeretoejSupplerendeKarrosseriSamling { get; set; }
}
