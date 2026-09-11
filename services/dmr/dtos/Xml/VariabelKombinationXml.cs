using System.Xml.Serialization;

namespace DMR.Services.Dmr.Dtos.Xml;

/// <summary>Maps &lt;ns:VariabelKombination&gt; — the "Variabel Kombination" detail of a
/// &lt;ns:TilladelseTypeDetaljeValg&gt;, holding a reference to the coupled vehicle. Nested inside
/// <see cref="TilladelseTypeDetaljeValgXml"/>.</summary>
[XmlType(Namespace = DmrXmlNamespace.Value)]
public sealed class VariabelKombinationXml
{
    [XmlElement("KoeretoejGenerelIdentifikatorStruktur")]
    public KoeretoejGenerelIdentifikatorStrukturXml? KoeretoejGenerelIdentifikatorStruktur { get; set; }
}
