using System.Xml.Serialization;

namespace DMR.Services.Dmr.Dtos.Xml;

/// <summary>Maps &lt;ns:KoeretoejAnvendelseSamlingStruktur&gt; — present only when a vehicle has more than
/// one usage; a repeated &lt;ns:KoeretoejAnvendelseSamling&gt; per usage. When absent, the vehicle's single
/// usage is carried by &lt;ns:KoeretoejAnvendelseStruktur&gt; directly (see <see cref="StatistikXml.KoeretoejAnvendelseStruktur"/>).</summary>
[XmlType(Namespace = DmrXmlNamespace.Value)]
public sealed class KoeretoejAnvendelseSamlingStrukturXml
{
    [XmlElement("KoeretoejAnvendelseSamling")]
    public List<KoeretoejAnvendelseSamlingItemXml>? KoeretoejAnvendelseSamling { get; set; }
}
