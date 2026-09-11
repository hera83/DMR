using System.Xml.Serialization;

namespace DMR.Services.Dmr.Dtos.Xml;

/// <summary>Maps one &lt;ns:KoeretoejDrivmiddelSamling&gt; entry, wrapping a nested &lt;ns:DrivmiddelStruktur&gt;.</summary>
[XmlType(Namespace = DmrXmlNamespace.Value)]
public sealed class KoeretoejDrivmiddelSamlingItemXml
{
    [XmlElement("DrivmiddelStruktur")]
    public DrivmiddelStrukturXml? DrivmiddelStruktur { get; set; }
}
