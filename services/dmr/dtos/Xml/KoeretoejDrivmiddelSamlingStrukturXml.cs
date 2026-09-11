using System.Xml.Serialization;

namespace DMR.Services.Dmr.Dtos.Xml;

/// <summary>Maps &lt;ns:KoeretoejDrivmiddelSamlingStruktur&gt;, nested inside <see cref="KoeretoejMotorStrukturXml"/>.</summary>
[XmlType(Namespace = DmrXmlNamespace.Value)]
public sealed class KoeretoejDrivmiddelSamlingStrukturXml
{
    [XmlElement("KoeretoejDrivmiddelSamling")]
    public List<KoeretoejDrivmiddelSamlingItemXml>? KoeretoejDrivmiddelSamling { get; set; }
}
