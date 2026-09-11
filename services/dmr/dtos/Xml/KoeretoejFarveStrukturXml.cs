using System.Xml.Serialization;

namespace DMR.Services.Dmr.Dtos.Xml;

/// <summary>Maps &lt;ns:KoeretoejFarveStruktur&gt;, nested inside <see cref="KoeretoejOplysningGrundStrukturXml"/>.</summary>
[XmlType(Namespace = DmrXmlNamespace.Value)]
public sealed class KoeretoejFarveStrukturXml
{
    [XmlElement("FarveTypeStruktur")]
    public FarveTypeStrukturXml? FarveTypeStruktur { get; set; }
}
