using System.Xml.Serialization;

namespace DMR.Services.Dmr.Dtos.Xml;

/// <summary>Maps &lt;ns:KoeretoejOplysningGrundStruktur&gt; — the bulk of a vehicle's technical data, nested
/// inside <see cref="StatistikXml"/>. Date/time fields are kept as raw strings here (not parsed) so a
/// malformed value can never abort the whole streaming import — <c>DmrService</c> parses them when writing
/// rows and logs (rather than throws) on failure. See services/dmr/docs.</summary>
[XmlType(Namespace = DmrXmlNamespace.Value)]
public sealed class KoeretoejOplysningGrundStrukturXml
{
    [XmlElement("KoeretoejOplysningOprettetUdFra")]
    public string? OprettetUdFra { get; set; }

    [XmlElement("KoeretoejOplysningStatus")]
    public string? Status { get; set; }

    [XmlElement("KoeretoejOplysningStatusDato")]
    public string? StatusDato { get; set; }

    [XmlElement("KoeretoejOplysningFoersteRegistreringDato")]
    public string? FoersteRegistreringDato { get; set; }

    [XmlElement("KoeretoejOplysningStelNummer")]
    public string? StelNummer { get; set; }

    [XmlElement("KoeretoejOplysningStelNummerAnbringelse")]
    public string? StelNummerAnbringelse { get; set; }

    [XmlElement("KoeretoejOplysningTotalVaegt")]
    public int? TotalVaegt { get; set; }

    [XmlElement("KoeretoejOplysningEgenVaegt")]
    public int? EgenVaegt { get; set; }

    [XmlElement("KoeretoejOplysningTekniskTotalVaegt")]
    public int? TekniskTotalVaegt { get; set; }

    [XmlElement("KoeretoejOplysningAkselAntal")]
    public int? AkselAntal { get; set; }

    [XmlElement("KoeretoejOplysningAkselAfstand")]
    public string? AkselAfstand { get; set; }

    [XmlElement("KoeretoejOplysningStoersteAkselTryk")]
    public int? StoersteAkselTryk { get; set; }

    [XmlElement("KoeretoejOplysningTilkoblingMulighed")]
    public bool? TilkoblingMulighed { get; set; }

    [XmlElement("KoeretoejOplysningTilkoblingsvaegtUdenBremser")]
    public int? TilkoblingsvaegtUdenBremser { get; set; }

    [XmlElement("KoeretoejOplysningTilkoblingsvaegtMedBremser")]
    public int? TilkoblingsvaegtMedBremser { get; set; }

    [XmlElement("KoeretoejOplysningTypeAnmeldelseNummer")]
    public string? TypeAnmeldelseNummer { get; set; }

    [XmlElement("KoeretoejOplysningTypeGodkendelseNummer")]
    public string? TypeGodkendelseNummer { get; set; }

    [XmlElement("KoeretoejOplysningTypegodkendtKategori")]
    public string? TypegodkendtKategori { get; set; }

    [XmlElement("KoeretoejOplysningEUVariant")]
    public string? EUVariant { get; set; }

    [XmlElement("KoeretoejOplysningEUVersion")]
    public string? EUVersion { get; set; }

    [XmlElement("KoeretoejOplysningKommentar")]
    public string? Kommentar { get; set; }

    [XmlElement("KoeretoejOplysningAntalDoere")]
    public int? AntalDoere { get; set; }

    [XmlElement("KoeretoejOplysningAntalGear")]
    public int? AntalGear { get; set; }

    [XmlElement("KoeretoejOplysningTrafikskade")]
    public bool? Trafikskade { get; set; }

    [XmlElement("KoeretoejOplysningNCAPTest")]
    public bool? NCAPTest { get; set; }

    [XmlElement("KoeretoejOplysningKoeretoejstand")]
    public string? Koeretoejstand { get; set; }

    // Not a plain count: seen as a comma-separated list of driven axle positions (e.g. "1, 2" for an
    // AWD vehicle), so — like AkselAfstand above — kept as text rather than int.
    [XmlElement("KoeretoejOplysningTraekkendeAksler")]
    public string? TraekkendeAksler { get; set; }

    [XmlElement("KoeretoejOplysningEgnetTilTaxi")]
    public bool? EgnetTilTaxi { get; set; }

    [XmlElement("KoeretoejOplysningKoereklarVaegtMaksimum")]
    public int? KoereklarVaegtMaksimum { get; set; }

    [XmlElement("KoeretoejOplysningKoereklarVaegtMinimum")]
    public int? KoereklarVaegtMinimum { get; set; }

    [XmlElement("KoeretoejOplysningMaksimumHastighed")]
    public int? MaksimumHastighed { get; set; }

    [XmlElement("KoeretoejOplysningModelAar")]
    public int? ModelAar { get; set; }

    [XmlElement("KoeretoejOplysningOevrigtUdstyr")]
    public string? OevrigtUdstyr { get; set; }

    [XmlElement("KoeretoejOplysningPaahaengVognTotalVaegt")]
    public int? PaahaengVognTotalVaegt { get; set; }

    [XmlElement("KoeretoejOplysningPassagerAntal")]
    public int? PassagerAntal { get; set; }

    [XmlElement("KoeretoejOplysningSaettevognTilladtAkselTryk")]
    public int? SaettevognTilladtAkselTryk { get; set; }

    [XmlElement("KoeretoejOplysningSiddepladserMaksimum")]
    public int? SiddepladserMaksimum { get; set; }

    [XmlElement("KoeretoejOplysningSiddepladserMinimum")]
    public int? SiddepladserMinimum { get; set; }

    [XmlElement("KoeretoejOplysningSkammelBelastning")]
    public int? SkammelBelastning { get; set; }

    [XmlElement("KoeretoejOplysningSkatteAkselAntal")]
    public int? SkatteAkselAntal { get; set; }

    [XmlElement("KoeretoejOplysningSkatteAkselTryk")]
    public int? SkatteAkselTryk { get; set; }

    [XmlElement("KoeretoejOplysningSporviddenBagest")]
    public int? SporviddenBagest { get; set; }

    [XmlElement("KoeretoejOplysningSporviddenForrest")]
    public int? SporviddenForrest { get; set; }

    [XmlElement("KoeretoejOplysningStaapladserMinimum")]
    public int? StaapladserMinimum { get; set; }

    // double, not decimal — see the note on KoeretoejMiljoeOplysningStrukturXml.Partikler.
    [XmlElement("KoeretoejOplysningVVaerdiLuft")]
    public double? VVaerdiLuft { get; set; }

    [XmlElement("KoeretoejOplysningVVaerdiMekanisk")]
    public double? VVaerdiMekanisk { get; set; }

    [XmlElement("KoeretoejOplysningVeteranKoeretoejOriginal")]
    public bool? VeteranKoeretoejOriginal { get; set; }

    [XmlElement("KoeretoejOplysningVogntogVaegt")]
    public int? VogntogVaegt { get; set; }

    [XmlElement("KoeretoejOplysningFaelgDaek")]
    public string? FaelgDaek { get; set; }

    [XmlElement("KoeretoejOplysningFabrikant")]
    public string? Fabrikant { get; set; }

    [XmlElement("KoeretoejOplysning30PctVarevogn")]
    public bool? Er30PctVarevogn { get; set; }

    [XmlElement("KoeretoejBetegnelseStruktur")]
    public KoeretoejBetegnelseStrukturXml? KoeretoejBetegnelseStruktur { get; set; }

    [XmlElement("KoeretoejFarveStruktur")]
    public KoeretoejFarveStrukturXml? KoeretoejFarveStruktur { get; set; }

    [XmlElement("KarrosseriTypeStruktur")]
    public KarrosseriTypeStrukturXml? KarrosseriTypeStruktur { get; set; }

    [XmlElement("KoeretoejSupplerendeKarrosseriSamlingStruktur")]
    public KoeretoejSupplerendeKarrosseriSamlingStrukturXml? KoeretoejSupplerendeKarrosseriSamlingStruktur { get; set; }

    [XmlElement("KoeretoejNormStruktur")]
    public KoeretoejNormStrukturXml? KoeretoejNormStruktur { get; set; }

    [XmlElement("KoeretoejMiljoeOplysningStruktur")]
    public KoeretoejMiljoeOplysningStrukturXml? KoeretoejMiljoeOplysningStruktur { get; set; }

    [XmlElement("KoeretoejMotorStruktur")]
    public KoeretoejMotorStrukturXml? KoeretoejMotorStruktur { get; set; }

    [XmlElement("KoeretoejBlokeringAarsagListeStruktur")]
    public KoeretoejBlokeringAarsagListeStrukturXml? KoeretoejBlokeringAarsagListeStruktur { get; set; }

    [XmlElement("KoeretoejUdstyrSamlingStruktur")]
    public KoeretoejUdstyrSamlingStrukturXml? KoeretoejUdstyrSamlingStruktur { get; set; }
}
