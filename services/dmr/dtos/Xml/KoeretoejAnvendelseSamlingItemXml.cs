using System.Xml.Serialization;

namespace DMR.Services.Dmr.Dtos.Xml;

/// <summary>Maps one &lt;ns:KoeretoejAnvendelseSamling&gt; entry — a single item in a vehicle's list of
/// usages, wrapping a nested &lt;ns:KoeretoejAnvendelseStruktur&gt;. See
/// <see cref="KoeretoejAnvendelseSamlingStrukturXml"/>.</summary>
[XmlType(Namespace = DmrXmlNamespace.Value)]
public sealed class KoeretoejAnvendelseSamlingItemXml
{
    [XmlElement("KoeretoejAnvendelseStruktur")]
    public KoeretoejAnvendelseStrukturXml? Struktur { get; set; }
}
