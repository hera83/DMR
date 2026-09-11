using System.Xml.Serialization;

namespace DMR.Services.Dmr.Dtos.Xml;

/// <summary>Maps &lt;ns:KoeretoejUdstyrTypeStruktur&gt; — one equipment type's code, name and visibility
/// flags, nested inside <see cref="KoeretoejUdstyrStrukturXml"/>.</summary>
[XmlType(Namespace = DmrXmlNamespace.Value)]
public sealed class KoeretoejUdstyrTypeStrukturXml
{
    [XmlElement("KoeretoejUdstyrTypeNummer")]
    public long? Nummer { get; set; }

    [XmlElement("KoeretoejUdstyrTypeNavn")]
    public string? Navn { get; set; }

    [XmlElement("KoeretoejUdstyrTypeVisesVedSyn")]
    public bool? VisesVedSyn { get; set; }

    [XmlElement("KoeretoejUdstyrTypeVisesVedForespoergsel")]
    public bool? VisesVedForespoergsel { get; set; }

    [XmlElement("KoeretoejUdstyrTypeVisesVedStandardOprettelse")]
    public bool? VisesVedStandardOprettelse { get; set; }
}
