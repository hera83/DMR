using System.Xml.Serialization;

namespace DMR.Services.Dmr.Dtos.Xml;

/// <summary>Maps &lt;ns:TilladelseTypeDetaljeValg&gt; — a permit-type-specific detail "choice", nested
/// inside <see cref="TilladelseStrukturXml"/>. Content varies by permit type; only the "Variabel Kombination"
/// variant (a reference to a coupled vehicle) has been observed in the source sample — other variants are
/// simply left null rather than failing the import, see the <c>UnknownElement</c> handling in
/// <c>DmrService</c> and services/dmr/docs "Known gaps".</summary>
[XmlType(Namespace = DmrXmlNamespace.Value)]
public sealed class TilladelseTypeDetaljeValgXml
{
    [XmlElement("VariabelKombination")]
    public VariabelKombinationXml? VariabelKombination { get; set; }
}
