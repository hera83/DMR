using System.Xml.Serialization;

namespace DMR.Services.Dmr.Dtos.Xml;

/// <summary>Maps &lt;ns:TilladelseStruktur&gt; — one permit (up to 4 observed per vehicle), nested inside
/// <see cref="TilladelseItemXml"/>. Written to the <c>KoeretoejTilladelse</c> child table — see services/dmr/docs.</summary>
[XmlType(Namespace = DmrXmlNamespace.Value)]
public sealed class TilladelseStrukturXml
{
    [XmlElement("TilladelseGyldigFra")]
    public string? GyldigFra { get; set; }

    [XmlElement("TilladelseKommentar")]
    public string? Kommentar { get; set; }

    [XmlElement("TilladelseTypeStruktur")]
    public TilladelseTypeStrukturXml? TypeStruktur { get; set; }

    [XmlElement("TilladelseTypeDetaljeValg")]
    public TilladelseTypeDetaljeValgXml? TypeDetaljeValg { get; set; }
}
