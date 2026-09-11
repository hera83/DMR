using System.Xml.Serialization;

namespace DMR.Services.Dmr.Dtos.Xml;

/// <summary>Maps &lt;ns:KoeretoejBlokeringAarsagListe&gt; — a list of blocking reasons, nested inside
/// <see cref="KoeretoejBlokeringAarsagListeStrukturXml"/>. Only a single-reason example was seen in the
/// source sample; the list shape is kept in case a vehicle can carry more than one — see services/dmr/docs.</summary>
[XmlType(Namespace = DmrXmlNamespace.Value)]
public sealed class KoeretoejBlokeringAarsagListeXml
{
    [XmlElement("KoeretoejBlokeringAarsag")]
    public List<KoeretoejBlokeringAarsagXml>? KoeretoejBlokeringAarsag { get; set; }
}
