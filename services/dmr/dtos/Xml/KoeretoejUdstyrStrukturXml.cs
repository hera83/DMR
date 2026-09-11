using System.Xml.Serialization;

namespace DMR.Services.Dmr.Dtos.Xml;

/// <summary>Maps &lt;ns:KoeretoejUdstyrStruktur&gt; — one equipment entry (up to 26 observed per vehicle),
/// nested inside <see cref="KoeretoejUdstyrSamlingItemXml"/>. Written to the <c>KoeretoejUdstyr</c> child
/// table — see services/dmr/docs.</summary>
[XmlType(Namespace = DmrXmlNamespace.Value)]
public sealed class KoeretoejUdstyrStrukturXml
{
    [XmlElement("KoeretoejUdstyrAntal")]
    public int? Antal { get; set; }

    [XmlElement("KoeretoejUdstyrTypeStruktur")]
    public KoeretoejUdstyrTypeStrukturXml? TypeStruktur { get; set; }
}
