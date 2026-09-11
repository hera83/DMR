using System.Xml.Serialization;

namespace DMR.Services.Dmr.Dtos.Xml;

/// <summary>
/// Maps one &lt;ns:Statistik&gt; element — a single vehicle record — from Motorstyrelsen's
/// "ESStatistikListeModtag" DMR export. This is the unit <c>DmrService</c> deserializes and imports one at
/// a time while streaming through the multi-GB source file; it is never the root of the whole document
/// (that's &lt;ns:ESStatistikListeModtag_I&gt;/&lt;ns:StatistikSamling&gt;, which DmrService navigates with
/// a plain <see cref="System.Xml.XmlReader"/> rather than deserializing as a whole). See services/dmr/docs.
/// </summary>
[XmlRoot("Statistik", Namespace = DmrXmlNamespace.Value)]
public sealed class StatistikXml
{
    [XmlElement("KoeretoejIdent")]
    public string? KoeretoejIdent { get; set; }

    [XmlElement("KoeretoejArtNummer")]
    public int? KoeretoejArtNummer { get; set; }

    [XmlElement("KoeretoejArtNavn")]
    public string? KoeretoejArtNavn { get; set; }

    [XmlElement("KoeretoejAnvendelseStruktur")]
    public KoeretoejAnvendelseStrukturXml? KoeretoejAnvendelseStruktur { get; set; }

    [XmlElement("KoeretoejAnvendelseSamlingStruktur")]
    public KoeretoejAnvendelseSamlingStrukturXml? KoeretoejAnvendelseSamlingStruktur { get; set; }

    [XmlElement("LeasingGyldigFra")]
    public string? LeasingGyldigFra { get; set; }

    [XmlElement("LeasingGyldigTil")]
    public string? LeasingGyldigTil { get; set; }

    [XmlElement("RegistreringNummerNummer")]
    public string? RegistreringNummerNummer { get; set; }

    [XmlElement("RegistreringNummerRettighedGyldigFra")]
    public string? RegistreringNummerRettighedGyldigFra { get; set; }

    [XmlElement("RegistreringNummerRettighedGyldigTil")]
    public string? RegistreringNummerRettighedGyldigTil { get; set; }

    [XmlElement("RegistreringNummerUdloebDato")]
    public string? RegistreringNummerUdloebDato { get; set; }

    [XmlElement("KoeretoejOplysningGrundStruktur")]
    public KoeretoejOplysningGrundStrukturXml? KoeretoejOplysningGrundStruktur { get; set; }

    [XmlElement("SynResultatStruktur")]
    public SynResultatStrukturXml? SynResultatStruktur { get; set; }

    [XmlElement("KoeretoejRegistreringStatus")]
    public string? KoeretoejRegistreringStatus { get; set; }

    [XmlElement("KoeretoejRegistreringStatusDato")]
    public string? KoeretoejRegistreringStatusDato { get; set; }

    [XmlElement("TilladelseSamling")]
    public TilladelseSamlingXml? TilladelseSamling { get; set; }
}
