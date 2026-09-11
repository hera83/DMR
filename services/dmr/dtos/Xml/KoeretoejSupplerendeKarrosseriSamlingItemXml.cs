using System.Xml.Serialization;

namespace DMR.Services.Dmr.Dtos.Xml;

/// <summary>Maps one &lt;ns:KoeretoejSupplerendeKarrosseriSamling&gt; entry (up to 4 observed per vehicle),
/// wrapping a nested &lt;ns:KoeretoejSupplerendeKarrosseriTypeStruktur&gt;.</summary>
[XmlType(Namespace = DmrXmlNamespace.Value)]
public sealed class KoeretoejSupplerendeKarrosseriSamlingItemXml
{
    [XmlElement("KoeretoejSupplerendeKarrosseriTypeStruktur")]
    public KoeretoejSupplerendeKarrosseriTypeStrukturXml? TypeStruktur { get; set; }
}
