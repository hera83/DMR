using System.Xml.Serialization;

namespace DMR.Services.Dmr.Dtos.Xml;

/// <summary>Maps &lt;ns:KoeretoejNormStruktur&gt;, nested inside <see cref="KoeretoejOplysningGrundStrukturXml"/>.</summary>
[XmlType(Namespace = DmrXmlNamespace.Value)]
public sealed class KoeretoejNormStrukturXml
{
    [XmlElement("NormTypeStruktur")]
    public NormTypeStrukturXml? NormTypeStruktur { get; set; }
}
