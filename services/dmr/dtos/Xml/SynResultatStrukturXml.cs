using System.Xml.Serialization;

namespace DMR.Services.Dmr.Dtos.Xml;

/// <summary>Maps &lt;ns:SynResultatStruktur&gt; — the vehicle's (single, most recent) inspection result,
/// nested inside <see cref="StatistikXml"/>. Confirmed max 1 per vehicle across a 6,000+ vehicle sample,
/// so it is flattened onto the main table rather than a child table — see services/dmr/docs.</summary>
[XmlType(Namespace = DmrXmlNamespace.Value)]
public sealed class SynResultatStrukturXml
{
    [XmlElement("SynResultatSynsType")]
    public string? SynsType { get; set; }

    [XmlElement("SynResultatSynsDato")]
    public string? SynsDato { get; set; }

    [XmlElement("SynResultatSynsResultat")]
    public string? SynsResultat { get; set; }

    [XmlElement("SynResultatSynStatus")]
    public string? SynStatus { get; set; }

    [XmlElement("SynResultatSynStatusDato")]
    public string? SynStatusDato { get; set; }
}
