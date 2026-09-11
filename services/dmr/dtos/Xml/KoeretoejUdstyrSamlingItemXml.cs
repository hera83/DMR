using System.Xml.Serialization;

namespace DMR.Services.Dmr.Dtos.Xml;

/// <summary>Maps one &lt;ns:KoeretoejUdstyrSamling&gt; entry, wrapping a nested &lt;ns:KoeretoejUdstyrStruktur&gt;.</summary>
[XmlType(Namespace = DmrXmlNamespace.Value)]
public sealed class KoeretoejUdstyrSamlingItemXml
{
    [XmlElement("KoeretoejUdstyrStruktur")]
    public KoeretoejUdstyrStrukturXml? UdstyrStruktur { get; set; }
}
