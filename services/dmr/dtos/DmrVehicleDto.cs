namespace DMR.Services.Dmr.Dtos;

/// <summary>
/// One vehicle looked up from the current DMR dataset, mirroring the <c>Koeretoej</c> table's columns 1:1
/// (see <c>DmrService.KoeretoejColumns</c>) plus its child-table collections. Supporting item type nested
/// inside <see cref="DmrLookupVehicleResponseDto"/>, not a standalone request/response pair — it never
/// travels on its own.
///
/// Every <c>INTEGER</c> column comes back as <see cref="long"/>? and every <c>REAL</c> column as
/// <see cref="double"/>? regardless of what the value actually represents (a count, a boolean 0/1 flag,
/// etc.) — this is a raw mirror of the SQLite row, not a reinterpretation of it; see services/dmr/docs for
/// what each field means.
/// </summary>
public class DmrVehicleDto
{
    public string KoeretoejIdent { get; set; } = string.Empty;
    public long? KoeretoejArtNummer { get; set; }
    public string? KoeretoejArtNavn { get; set; }
    public long? AnvendelseNummer { get; set; }
    public string? AnvendelseNavn { get; set; }
    public string? LeasingGyldigFra { get; set; }
    public string? LeasingGyldigTil { get; set; }
    public string? RegistreringNummer { get; set; }
    public string? RegistreringNummerRettighedGyldigFra { get; set; }
    public string? RegistreringNummerRettighedGyldigTil { get; set; }
    public string? RegistreringNummerUdloebDato { get; set; }
    public string? OprettetUdFra { get; set; }
    public string? Status { get; set; }
    public string? StatusDato { get; set; }
    public string? FoersteRegistreringDato { get; set; }
    public string? StelNummer { get; set; }
    public string? StelNummerAnbringelse { get; set; }
    public long? TotalVaegt { get; set; }
    public long? EgenVaegt { get; set; }
    public long? TekniskTotalVaegt { get; set; }
    public long? AkselAntal { get; set; }
    public string? AkselAfstand { get; set; }
    public long? StoersteAkselTryk { get; set; }
    public long? TilkoblingMulighed { get; set; }
    public long? TilkoblingsvaegtUdenBremser { get; set; }
    public long? TilkoblingsvaegtMedBremser { get; set; }
    public string? TypeAnmeldelseNummer { get; set; }
    public string? TypeGodkendelseNummer { get; set; }
    public string? TypegodkendtKategori { get; set; }
    public string? EUVariant { get; set; }
    public string? EUVersion { get; set; }
    public string? Kommentar { get; set; }
    public long? AntalDoere { get; set; }
    public long? AntalGear { get; set; }
    public long? Trafikskade { get; set; }
    public long? NCAPTest { get; set; }
    public string? Koeretoejstand { get; set; }
    public string? TraekkendeAksler { get; set; }
    public long? EgnetTilTaxi { get; set; }
    public long? KoereklarVaegtMaksimum { get; set; }
    public long? KoereklarVaegtMinimum { get; set; }
    public long? MaksimumHastighed { get; set; }
    public long? ModelAar { get; set; }
    public string? OevrigtUdstyr { get; set; }
    public long? PaahaengVognTotalVaegt { get; set; }
    public long? PassagerAntal { get; set; }
    public long? SaettevognTilladtAkselTryk { get; set; }
    public long? SiddepladserMaksimum { get; set; }
    public long? SiddepladserMinimum { get; set; }
    public long? SkammelBelastning { get; set; }
    public long? SkatteAkselAntal { get; set; }
    public long? SkatteAkselTryk { get; set; }
    public long? SporviddenBagest { get; set; }
    public long? SporviddenForrest { get; set; }
    public long? StaapladserMinimum { get; set; }
    public double? VVaerdiLuft { get; set; }
    public double? VVaerdiMekanisk { get; set; }
    public long? VeteranKoeretoejOriginal { get; set; }
    public long? VogntogVaegt { get; set; }
    public string? FaelgDaek { get; set; }
    public string? Fabrikant { get; set; }
    public long? Er30PctVarevogn { get; set; }
    public long? MaerkeNummer { get; set; }
    public string? MaerkeNavn { get; set; }
    public long? ModelNummer { get; set; }
    public string? ModelNavn { get; set; }
    public long? VariantNummer { get; set; }
    public string? VariantNavn { get; set; }
    public long? TypeNummer { get; set; }
    public string? TypeNavn { get; set; }
    public long? FarveNummer { get; set; }
    public string? FarveNavn { get; set; }
    public string? KarrosseriNavn { get; set; }
    public long? NormNummer { get; set; }
    public string? NormNavn { get; set; }
    public double? MiljoeEmissionCO { get; set; }
    public double? MiljoeEmissionHCPlusNOX { get; set; }
    public double? MiljoeEmissionNOX { get; set; }
    public double? MiljoeNyttelastvaerdi { get; set; }
    public long? MiljoePartikelFilter { get; set; }
    public double? MiljoePartikler { get; set; }
    public double? MiljoeRoegtaethed { get; set; }
    public long? MiljoeRoegtaethedOmdrejningstal { get; set; }
    public long? MiljoeTungtNulEmissionKoeretoej { get; set; }
    public long? MotorCylinderAntal { get; set; }
    public double? MotorSlagVolumen { get; set; }
    public long? MotorSlagVolumenIkkeTilgaengelig { get; set; }
    public double? MotorStoersteEffekt { get; set; }
    public long? MotorStoersteEffektIkkeTilgaengelig { get; set; }
    public long? MotorKilometerstand { get; set; }
    public long? MotorKilometerstandDokumentation { get; set; }
    public long? MotorKilometerstandIkkeTilgaengelig { get; set; }
    public long? MotorInnovativTeknik { get; set; }

    /// <summary>Despite the "Antal" (count) name, this is a decimal — the CO2 savings (g/km) attributed
    /// to the innovative technology flagged by <see cref="MotorInnovativTeknik"/>, per EU vehicle
    /// type-approval "eco-innovation" reporting.</summary>
    public double? MotorInnovativTeknikAntal { get; set; }
    public double? MotorKoerselStoej { get; set; }
    public double? MotorStandStoej { get; set; }
    public long? MotorStandStoejOmdrejningstal { get; set; }
    public long? MotorBraendselscelle { get; set; }
    public string? MotorMaerkning { get; set; }
    public string? SynSynsType { get; set; }
    public string? SynSynsDato { get; set; }
    public string? SynSynsResultat { get; set; }
    public string? SynStatus { get; set; }
    public string? SynStatusDato { get; set; }
    public string? RegistreringStatus { get; set; }
    public string? RegistreringStatusDato { get; set; }

    /// <summary>Full usage list, from <c>KoeretoejAnvendelse</c> — only non-empty for multi-usage vehicles
    /// (the primary usage is already flattened onto <see cref="AnvendelseNummer"/>/<see cref="AnvendelseNavn"/>
    /// above).</summary>
    public List<DmrVehicleAnvendelseDto> Anvendelser { get; set; } = [];

    public List<DmrVehicleSupplerendeKarrosseriDto> SupplerendeKarrosserier { get; set; } = [];
    public List<DmrVehicleDrivmiddelDto> Drivmidler { get; set; } = [];
    public List<DmrVehicleUdstyrDto> Udstyr { get; set; } = [];
    public List<DmrVehicleBlokeringAarsagDto> BlokeringAarsager { get; set; } = [];
    public List<DmrVehicleTilladelseDto> Tilladelser { get; set; } = [];
}
