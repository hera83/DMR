using System.Xml.Serialization;

namespace DMR.Services.Dmr.Dtos.Xml;

/// <summary>Maps &lt;ns:KoeretoejUdstyrSamlingStruktur&gt;, nested inside <see cref="KoeretoejOplysningGrundStrukturXml"/>.</summary>
[XmlType(Namespace = DmrXmlNamespace.Value)]
public sealed class KoeretoejUdstyrSamlingStrukturXml
{
    [XmlElement("KoeretoejUdstyrSamling")]
    public List<KoeretoejUdstyrSamlingItemXml>? KoeretoejUdstyrSamling { get; set; }
}
