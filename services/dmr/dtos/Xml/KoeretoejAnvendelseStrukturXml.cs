using System.Xml.Serialization;

namespace DMR.Services.Dmr.Dtos.Xml;

/// <summary>Maps &lt;ns:KoeretoejAnvendelseStruktur&gt; — a single vehicle usage code/name pair (e.g.
/// "1 / Privat personkørsel"). Appears once as the vehicle's primary usage directly under &lt;Statistik&gt;,
/// and reused as the item shape inside <see cref="KoeretoejAnvendelseSamlingItemXml"/> when a vehicle has
/// more than one usage. See services/dmr/docs for the XML-DTO exception to request/response pairing.</summary>
[XmlType(Namespace = DmrXmlNamespace.Value)]
public sealed class KoeretoejAnvendelseStrukturXml
{
    [XmlElement("KoeretoejAnvendelseNummer")]
    public int? Nummer { get; set; }

    [XmlElement("KoeretoejAnvendelseNavn")]
    public string? Navn { get; set; }
}
