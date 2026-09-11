using System.Xml.Serialization;

namespace DMR.Services.Dmr.Dtos.Xml;

/// <summary>Maps &lt;ns:KoeretoejBlokeringAarsag&gt; — one blocking-reason code/name (e.g. "Totalskadet
/// køretøj"), nested inside <see cref="KoeretoejBlokeringAarsagListeXml"/>.</summary>
[XmlType(Namespace = DmrXmlNamespace.Value)]
public sealed class KoeretoejBlokeringAarsagXml
{
    [XmlElement("KoeretoejBlokeringAarsagTypeNummer")]
    public long? TypeNummer { get; set; }

    [XmlElement("KoeretoejBlokeringAarsagTypeNavn")]
    public string? TypeNavn { get; set; }
}
