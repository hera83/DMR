using System.Xml.Serialization;

namespace DMR.Services.Dmr.Dtos.Xml;

/// <summary>Maps one &lt;ns:Tilladelse&gt; entry, wrapping a nested &lt;ns:TilladelseStruktur&gt;.</summary>
[XmlType(Namespace = DmrXmlNamespace.Value)]
public sealed class TilladelseItemXml
{
    [XmlElement("TilladelseStruktur")]
    public TilladelseStrukturXml? TilladelseStruktur { get; set; }
}
