using System.Xml.Serialization;

namespace DMR.Services.Dmr.Dtos.Xml;

/// <summary>Maps &lt;ns:KoeretoejGenerelIdentifikatorStruktur&gt;, nested inside <see cref="VariabelKombinationXml"/>.</summary>
[XmlType(Namespace = DmrXmlNamespace.Value)]
public sealed class KoeretoejGenerelIdentifikatorStrukturXml
{
    [XmlElement("KoeretoejGenerelIdentifikatorValg")]
    public KoeretoejGenerelIdentifikatorValgXml? Valg { get; set; }
}
