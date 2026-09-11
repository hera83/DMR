using System.Xml.Serialization;

namespace DMR.Services.Dmr.Dtos.Xml;

/// <summary>Maps &lt;ns:KoeretoejBlokeringAarsagListeStruktur&gt;, nested inside
/// <see cref="KoeretoejOplysningGrundStrukturXml"/>. Written to the <c>KoeretoejBlokeringAarsag</c> child
/// table — see services/dmr/docs.</summary>
[XmlType(Namespace = DmrXmlNamespace.Value)]
public sealed class KoeretoejBlokeringAarsagListeStrukturXml
{
    [XmlElement("KoeretoejBlokeringAarsagListe")]
    public KoeretoejBlokeringAarsagListeXml? KoeretoejBlokeringAarsagListe { get; set; }
}
