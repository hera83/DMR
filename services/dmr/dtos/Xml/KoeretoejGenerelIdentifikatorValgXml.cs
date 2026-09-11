using System.Xml.Serialization;

namespace DMR.Services.Dmr.Dtos.Xml;

/// <summary>Maps &lt;ns:KoeretoejGenerelIdentifikatorValg&gt; — a reference to another vehicle by its
/// KoeretoejIdent, nested inside <see cref="KoeretoejGenerelIdentifikatorStrukturXml"/>. Only the
/// KoeretoejIdent variant of this choice has been observed (used for "Variabel Kombination" permits that
/// reference a coupled vehicle) — see services/dmr/docs.</summary>
[XmlType(Namespace = DmrXmlNamespace.Value)]
public sealed class KoeretoejGenerelIdentifikatorValgXml
{
    [XmlElement("KoeretoejIdent")]
    public string? KoeretoejIdent { get; set; }
}
