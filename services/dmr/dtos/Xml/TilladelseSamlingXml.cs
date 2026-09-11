using System.Xml.Serialization;

namespace DMR.Services.Dmr.Dtos.Xml;

/// <summary>Maps &lt;ns:TilladelseSamling&gt;, nested inside <see cref="StatistikXml"/>.</summary>
[XmlType(Namespace = DmrXmlNamespace.Value)]
public sealed class TilladelseSamlingXml
{
    [XmlElement("Tilladelse")]
    public List<TilladelseItemXml>? Tilladelse { get; set; }
}
