using System.Collections.Concurrent;
using System.Diagnostics;
using System.Xml;
using System.Xml.Serialization;
using DMR.Data;
using DMR.Services.Dmr.Dtos;
using DMR.Services.Dmr.Dtos.Xml;
using DMR.Services.Dmr.Interfaces;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace DMR.Services.Dmr;

/// <inheritdoc cref="IDmrService"/>
public class DmrService : IDmrService
{
    /// <summary>Name of the file ZipExtensions.Unpack always produces from the DMR export zip.</summary>
    private const string SourceXmlFileName = "ESStatistikListeModtag.xml";

    /// <summary>Format for the date stamp in the output database's file name — e.g. "dmr_2026-09-06.db",
    /// taken from the source XML file's own last-write time, not the date the conversion happens to run on
    /// (see <see cref="GetDatabasePath"/>).</summary>
    private const string DatabaseFileNameDateFormat = "yyyy-MM-dd";

    /// <summary>How many vehicles to write per SQLite transaction — batching transactions is what makes
    /// bulk inserts fast; see services/dmr/docs "Performance tuning".</summary>
    private const int BatchSize = 20_000;

    /// <summary>Bound on the parse→write handoff queue in <see cref="ImportToSqlite"/> — see "Performance
    /// tuning" in services/dmr/docs for why parsing and writing run on separate threads at all. Bounded so
    /// a temporarily slow writer applies backpressure to the parser instead of buffering the whole file's
    /// vehicles in memory.</summary>
    private const int ParseQueueCapacity = 2_000;

    private const int ProgressLogInterval = 100_000;

    private static readonly XmlSerializer StatistikSerializer = new(typeof(StatistikXml));

    // Column lists drive both the CREATE TABLE and INSERT statements, so the two can never drift apart.
    private static readonly (string Name, string SqlType)[] KoeretoejColumns =
    [
        ("KoeretoejIdent", "TEXT PRIMARY KEY"),
        ("KoeretoejArtNummer", "INTEGER"),
        ("KoeretoejArtNavn", "TEXT"),
        ("AnvendelseNummer", "INTEGER"),
        ("AnvendelseNavn", "TEXT"),
        ("LeasingGyldigFra", "TEXT"),
        ("LeasingGyldigTil", "TEXT"),
        // COLLATE NOCASE on the column itself (not just the query) matters: SQLite only uses an index to
        // accelerate a COLLATE NOCASE comparison if the index's collating sequence matches — otherwise it
        // falls back to a full table scan even with IX_Koeretoej_RegistreringNummer in place. An index
        // inherits its collation from the column it's built on, so declaring it here is what makes
        // ReadKoeretoej's "WHERE RegistreringNummer = @x COLLATE NOCASE" (services/dmr's primary search
        // parameter) actually hit IX_Koeretoej_RegistreringNummer.
        ("RegistreringNummer", "TEXT COLLATE NOCASE"),
        ("RegistreringNummerRettighedGyldigFra", "TEXT"),
        ("RegistreringNummerRettighedGyldigTil", "TEXT"),
        ("RegistreringNummerUdloebDato", "TEXT"),
        ("OprettetUdFra", "TEXT"),
        ("Status", "TEXT"),
        ("StatusDato", "TEXT"),
        ("FoersteRegistreringDato", "TEXT"),
        ("StelNummer", "TEXT"),
        ("StelNummerAnbringelse", "TEXT"),
        ("TotalVaegt", "INTEGER"),
        ("EgenVaegt", "INTEGER"),
        ("TekniskTotalVaegt", "INTEGER"),
        ("AkselAntal", "INTEGER"),
        ("AkselAfstand", "TEXT"),
        ("StoersteAkselTryk", "INTEGER"),
        ("TilkoblingMulighed", "INTEGER"),
        ("TilkoblingsvaegtUdenBremser", "INTEGER"),
        ("TilkoblingsvaegtMedBremser", "INTEGER"),
        ("TypeAnmeldelseNummer", "TEXT"),
        ("TypeGodkendelseNummer", "TEXT"),
        ("TypegodkendtKategori", "TEXT"),
        ("EUVariant", "TEXT"),
        ("EUVersion", "TEXT"),
        ("Kommentar", "TEXT"),
        ("AntalDoere", "INTEGER"),
        ("AntalGear", "INTEGER"),
        ("Trafikskade", "INTEGER"),
        ("NCAPTest", "INTEGER"),
        ("Koeretoejstand", "TEXT"),
        ("TraekkendeAksler", "TEXT"),
        ("EgnetTilTaxi", "INTEGER"),
        ("KoereklarVaegtMaksimum", "INTEGER"),
        ("KoereklarVaegtMinimum", "INTEGER"),
        ("MaksimumHastighed", "INTEGER"),
        ("ModelAar", "INTEGER"),
        ("OevrigtUdstyr", "TEXT"),
        ("PaahaengVognTotalVaegt", "INTEGER"),
        ("PassagerAntal", "INTEGER"),
        ("SaettevognTilladtAkselTryk", "INTEGER"),
        ("SiddepladserMaksimum", "INTEGER"),
        ("SiddepladserMinimum", "INTEGER"),
        ("SkammelBelastning", "INTEGER"),
        ("SkatteAkselAntal", "INTEGER"),
        ("SkatteAkselTryk", "INTEGER"),
        ("SporviddenBagest", "INTEGER"),
        ("SporviddenForrest", "INTEGER"),
        ("StaapladserMinimum", "INTEGER"),
        ("VVaerdiLuft", "REAL"),
        ("VVaerdiMekanisk", "REAL"),
        ("VeteranKoeretoejOriginal", "INTEGER"),
        ("VogntogVaegt", "INTEGER"),
        ("FaelgDaek", "TEXT"),
        ("Fabrikant", "TEXT"),
        ("Er30PctVarevogn", "INTEGER"),
        ("MaerkeNummer", "INTEGER"),
        ("MaerkeNavn", "TEXT"),
        ("ModelNummer", "INTEGER"),
        ("ModelNavn", "TEXT"),
        ("VariantNummer", "INTEGER"),
        ("VariantNavn", "TEXT"),
        ("TypeNummer", "INTEGER"),
        ("TypeNavn", "TEXT"),
        ("FarveNummer", "INTEGER"),
        ("FarveNavn", "TEXT"),
        ("KarrosseriNavn", "TEXT"),
        ("NormNummer", "INTEGER"),
        ("NormNavn", "TEXT"),
        ("MiljoeEmissionCO", "REAL"),
        ("MiljoeEmissionHCPlusNOX", "REAL"),
        ("MiljoeEmissionNOX", "REAL"),
        ("MiljoeNyttelastvaerdi", "REAL"),
        ("MiljoePartikelFilter", "INTEGER"),
        ("MiljoePartikler", "REAL"),
        ("MiljoeRoegtaethed", "REAL"),
        ("MiljoeRoegtaethedOmdrejningstal", "INTEGER"),
        ("MiljoeTungtNulEmissionKoeretoej", "INTEGER"),
        ("MotorCylinderAntal", "INTEGER"),
        ("MotorSlagVolumen", "REAL"),
        ("MotorSlagVolumenIkkeTilgaengelig", "INTEGER"),
        ("MotorStoersteEffekt", "REAL"),
        ("MotorStoersteEffektIkkeTilgaengelig", "INTEGER"),
        ("MotorKilometerstand", "INTEGER"),
        ("MotorKilometerstandDokumentation", "INTEGER"),
        ("MotorKilometerstandIkkeTilgaengelig", "INTEGER"),
        ("MotorInnovativTeknik", "INTEGER"),
        ("MotorInnovativTeknikAntal", "REAL"),
        ("MotorKoerselStoej", "REAL"),
        ("MotorStandStoej", "REAL"),
        ("MotorStandStoejOmdrejningstal", "INTEGER"),
        ("MotorBraendselscelle", "INTEGER"),
        ("MotorMaerkning", "TEXT"),
        ("SynSynsType", "TEXT"),
        ("SynSynsDato", "TEXT"),
        ("SynSynsResultat", "TEXT"),
        ("SynStatus", "TEXT"),
        ("SynStatusDato", "TEXT"),
        ("RegistreringStatus", "TEXT"),
        ("RegistreringStatusDato", "TEXT")
    ];

    private static readonly (string Name, string SqlType)[] AnvendelseColumns =
    [
        ("KoeretoejIdent", "TEXT"),
        ("AnvendelseNummer", "INTEGER"),
        ("AnvendelseNavn", "TEXT")
    ];

    private static readonly (string Name, string SqlType)[] SupplerendeKarrosseriColumns =
    [
        ("KoeretoejIdent", "TEXT"),
        ("TypeNummer", "INTEGER"),
        ("TypeNavn", "TEXT")
    ];

    private static readonly (string Name, string SqlType)[] DrivmiddelColumns =
    [
        ("KoeretoejIdent", "TEXT"),
        ("DrivkraftTypeNummer", "INTEGER"),
        ("DrivkraftTypeNavn", "TEXT"),
        ("KmPerLiter", "REAL"),
        ("CO2Udslip", "REAL"),
        ("ElektriskForbrug", "REAL"),
        ("MaaleNormNummer", "INTEGER"),
        ("MaaleNormNavn", "TEXT"),
        ("ErPrimaer", "INTEGER")
    ];

    private static readonly (string Name, string SqlType)[] UdstyrColumns =
    [
        ("KoeretoejIdent", "TEXT"),
        ("UdstyrTypeNummer", "INTEGER"),
        ("UdstyrTypeNavn", "TEXT"),
        ("Antal", "INTEGER"),
        ("VisesVedSyn", "INTEGER"),
        ("VisesVedForespoergsel", "INTEGER"),
        ("VisesVedStandardOprettelse", "INTEGER")
    ];

    private static readonly (string Name, string SqlType)[] BlokeringAarsagColumns =
    [
        ("KoeretoejIdent", "TEXT"),
        ("TypeNummer", "INTEGER"),
        ("TypeNavn", "TEXT")
    ];

    private static readonly (string Name, string SqlType)[] TilladelseColumns =
    [
        ("KoeretoejIdent", "TEXT"),
        ("GyldigFra", "TEXT"),
        ("Kommentar", "TEXT"),
        ("TypeNummer", "INTEGER"),
        ("TypeNavn", "TEXT"),
        ("DetaljeReferenceKoeretoejIdent", "TEXT")
    ];

    private readonly IHostEnvironment _environment;
    private readonly AppIdentityDbContext _dbContext;
    private readonly ILogger<DmrService> _logger;

    public DmrService(IHostEnvironment environment, AppIdentityDbContext dbContext, ILogger<DmrService> logger)
    {
        _environment = environment;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<DmrConvertResponseDto> ConvertAsync(DmrConvertRequestDto request, CancellationToken cancellationToken = default)
    {
        var sourcePath = GetSourceXmlPath();

        if (!File.Exists(sourcePath))
        {
            return new DmrConvertResponseDto
            {
                Success = false,
                ErrorMessage = $"Source XML file not found at '{sourcePath}'. Unpack the export first (see ZipExtensions.Unpack)."
            };
        }

        // Date-stamped by the source file's own last-write time — not today's date — so the database's
        // name reflects which data vintage it holds (see GetDatabasePath). Computed only now, rather than
        // before the existence check above, because it needs to stat a file that's confirmed to exist.
        var databasePath = GetDatabasePath(sourcePath);

        var stopwatch = Stopwatch.StartNew();
        try
        {
            // The parse/insert loop below is synchronous (XmlSerializer.Deserialize and Microsoft.Data.Sqlite
            // are both sync APIs) and can run for a long time on the full export — offload it so it never
            // blocks the calling thread. Callers must still not await this from an HTTP request; see
            // IDmrService.ConvertAsync and services/ftp/docs for why.
            var (recordsImported, recordsSkipped) = await Task.Run(
                () => ImportToSqlite(sourcePath, databasePath, request.MaxRecords, cancellationToken),
                cancellationToken);

            stopwatch.Stop();
            _logger.LogInformation(
                "DMR conversion finished: {RecordsImported} vehicles imported ({RecordsSkipped} skipped) into {DatabasePath} in {Duration}",
                recordsImported, recordsSkipped, databasePath, stopwatch.Elapsed);

            return new DmrConvertResponseDto
            {
                Success = true,
                RecordsImported = recordsImported,
                RecordsSkipped = recordsSkipped,
                DatabasePath = databasePath,
                Duration = stopwatch.Elapsed
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "DMR conversion of {SourcePath} failed after {Duration}", sourcePath, stopwatch.Elapsed);
            return new DmrConvertResponseDto
            {
                Success = false,
                ErrorMessage = ex.Message,
                DatabasePath = databasePath,
                Duration = stopwatch.Elapsed
            };
        }
    }

    public async Task<DmrLookupVehicleResponseDto> LookupVehicleAsync(DmrLookupVehicleRequestDto request, CancellationToken cancellationToken = default)
    {
        var registreringNummer = request.RegistreringNummer?.Trim() ?? string.Empty;
        if (registreringNummer.Length == 0)
            return new DmrLookupVehicleResponseDto { Success = false, ErrorMessage = "RegistreringNummer is required." };

        var dataset = await _dbContext.DmrDatasets
            .OrderByDescending(d => d.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (dataset is null)
            return new DmrLookupVehicleResponseDto { Success = false, ErrorMessage = "No DMR dataset has been ingested yet." };

        if (!File.Exists(dataset.DatabasePath))
        {
            _logger.LogError("DmrService: current dataset {DatasetId}'s database file {DatabasePath} is missing.", dataset.Id, dataset.DatabasePath);
            return new DmrLookupVehicleResponseDto { Success = false, ErrorMessage = "The current dataset's database file is missing." };
        }

        try
        {
            // Mode=ReadOnly: this is a lookup, never a write. Pooling=False for the same reason as
            // ImportToSqlite's write connection above — a pooled connection would keep the native SQLite
            // handle alive past Dispose(), which could make DmrWorker's retention cleanup File.Delete this
            // very database once it falls out of the retained set fail with the file still locked.
            using var connection = new SqliteConnection($"Data Source={dataset.DatabasePath};Mode=ReadOnly;Pooling=False");
            connection.Open();

            var vehicle = ReadKoeretoej(connection, registreringNummer);
            if (vehicle is null)
            {
                return new DmrLookupVehicleResponseDto
                {
                    Success = false,
                    ErrorMessage = $"No vehicle found with registration number '{registreringNummer}'.",
                    DatasetSourceFileName = dataset.SourceFileName,
                    DatasetCreatedAtUtc = dataset.CreatedAtUtc
                };
            }

            vehicle.Anvendelser = ReadAnvendelseList(connection, vehicle.KoeretoejIdent);
            vehicle.SupplerendeKarrosserier = ReadSupplerendeKarrosseriList(connection, vehicle.KoeretoejIdent);
            vehicle.Drivmidler = ReadDrivmiddelList(connection, vehicle.KoeretoejIdent);
            vehicle.Udstyr = ReadUdstyrList(connection, vehicle.KoeretoejIdent);
            vehicle.BlokeringAarsager = ReadBlokeringAarsagList(connection, vehicle.KoeretoejIdent);
            vehicle.Tilladelser = ReadTilladelseList(connection, vehicle.KoeretoejIdent);

            return new DmrLookupVehicleResponseDto
            {
                Success = true,
                Vehicle = vehicle,
                DatasetSourceFileName = dataset.SourceFileName,
                DatasetCreatedAtUtc = dataset.CreatedAtUtc
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DmrService: lookup of registration number {RegistreringNummer} failed.", registreringNummer);
            return new DmrLookupVehicleResponseDto { Success = false, ErrorMessage = ex.Message };
        }
    }

    private static DmrVehicleDto? ReadKoeretoej(SqliteConnection connection, string registreringNummer)
    {
        using var command = connection.CreateCommand();
        // COLLATE NOCASE: the export's casing for RegistreringNummer isn't guaranteed, and callers
        // shouldn't have to match it exactly (see DmrLookupVehicleRequestDto). Kept explicit here even
        // though RegistreringNummer's column definition also declares COLLATE NOCASE (see KoeretoejColumns)
        // — verified with EXPLAIN QUERY PLAN that a query-side COLLATE NOCASE that *agrees* with the
        // column's own collation still hits IX_Koeretoej_RegistreringNummer, so this costs nothing on a
        // database built by the current schema. What it buys: correctness against an *older* database built
        // before RegistreringNummer's column got COLLATE NOCASE (e.g. an app_dbs/dmr_<date>.db imported by
        // an earlier build of this service, still in place until its next re-import) — there, dropping this
        // would silently turn lookups case-sensitive. It does mean such an older database still falls back
        // to a full table scan (SCAN Koeretoej, confirmed the same way) until it's rebuilt — no query-side
        // change can fix that without a schema rebuild; see services/dmr/docs.
        command.CommandText =
            $"SELECT {string.Join(", ", KoeretoejColumns.Select(c => c.Name))} FROM Koeretoej " +
            "WHERE RegistreringNummer = @RegistreringNummer COLLATE NOCASE LIMIT 1;";
        command.Parameters.AddWithValue("@RegistreringNummer", registreringNummer);

        using var reader = command.ExecuteReader();
        if (!reader.Read())
            return null;

        return new DmrVehicleDto
        {
            KoeretoejIdent = GetNullableString(reader, "KoeretoejIdent") ?? string.Empty,
            KoeretoejArtNummer = GetNullableInt64(reader, "KoeretoejArtNummer"),
            KoeretoejArtNavn = GetNullableString(reader, "KoeretoejArtNavn"),
            AnvendelseNummer = GetNullableInt64(reader, "AnvendelseNummer"),
            AnvendelseNavn = GetNullableString(reader, "AnvendelseNavn"),
            LeasingGyldigFra = GetNullableString(reader, "LeasingGyldigFra"),
            LeasingGyldigTil = GetNullableString(reader, "LeasingGyldigTil"),
            RegistreringNummer = GetNullableString(reader, "RegistreringNummer"),
            RegistreringNummerRettighedGyldigFra = GetNullableString(reader, "RegistreringNummerRettighedGyldigFra"),
            RegistreringNummerRettighedGyldigTil = GetNullableString(reader, "RegistreringNummerRettighedGyldigTil"),
            RegistreringNummerUdloebDato = GetNullableString(reader, "RegistreringNummerUdloebDato"),
            OprettetUdFra = GetNullableString(reader, "OprettetUdFra"),
            Status = GetNullableString(reader, "Status"),
            StatusDato = GetNullableString(reader, "StatusDato"),
            FoersteRegistreringDato = GetNullableString(reader, "FoersteRegistreringDato"),
            StelNummer = GetNullableString(reader, "StelNummer"),
            StelNummerAnbringelse = GetNullableString(reader, "StelNummerAnbringelse"),
            TotalVaegt = GetNullableInt64(reader, "TotalVaegt"),
            EgenVaegt = GetNullableInt64(reader, "EgenVaegt"),
            TekniskTotalVaegt = GetNullableInt64(reader, "TekniskTotalVaegt"),
            AkselAntal = GetNullableInt64(reader, "AkselAntal"),
            AkselAfstand = GetNullableString(reader, "AkselAfstand"),
            StoersteAkselTryk = GetNullableInt64(reader, "StoersteAkselTryk"),
            TilkoblingMulighed = GetNullableInt64(reader, "TilkoblingMulighed"),
            TilkoblingsvaegtUdenBremser = GetNullableInt64(reader, "TilkoblingsvaegtUdenBremser"),
            TilkoblingsvaegtMedBremser = GetNullableInt64(reader, "TilkoblingsvaegtMedBremser"),
            TypeAnmeldelseNummer = GetNullableString(reader, "TypeAnmeldelseNummer"),
            TypeGodkendelseNummer = GetNullableString(reader, "TypeGodkendelseNummer"),
            TypegodkendtKategori = GetNullableString(reader, "TypegodkendtKategori"),
            EUVariant = GetNullableString(reader, "EUVariant"),
            EUVersion = GetNullableString(reader, "EUVersion"),
            Kommentar = GetNullableString(reader, "Kommentar"),
            AntalDoere = GetNullableInt64(reader, "AntalDoere"),
            AntalGear = GetNullableInt64(reader, "AntalGear"),
            Trafikskade = GetNullableInt64(reader, "Trafikskade"),
            NCAPTest = GetNullableInt64(reader, "NCAPTest"),
            Koeretoejstand = GetNullableString(reader, "Koeretoejstand"),
            TraekkendeAksler = GetNullableString(reader, "TraekkendeAksler"),
            EgnetTilTaxi = GetNullableInt64(reader, "EgnetTilTaxi"),
            KoereklarVaegtMaksimum = GetNullableInt64(reader, "KoereklarVaegtMaksimum"),
            KoereklarVaegtMinimum = GetNullableInt64(reader, "KoereklarVaegtMinimum"),
            MaksimumHastighed = GetNullableInt64(reader, "MaksimumHastighed"),
            ModelAar = GetNullableInt64(reader, "ModelAar"),
            OevrigtUdstyr = GetNullableString(reader, "OevrigtUdstyr"),
            PaahaengVognTotalVaegt = GetNullableInt64(reader, "PaahaengVognTotalVaegt"),
            PassagerAntal = GetNullableInt64(reader, "PassagerAntal"),
            SaettevognTilladtAkselTryk = GetNullableInt64(reader, "SaettevognTilladtAkselTryk"),
            SiddepladserMaksimum = GetNullableInt64(reader, "SiddepladserMaksimum"),
            SiddepladserMinimum = GetNullableInt64(reader, "SiddepladserMinimum"),
            SkammelBelastning = GetNullableInt64(reader, "SkammelBelastning"),
            SkatteAkselAntal = GetNullableInt64(reader, "SkatteAkselAntal"),
            SkatteAkselTryk = GetNullableInt64(reader, "SkatteAkselTryk"),
            SporviddenBagest = GetNullableInt64(reader, "SporviddenBagest"),
            SporviddenForrest = GetNullableInt64(reader, "SporviddenForrest"),
            StaapladserMinimum = GetNullableInt64(reader, "StaapladserMinimum"),
            VVaerdiLuft = GetNullableDouble(reader, "VVaerdiLuft"),
            VVaerdiMekanisk = GetNullableDouble(reader, "VVaerdiMekanisk"),
            VeteranKoeretoejOriginal = GetNullableInt64(reader, "VeteranKoeretoejOriginal"),
            VogntogVaegt = GetNullableInt64(reader, "VogntogVaegt"),
            FaelgDaek = GetNullableString(reader, "FaelgDaek"),
            Fabrikant = GetNullableString(reader, "Fabrikant"),
            Er30PctVarevogn = GetNullableInt64(reader, "Er30PctVarevogn"),
            MaerkeNummer = GetNullableInt64(reader, "MaerkeNummer"),
            MaerkeNavn = GetNullableString(reader, "MaerkeNavn"),
            ModelNummer = GetNullableInt64(reader, "ModelNummer"),
            ModelNavn = GetNullableString(reader, "ModelNavn"),
            VariantNummer = GetNullableInt64(reader, "VariantNummer"),
            VariantNavn = GetNullableString(reader, "VariantNavn"),
            TypeNummer = GetNullableInt64(reader, "TypeNummer"),
            TypeNavn = GetNullableString(reader, "TypeNavn"),
            FarveNummer = GetNullableInt64(reader, "FarveNummer"),
            FarveNavn = GetNullableString(reader, "FarveNavn"),
            KarrosseriNavn = GetNullableString(reader, "KarrosseriNavn"),
            NormNummer = GetNullableInt64(reader, "NormNummer"),
            NormNavn = GetNullableString(reader, "NormNavn"),
            MiljoeEmissionCO = GetNullableDouble(reader, "MiljoeEmissionCO"),
            MiljoeEmissionHCPlusNOX = GetNullableDouble(reader, "MiljoeEmissionHCPlusNOX"),
            MiljoeEmissionNOX = GetNullableDouble(reader, "MiljoeEmissionNOX"),
            MiljoeNyttelastvaerdi = GetNullableDouble(reader, "MiljoeNyttelastvaerdi"),
            MiljoePartikelFilter = GetNullableInt64(reader, "MiljoePartikelFilter"),
            MiljoePartikler = GetNullableDouble(reader, "MiljoePartikler"),
            MiljoeRoegtaethed = GetNullableDouble(reader, "MiljoeRoegtaethed"),
            MiljoeRoegtaethedOmdrejningstal = GetNullableInt64(reader, "MiljoeRoegtaethedOmdrejningstal"),
            MiljoeTungtNulEmissionKoeretoej = GetNullableInt64(reader, "MiljoeTungtNulEmissionKoeretoej"),
            MotorCylinderAntal = GetNullableInt64(reader, "MotorCylinderAntal"),
            MotorSlagVolumen = GetNullableDouble(reader, "MotorSlagVolumen"),
            MotorSlagVolumenIkkeTilgaengelig = GetNullableInt64(reader, "MotorSlagVolumenIkkeTilgaengelig"),
            MotorStoersteEffekt = GetNullableDouble(reader, "MotorStoersteEffekt"),
            MotorStoersteEffektIkkeTilgaengelig = GetNullableInt64(reader, "MotorStoersteEffektIkkeTilgaengelig"),
            MotorKilometerstand = GetNullableInt64(reader, "MotorKilometerstand"),
            MotorKilometerstandDokumentation = GetNullableInt64(reader, "MotorKilometerstandDokumentation"),
            MotorKilometerstandIkkeTilgaengelig = GetNullableInt64(reader, "MotorKilometerstandIkkeTilgaengelig"),
            MotorInnovativTeknik = GetNullableInt64(reader, "MotorInnovativTeknik"),
            MotorInnovativTeknikAntal = GetNullableDouble(reader, "MotorInnovativTeknikAntal"),
            MotorKoerselStoej = GetNullableDouble(reader, "MotorKoerselStoej"),
            MotorStandStoej = GetNullableDouble(reader, "MotorStandStoej"),
            MotorStandStoejOmdrejningstal = GetNullableInt64(reader, "MotorStandStoejOmdrejningstal"),
            MotorBraendselscelle = GetNullableInt64(reader, "MotorBraendselscelle"),
            MotorMaerkning = GetNullableString(reader, "MotorMaerkning"),
            SynSynsType = GetNullableString(reader, "SynSynsType"),
            SynSynsDato = GetNullableString(reader, "SynSynsDato"),
            SynSynsResultat = GetNullableString(reader, "SynSynsResultat"),
            SynStatus = GetNullableString(reader, "SynStatus"),
            SynStatusDato = GetNullableString(reader, "SynStatusDato"),
            RegistreringStatus = GetNullableString(reader, "RegistreringStatus"),
            RegistreringStatusDato = GetNullableString(reader, "RegistreringStatusDato")
        };
    }

    private static List<DmrVehicleAnvendelseDto> ReadAnvendelseList(SqliteConnection connection, string koeretoejIdent) =>
        ReadChildRows(connection, "KoeretoejAnvendelse", AnvendelseColumns, koeretoejIdent, reader => new DmrVehicleAnvendelseDto
        {
            AnvendelseNummer = GetNullableInt64(reader, "AnvendelseNummer"),
            AnvendelseNavn = GetNullableString(reader, "AnvendelseNavn")
        });

    private static List<DmrVehicleSupplerendeKarrosseriDto> ReadSupplerendeKarrosseriList(SqliteConnection connection, string koeretoejIdent) =>
        ReadChildRows(connection, "KoeretoejSupplerendeKarrosseri", SupplerendeKarrosseriColumns, koeretoejIdent, reader => new DmrVehicleSupplerendeKarrosseriDto
        {
            TypeNummer = GetNullableInt64(reader, "TypeNummer"),
            TypeNavn = GetNullableString(reader, "TypeNavn")
        });

    private static List<DmrVehicleDrivmiddelDto> ReadDrivmiddelList(SqliteConnection connection, string koeretoejIdent) =>
        ReadChildRows(connection, "KoeretoejDrivmiddel", DrivmiddelColumns, koeretoejIdent, reader => new DmrVehicleDrivmiddelDto
        {
            DrivkraftTypeNummer = GetNullableInt64(reader, "DrivkraftTypeNummer"),
            DrivkraftTypeNavn = GetNullableString(reader, "DrivkraftTypeNavn"),
            KmPerLiter = GetNullableDouble(reader, "KmPerLiter"),
            CO2Udslip = GetNullableDouble(reader, "CO2Udslip"),
            ElektriskForbrug = GetNullableDouble(reader, "ElektriskForbrug"),
            MaaleNormNummer = GetNullableInt64(reader, "MaaleNormNummer"),
            MaaleNormNavn = GetNullableString(reader, "MaaleNormNavn"),
            ErPrimaer = GetNullableInt64(reader, "ErPrimaer")
        });

    private static List<DmrVehicleUdstyrDto> ReadUdstyrList(SqliteConnection connection, string koeretoejIdent) =>
        ReadChildRows(connection, "KoeretoejUdstyr", UdstyrColumns, koeretoejIdent, reader => new DmrVehicleUdstyrDto
        {
            UdstyrTypeNummer = GetNullableInt64(reader, "UdstyrTypeNummer"),
            UdstyrTypeNavn = GetNullableString(reader, "UdstyrTypeNavn"),
            Antal = GetNullableInt64(reader, "Antal"),
            VisesVedSyn = GetNullableInt64(reader, "VisesVedSyn"),
            VisesVedForespoergsel = GetNullableInt64(reader, "VisesVedForespoergsel"),
            VisesVedStandardOprettelse = GetNullableInt64(reader, "VisesVedStandardOprettelse")
        });

    private static List<DmrVehicleBlokeringAarsagDto> ReadBlokeringAarsagList(SqliteConnection connection, string koeretoejIdent) =>
        ReadChildRows(connection, "KoeretoejBlokeringAarsag", BlokeringAarsagColumns, koeretoejIdent, reader => new DmrVehicleBlokeringAarsagDto
        {
            TypeNummer = GetNullableInt64(reader, "TypeNummer"),
            TypeNavn = GetNullableString(reader, "TypeNavn")
        });

    private static List<DmrVehicleTilladelseDto> ReadTilladelseList(SqliteConnection connection, string koeretoejIdent) =>
        ReadChildRows(connection, "KoeretoejTilladelse", TilladelseColumns, koeretoejIdent, reader => new DmrVehicleTilladelseDto
        {
            GyldigFra = GetNullableString(reader, "GyldigFra"),
            Kommentar = GetNullableString(reader, "Kommentar"),
            TypeNummer = GetNullableInt64(reader, "TypeNummer"),
            TypeNavn = GetNullableString(reader, "TypeNavn"),
            DetaljeReferenceKoeretoejIdent = GetNullableString(reader, "DetaljeReferenceKoeretoejIdent")
        });

    /// <summary>Shared plumbing for every "child rows of one vehicle" query above: selects
    /// <paramref name="columns"/> from <paramref name="table"/> where <c>KoeretoejIdent</c> matches, and
    /// maps each row via <paramref name="map"/>.</summary>
    private static List<T> ReadChildRows<T>(SqliteConnection connection, string table, (string Name, string SqlType)[] columns, string koeretoejIdent, Func<SqliteDataReader, T> map)
    {
        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT {string.Join(", ", columns.Select(c => c.Name))} FROM {table} WHERE KoeretoejIdent = @KoeretoejIdent;";
        command.Parameters.AddWithValue("@KoeretoejIdent", koeretoejIdent);

        using var reader = command.ExecuteReader();
        var results = new List<T>();
        while (reader.Read())
            results.Add(map(reader));
        return results;
    }

    private static string? GetNullableString(SqliteDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

    private static long? GetNullableInt64(SqliteDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetInt64(ordinal);
    }

    private static double? GetNullableDouble(SqliteDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetDouble(ordinal);
    }

    /// <summary>Fixed source file — always where ZipExtensions.Unpack lands the export, under
    /// ".\app_files\temp". Not configurable per request; see services/dmr/docs.</summary>
    private string GetSourceXmlPath() =>
        Path.Combine(_environment.ContentRootPath, "app_files", "temp", SourceXmlFileName);

    /// <summary>Fixed destination — always ".\app_dbs\dmr_&lt;yyyy-MM-dd&gt;.db", date-stamped with
    /// <paramref name="sourceXmlPath"/>'s own last-write time (i.e. when Motorstyrelsen actually produced
    /// that data — preserved from the zip entry's timestamp through <c>ZipExtensions.Unpack</c>), not the
    /// date the conversion happens to run on. That keeps the filename describing which data vintage it
    /// holds even if a conversion runs late, is re-run, or the source file's date happens to fall on a
    /// different day than "now" (see CLAUDE.md: app_dbs/ is reserved for local database files). Not
    /// configurable per request; see services/dmr/docs.</summary>
    private string GetDatabasePath(string sourceXmlPath)
    {
        var sourceDate = File.GetLastWriteTime(sourceXmlPath);
        return Path.Combine(_environment.ContentRootPath, "app_dbs", $"dmr_{sourceDate.ToString(DatabaseFileNameDateFormat)}.db");
    }

    private (int RecordsImported, int RecordsSkipped) ImportToSqlite(string sourcePath, string databasePath, int? maxRecords, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);

        // Every run rebuilds that day's database from scratch so a same-day re-import can never mix with
        // stale data from an earlier run — a run on a different day gets its own dmr_<date>.db and leaves
        // prior days' databases untouched.
        File.Delete(databasePath);
        File.Delete(databasePath + "-wal");
        File.Delete(databasePath + "-shm");

        // Pooling=False: this connection is opened once for the whole (long) import and then closed —
        // pooling exists to speed up many short-lived connections, not this. Without it, Microsoft.Data.Sqlite
        // would keep the native handle alive in the pool past Dispose(), and a re-run's File.Delete(databasePath)
        // above would fail with the file still locked (see dmr.tests/DmrServiceTests.cs for the same issue).
        using var connection = new SqliteConnection($"Data Source={databasePath};Pooling=False");
        connection.Open();

        using (var pragma = connection.CreateCommand())
        {
            // WAL + synchronous=NORMAL is the standard SQLite bulk-load tuning: it skips most of the
            // durability fsyncs the default (rollback-journal, synchronous=FULL) mode pays for on every
            // transaction, which matters a lot across the many batched transactions a multi-GB import does.
            // cache_size/mmap_size are raised well past SQLite's small defaults (2 MB page cache by default)
            // so far more of the working set — index pages especially — stays in memory across the whole
            // import instead of being evicted and re-read from disk; temp_store=MEMORY keeps the temp
            // b-trees CreateLookupIndexes builds afterward off disk too. See services/dmr/docs "Performance
            // tuning" for the reasoning and the numbers behind each of these.
            pragma.CommandText =
                "PRAGMA journal_mode=WAL; " +
                "PRAGMA synchronous=NORMAL; " +
                "PRAGMA cache_size=-200000; " +
                "PRAGMA temp_store=MEMORY; " +
                "PRAGMA mmap_size=268435456;";
            pragma.ExecuteNonQuery();
        }

        // Only the tables plus the child-table KoeretoejIdent indexes are created up front — those indexes
        // are load-bearing during the import itself (VehicleBatchWriter.Add's duplicate-KoeretoejIdent path
        // deletes by KoeretoejIdent on every child table; without an index that's a full table scan, and
        // late in a multi-million-row import that's exactly the kind of blow-up this whole change is meant
        // to avoid — see services/dmr/docs "Known gaps"). Koeretoej's own two lookup indexes
        // (RegistreringNummer, StelNummer) are never touched during the import — nothing here reads by
        // either column — so they're deferred to CreateLookupIndexes, built once after every row is in,
        // which is far cheaper than incrementally maintaining their B-trees on every single insert.
        CreateTables(connection);

        var recordCount = 0;
        var writeSkippedCount = 0;
        var parseSkippedCount = 0;
        var seenCount = 0;

        using (var writer = new VehicleBatchWriter(connection, BatchSize))
        {
            // Parsing (XmlSerializer — reflection-based, CPU-bound) and writing (SQLite — a different mix
            // of CPU and disk I/O) are handed to two separate threads via this bounded queue instead of
            // strictly alternating on one thread, so they overlap instead of each waiting on the other —
            // see services/dmr/docs "Performance tuning" for why this is the single biggest lever here.
            // Only the writer thread ever touches `connection`/`writer` — SqliteConnection isn't safe for
            // concurrent use from multiple threads, so the parser thread never touches either.
            using var queue = new BlockingCollection<(int SeenNumber, StatistikXml Vehicle)>(ParseQueueCapacity);

            var writerTask = Task.Run(() =>
            {
                // recordCount/writeSkippedCount are only ever mutated here, on this one thread — plain
                // increments are safe because the only read of them (below, after writerTask is joined via
                // GetAwaiter().GetResult()) happens-after every write here, per Task's completion guarantees.
                foreach (var (seenNumber, vehicle) in queue.GetConsumingEnumerable(cancellationToken))
                {
                    try
                    {
                        writer.Add(vehicle);
                        recordCount++;
                        if (recordCount % ProgressLogInterval == 0)
                            _logger.LogInformation("DMR conversion progress: {RecordCount} vehicles imported so far ({SkippedCount} skipped)",
                                recordCount, writeSkippedCount + parseSkippedCount);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        // Distinct from the parse-side catch below: this is a write that failed after the
                        // record parsed fine (e.g. a constraint violation VehicleBatchWriter.Add's own
                        // duplicate-KoeretoejIdent handling didn't already recover from). Same "skip and
                        // keep going" policy either way — see the parse-side catch for the full reasoning.
                        writeSkippedCount++;
                        _logger.LogWarning(ex, "Skipped a <Statistik> record that failed to write (record #{RecordNumber}).", seenNumber);
                    }
                }
            }, cancellationToken);

            using var stream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read,
                bufferSize: 1 << 20, FileOptions.SequentialScan);
            using var reader = XmlReader.Create(stream, new XmlReaderSettings { IgnoreWhitespace = true });

            var queuedCount = 0;
            try
            {
                while (reader.ReadToFollowing("Statistik", DmrXmlNamespace.Value))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    seenCount++;

                    try
                    {
                        using var subtree = reader.ReadSubtree();
                        subtree.Read(); // position on the <Statistik> start element for the serializer
                        var vehicle = (StatistikXml)StatistikSerializer.Deserialize(subtree)!;

                        if (string.IsNullOrEmpty(vehicle.KoeretoejIdent))
                        {
                            parseSkippedCount++;
                            _logger.LogWarning("Skipped a <Statistik> record with no KoeretoejIdent (record #{RecordNumber}).", seenCount);
                        }
                        else
                        {
                            // Blocks here (backpressure) once the queue is full instead of growing it
                            // unboundedly — see ParseQueueCapacity.
                            queue.Add((seenCount, vehicle), cancellationToken);
                            queuedCount++;
                        }
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        // The export is ~120 GB and only ever spot-checked in samples (see services/dmr/docs) — an
                        // unanticipated field shape elsewhere in the file (as happened with KoeretoejOplysningTraekkendeAksler,
                        // which turned out to hold a comma-separated axle list rather than a plain count) must not
                        // abort a run that may already be an hour deep. Skip just this one vehicle and keep going;
                        // disposing the subtree reader above still advances the outer reader past it correctly even
                        // though deserialization failed partway through.
                        parseSkippedCount++;
                        _logger.LogWarning(ex, "Skipped an unparseable <Statistik> record (record #{RecordNumber}).", seenCount);
                    }

                    // Caps how many vehicles are handed to the writer, not how many end up persisted (a
                    // queued vehicle could still fail to write) — close enough to the old "stop scanning
                    // the file" behavior for MaxRecords's actual purpose, a quick smoke test on a subset.
                    if (maxRecords.HasValue && queuedCount >= maxRecords.Value)
                        break;
                }
            }
            finally
            {
                // Always signal completion, including on a thrown/cancelled exit above, so the writer
                // thread's GetConsumingEnumerable loop ends instead of blocking forever waiting for more.
                queue.CompleteAdding();
            }

            // Propagates a fault from the writer thread (anything outside its own per-record catch above —
            // should not normally happen) and otherwise just waits for every queued vehicle to be written.
            writerTask.GetAwaiter().GetResult();
            writer.Flush();
        }

        CreateLookupIndexes(connection);

        var skippedCount = parseSkippedCount + writeSkippedCount;
        if (skippedCount > 0)
            _logger.LogWarning("DMR conversion finished with {SkippedCount} unparseable/incomplete records skipped out of {SeenCount} seen.", skippedCount, seenCount);

        return (recordCount, skippedCount);
    }

    private static void CreateTables(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = string.Join('\n',
            BuildCreateTableSql("Koeretoej", KoeretoejColumns),
            BuildCreateTableSql("KoeretoejAnvendelse", AnvendelseColumns),
            "CREATE INDEX IX_KoeretoejAnvendelse_KoeretoejIdent ON KoeretoejAnvendelse(KoeretoejIdent);",
            BuildCreateTableSql("KoeretoejSupplerendeKarrosseri", SupplerendeKarrosseriColumns),
            "CREATE INDEX IX_KoeretoejSupplerendeKarrosseri_KoeretoejIdent ON KoeretoejSupplerendeKarrosseri(KoeretoejIdent);",
            BuildCreateTableSql("KoeretoejDrivmiddel", DrivmiddelColumns),
            "CREATE INDEX IX_KoeretoejDrivmiddel_KoeretoejIdent ON KoeretoejDrivmiddel(KoeretoejIdent);",
            BuildCreateTableSql("KoeretoejUdstyr", UdstyrColumns),
            "CREATE INDEX IX_KoeretoejUdstyr_KoeretoejIdent ON KoeretoejUdstyr(KoeretoejIdent);",
            BuildCreateTableSql("KoeretoejBlokeringAarsag", BlokeringAarsagColumns),
            "CREATE INDEX IX_KoeretoejBlokeringAarsag_KoeretoejIdent ON KoeretoejBlokeringAarsag(KoeretoejIdent);",
            BuildCreateTableSql("KoeretoejTilladelse", TilladelseColumns),
            "CREATE INDEX IX_KoeretoejTilladelse_KoeretoejIdent ON KoeretoejTilladelse(KoeretoejIdent);");
        command.ExecuteNonQuery();
    }

    /// <summary>Builds <c>Koeretoej</c>'s two lookup indexes (for <see cref="LookupVehicleAsync"/>) after
    /// every row is already in, rather than maintaining their B-trees incrementally across ~20 million
    /// individual inserts — see the comment where this is called from <see cref="ImportToSqlite"/>.</summary>
    private static void CreateLookupIndexes(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            "CREATE INDEX IX_Koeretoej_RegistreringNummer ON Koeretoej(RegistreringNummer);\n" +
            "CREATE INDEX IX_Koeretoej_StelNummer ON Koeretoej(StelNummer);";
        command.ExecuteNonQuery();
    }

    private static string BuildCreateTableSql(string table, (string Name, string SqlType)[] columns) =>
        $"CREATE TABLE {table} ({string.Join(", ", columns.Select(c => $"{c.Name} {c.SqlType}"))});";

    private static string BuildInsertSql(string table, (string Name, string SqlType)[] columns) =>
        $"INSERT INTO {table} ({string.Join(", ", columns.Select(c => c.Name))}) " +
        $"VALUES ({string.Join(", ", columns.Select(c => "@" + c.Name))});";

    private static SqliteCommand BuildInsertCommand(SqliteConnection connection, string table, (string Name, string SqlType)[] columns)
    {
        var command = connection.CreateCommand();
        command.CommandText = BuildInsertSql(table, columns);
        foreach (var column in columns)
            command.Parameters.Add(new SqliteParameter("@" + column.Name, DBNull.Value));
        return command;
    }

    /// <summary>A "delete every row for this vehicle" command for one table — used only by
    /// <see cref="VehicleBatchWriter"/>'s duplicate-<c>KoeretoejIdent</c> handling (see
    /// <see cref="VehicleBatchWriter.Add"/>), never on the normal per-vehicle path.</summary>
    private static SqliteCommand BuildDeleteCommand(SqliteConnection connection, string table)
    {
        var command = connection.CreateCommand();
        command.CommandText = $"DELETE FROM {table} WHERE KoeretoejIdent = @KoeretoejIdent;";
        command.Parameters.Add(new SqliteParameter("@KoeretoejIdent", DBNull.Value));
        return command;
    }

    private static void SetParam(SqliteCommand command, string name, object? value) =>
        command.Parameters[name]!.Value = value ?? (object)DBNull.Value;

    /// <summary>
    /// Owns one prepared, reusable <see cref="SqliteCommand"/> per table and batches vehicles into
    /// transactions of <see cref="BatchSize"/> records — both are what make writing millions of rows fast:
    /// preparing each INSERT once instead of per row, and committing many rows per transaction instead of
    /// one. Kept as a private nested class rather than its own file to keep DmrService.cs the service's one
    /// implementation file, per the project's service structure conventions.
    /// </summary>
    private sealed class VehicleBatchWriter : IDisposable
    {
        /// <summary>SQLite primary result code for a constraint violation (<c>SQLITE_CONSTRAINT</c>).
        /// <c>KoeretoejIdent</c> is the only constrained column anywhere in this schema (it's the
        /// <c>Koeretoej</c> table's primary key — see <see cref="KoeretoejColumns"/>), so seeing this code
        /// while inserting into <c>Koeretoej</c> can only mean one thing: the export listed the same
        /// vehicle more than once. Seen for real on a full production run — a dense burst of thousands of
        /// repeated <c>KoeretoejIdent</c> values, several million records into a ~120 GB export (see
        /// services/dmr/docs "Known gaps").</summary>
        private const int SqliteConstraintViolationErrorCode = 19;

        private readonly SqliteConnection _connection;
        private readonly int _batchSize;
        private readonly SqliteCommand _insertKoeretoej;
        private readonly SqliteCommand _insertAnvendelse;
        private readonly SqliteCommand _insertSupplerendeKarrosseri;
        private readonly SqliteCommand _insertDrivmiddel;
        private readonly SqliteCommand _insertUdstyr;
        private readonly SqliteCommand _insertBlokeringAarsag;
        private readonly SqliteCommand _insertTilladelse;

        // One DELETE per table, all keyed on KoeretoejIdent — only ever used by the duplicate-KoeretoejIdent
        // path in Add() below, to fully wipe a vehicle (main row + every child row) before re-inserting it
        // fresh so the newer occurrence in the file wins outright, never mixing with the stale child rows
        // of the earlier occurrence.
        private readonly SqliteCommand _deleteKoeretoej;
        private readonly SqliteCommand _deleteAnvendelse;
        private readonly SqliteCommand _deleteSupplerendeKarrosseri;
        private readonly SqliteCommand _deleteDrivmiddel;
        private readonly SqliteCommand _deleteUdstyr;
        private readonly SqliteCommand _deleteBlokeringAarsag;
        private readonly SqliteCommand _deleteTilladelse;

        private SqliteTransaction? _transaction;
        private int _pending;

        public VehicleBatchWriter(SqliteConnection connection, int batchSize)
        {
            _connection = connection;
            _batchSize = batchSize;

            _insertKoeretoej = BuildInsertCommand(connection, "Koeretoej", KoeretoejColumns);
            _insertAnvendelse = BuildInsertCommand(connection, "KoeretoejAnvendelse", AnvendelseColumns);
            _insertSupplerendeKarrosseri = BuildInsertCommand(connection, "KoeretoejSupplerendeKarrosseri", SupplerendeKarrosseriColumns);
            _insertDrivmiddel = BuildInsertCommand(connection, "KoeretoejDrivmiddel", DrivmiddelColumns);
            _insertUdstyr = BuildInsertCommand(connection, "KoeretoejUdstyr", UdstyrColumns);
            _insertBlokeringAarsag = BuildInsertCommand(connection, "KoeretoejBlokeringAarsag", BlokeringAarsagColumns);
            _insertTilladelse = BuildInsertCommand(connection, "KoeretoejTilladelse", TilladelseColumns);

            _deleteKoeretoej = BuildDeleteCommand(connection, "Koeretoej");
            _deleteAnvendelse = BuildDeleteCommand(connection, "KoeretoejAnvendelse");
            _deleteSupplerendeKarrosseri = BuildDeleteCommand(connection, "KoeretoejSupplerendeKarrosseri");
            _deleteDrivmiddel = BuildDeleteCommand(connection, "KoeretoejDrivmiddel");
            _deleteUdstyr = BuildDeleteCommand(connection, "KoeretoejUdstyr");
            _deleteBlokeringAarsag = BuildDeleteCommand(connection, "KoeretoejBlokeringAarsag");
            _deleteTilladelse = BuildDeleteCommand(connection, "KoeretoejTilladelse");

            BeginBatch();
        }

        public void Add(StatistikXml vehicle)
        {
            try
            {
                WriteKoeretoej(vehicle);
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == SqliteConstraintViolationErrorCode)
            {
                // Duplicate KoeretoejIdent — see SqliteConstraintViolationErrorCode above. This is the rare
                // path (a handful of thousand times across tens of millions of vehicles, going by the one
                // full run that hit it), so paying for a full delete-then-reinsert only here, instead of on
                // every single vehicle, is what keeps the normal one-row-per-vehicle path free of any added
                // cost. The transaction itself is unaffected by SQLite's default ABORT conflict resolution
                // above — it aborts just the failed INSERT, so the DELETEs and retry below still run inside
                // the same batch transaction as everything else.
                // Non-null: the caller (ImportToSqlite's loop) already skips any vehicle with a null/empty
                // KoeretoejIdent before ever reaching Add(), so WriteKoeretoej could only have failed on
                // this constraint with a real value here.
                DeleteVehicle(vehicle.KoeretoejIdent!);
                WriteKoeretoej(vehicle);
            }

            WriteAnvendelseList(vehicle);
            WriteSupplerendeKarrosseriList(vehicle);
            WriteDrivmiddelList(vehicle);
            WriteUdstyrList(vehicle);
            WriteBlokeringAarsagList(vehicle);
            WriteTilladelseList(vehicle);

            if (++_pending >= _batchSize)
            {
                CommitBatch();
                BeginBatch();
            }
        }

        public void Flush()
        {
            if (_pending > 0)
                CommitBatch();
        }

        private void DeleteVehicle(string koeretoejIdent)
        {
            ExecuteDelete(_deleteKoeretoej, koeretoejIdent);
            ExecuteDelete(_deleteAnvendelse, koeretoejIdent);
            ExecuteDelete(_deleteSupplerendeKarrosseri, koeretoejIdent);
            ExecuteDelete(_deleteDrivmiddel, koeretoejIdent);
            ExecuteDelete(_deleteUdstyr, koeretoejIdent);
            ExecuteDelete(_deleteBlokeringAarsag, koeretoejIdent);
            ExecuteDelete(_deleteTilladelse, koeretoejIdent);
        }

        private static void ExecuteDelete(SqliteCommand command, string koeretoejIdent)
        {
            command.Parameters["@KoeretoejIdent"]!.Value = koeretoejIdent;
            command.ExecuteNonQuery();
        }

        private void BeginBatch()
        {
            _transaction = _connection.BeginTransaction();
            _insertKoeretoej.Transaction = _transaction;
            _insertAnvendelse.Transaction = _transaction;
            _insertSupplerendeKarrosseri.Transaction = _transaction;
            _insertDrivmiddel.Transaction = _transaction;
            _insertUdstyr.Transaction = _transaction;
            _insertBlokeringAarsag.Transaction = _transaction;
            _insertTilladelse.Transaction = _transaction;
            _deleteKoeretoej.Transaction = _transaction;
            _deleteAnvendelse.Transaction = _transaction;
            _deleteSupplerendeKarrosseri.Transaction = _transaction;
            _deleteDrivmiddel.Transaction = _transaction;
            _deleteUdstyr.Transaction = _transaction;
            _deleteBlokeringAarsag.Transaction = _transaction;
            _deleteTilladelse.Transaction = _transaction;
        }

        private void CommitBatch()
        {
            _transaction!.Commit();
            _transaction.Dispose();
            _transaction = null;
            _pending = 0;
        }

        private void WriteKoeretoej(StatistikXml v)
        {
            var grund = v.KoeretoejOplysningGrundStruktur;
            var betegnelse = grund?.KoeretoejBetegnelseStruktur;
            var farve = grund?.KoeretoejFarveStruktur?.FarveTypeStruktur;
            var karrosseri = grund?.KarrosseriTypeStruktur;
            var norm = grund?.KoeretoejNormStruktur?.NormTypeStruktur;
            var miljoe = grund?.KoeretoejMiljoeOplysningStruktur;
            var motor = grund?.KoeretoejMotorStruktur;
            var syn = v.SynResultatStruktur;
            // Multi-usage vehicles carry the full list under KoeretoejAnvendelseSamlingStruktur (written to
            // the KoeretoejAnvendelse child table below); this single Struktur is always present too and
            // holds the vehicle's primary usage, which is what lands on the flat main-table columns.
            var anvendelse = v.KoeretoejAnvendelseStruktur;

            var cmd = _insertKoeretoej;
            SetParam(cmd, "@KoeretoejIdent", v.KoeretoejIdent);
            SetParam(cmd, "@KoeretoejArtNummer", v.KoeretoejArtNummer);
            SetParam(cmd, "@KoeretoejArtNavn", v.KoeretoejArtNavn);
            SetParam(cmd, "@AnvendelseNummer", anvendelse?.Nummer);
            SetParam(cmd, "@AnvendelseNavn", anvendelse?.Navn);
            SetParam(cmd, "@LeasingGyldigFra", v.LeasingGyldigFra);
            SetParam(cmd, "@LeasingGyldigTil", v.LeasingGyldigTil);
            SetParam(cmd, "@RegistreringNummer", v.RegistreringNummerNummer);
            SetParam(cmd, "@RegistreringNummerRettighedGyldigFra", v.RegistreringNummerRettighedGyldigFra);
            SetParam(cmd, "@RegistreringNummerRettighedGyldigTil", v.RegistreringNummerRettighedGyldigTil);
            SetParam(cmd, "@RegistreringNummerUdloebDato", v.RegistreringNummerUdloebDato);
            SetParam(cmd, "@OprettetUdFra", grund?.OprettetUdFra);
            SetParam(cmd, "@Status", grund?.Status);
            SetParam(cmd, "@StatusDato", grund?.StatusDato);
            SetParam(cmd, "@FoersteRegistreringDato", grund?.FoersteRegistreringDato);
            SetParam(cmd, "@StelNummer", grund?.StelNummer);
            SetParam(cmd, "@StelNummerAnbringelse", grund?.StelNummerAnbringelse);
            SetParam(cmd, "@TotalVaegt", grund?.TotalVaegt);
            SetParam(cmd, "@EgenVaegt", grund?.EgenVaegt);
            SetParam(cmd, "@TekniskTotalVaegt", grund?.TekniskTotalVaegt);
            SetParam(cmd, "@AkselAntal", grund?.AkselAntal);
            SetParam(cmd, "@AkselAfstand", grund?.AkselAfstand);
            SetParam(cmd, "@StoersteAkselTryk", grund?.StoersteAkselTryk);
            SetParam(cmd, "@TilkoblingMulighed", grund?.TilkoblingMulighed);
            SetParam(cmd, "@TilkoblingsvaegtUdenBremser", grund?.TilkoblingsvaegtUdenBremser);
            SetParam(cmd, "@TilkoblingsvaegtMedBremser", grund?.TilkoblingsvaegtMedBremser);
            SetParam(cmd, "@TypeAnmeldelseNummer", grund?.TypeAnmeldelseNummer);
            SetParam(cmd, "@TypeGodkendelseNummer", grund?.TypeGodkendelseNummer);
            SetParam(cmd, "@TypegodkendtKategori", grund?.TypegodkendtKategori);
            SetParam(cmd, "@EUVariant", grund?.EUVariant);
            SetParam(cmd, "@EUVersion", grund?.EUVersion);
            SetParam(cmd, "@Kommentar", grund?.Kommentar);
            SetParam(cmd, "@AntalDoere", grund?.AntalDoere);
            SetParam(cmd, "@AntalGear", grund?.AntalGear);
            SetParam(cmd, "@Trafikskade", grund?.Trafikskade);
            SetParam(cmd, "@NCAPTest", grund?.NCAPTest);
            SetParam(cmd, "@Koeretoejstand", grund?.Koeretoejstand);
            SetParam(cmd, "@TraekkendeAksler", grund?.TraekkendeAksler);
            SetParam(cmd, "@EgnetTilTaxi", grund?.EgnetTilTaxi);
            SetParam(cmd, "@KoereklarVaegtMaksimum", grund?.KoereklarVaegtMaksimum);
            SetParam(cmd, "@KoereklarVaegtMinimum", grund?.KoereklarVaegtMinimum);
            SetParam(cmd, "@MaksimumHastighed", grund?.MaksimumHastighed);
            SetParam(cmd, "@ModelAar", grund?.ModelAar);
            SetParam(cmd, "@OevrigtUdstyr", grund?.OevrigtUdstyr);
            SetParam(cmd, "@PaahaengVognTotalVaegt", grund?.PaahaengVognTotalVaegt);
            SetParam(cmd, "@PassagerAntal", grund?.PassagerAntal);
            SetParam(cmd, "@SaettevognTilladtAkselTryk", grund?.SaettevognTilladtAkselTryk);
            SetParam(cmd, "@SiddepladserMaksimum", grund?.SiddepladserMaksimum);
            SetParam(cmd, "@SiddepladserMinimum", grund?.SiddepladserMinimum);
            SetParam(cmd, "@SkammelBelastning", grund?.SkammelBelastning);
            SetParam(cmd, "@SkatteAkselAntal", grund?.SkatteAkselAntal);
            SetParam(cmd, "@SkatteAkselTryk", grund?.SkatteAkselTryk);
            SetParam(cmd, "@SporviddenBagest", grund?.SporviddenBagest);
            SetParam(cmd, "@SporviddenForrest", grund?.SporviddenForrest);
            SetParam(cmd, "@StaapladserMinimum", grund?.StaapladserMinimum);
            SetParam(cmd, "@VVaerdiLuft", grund?.VVaerdiLuft);
            SetParam(cmd, "@VVaerdiMekanisk", grund?.VVaerdiMekanisk);
            SetParam(cmd, "@VeteranKoeretoejOriginal", grund?.VeteranKoeretoejOriginal);
            SetParam(cmd, "@VogntogVaegt", grund?.VogntogVaegt);
            SetParam(cmd, "@FaelgDaek", grund?.FaelgDaek);
            SetParam(cmd, "@Fabrikant", grund?.Fabrikant);
            SetParam(cmd, "@Er30PctVarevogn", grund?.Er30PctVarevogn);
            SetParam(cmd, "@MaerkeNummer", betegnelse?.MaerkeTypeNummer);
            SetParam(cmd, "@MaerkeNavn", betegnelse?.MaerkeTypeNavn);
            SetParam(cmd, "@ModelNummer", betegnelse?.Model?.Nummer);
            SetParam(cmd, "@ModelNavn", betegnelse?.Model?.Navn);
            SetParam(cmd, "@VariantNummer", betegnelse?.Variant?.Nummer);
            SetParam(cmd, "@VariantNavn", betegnelse?.Variant?.Navn);
            SetParam(cmd, "@TypeNummer", betegnelse?.Type?.Nummer);
            SetParam(cmd, "@TypeNavn", betegnelse?.Type?.Navn);
            SetParam(cmd, "@FarveNummer", farve?.Nummer);
            SetParam(cmd, "@FarveNavn", farve?.Navn);
            SetParam(cmd, "@KarrosseriNavn", karrosseri?.Navn);
            SetParam(cmd, "@NormNummer", norm?.Nummer);
            SetParam(cmd, "@NormNavn", norm?.Navn);
            SetParam(cmd, "@MiljoeEmissionCO", miljoe?.EmissionCO);
            SetParam(cmd, "@MiljoeEmissionHCPlusNOX", miljoe?.EmissionHCPlusNOX);
            SetParam(cmd, "@MiljoeEmissionNOX", miljoe?.EmissionNOX);
            SetParam(cmd, "@MiljoeNyttelastvaerdi", miljoe?.Nyttelastvaerdi);
            SetParam(cmd, "@MiljoePartikelFilter", miljoe?.PartikelFilter);
            SetParam(cmd, "@MiljoePartikler", miljoe?.Partikler);
            SetParam(cmd, "@MiljoeRoegtaethed", miljoe?.Roegtaethed);
            SetParam(cmd, "@MiljoeRoegtaethedOmdrejningstal", miljoe?.RoegtaethedOmdrejningstal);
            SetParam(cmd, "@MiljoeTungtNulEmissionKoeretoej", miljoe?.TungtNulEmissionKoeretoej);
            SetParam(cmd, "@MotorCylinderAntal", motor?.CylinderAntal);
            SetParam(cmd, "@MotorSlagVolumen", motor?.SlagVolumen);
            SetParam(cmd, "@MotorSlagVolumenIkkeTilgaengelig", motor?.SlagVolumenIkkeTilgaengelig);
            SetParam(cmd, "@MotorStoersteEffekt", motor?.StoersteEffekt);
            SetParam(cmd, "@MotorStoersteEffektIkkeTilgaengelig", motor?.StoersteEffektIkkeTilgaengelig);
            SetParam(cmd, "@MotorKilometerstand", motor?.Kilometerstand);
            SetParam(cmd, "@MotorKilometerstandDokumentation", motor?.KilometerstandDokumentation);
            SetParam(cmd, "@MotorKilometerstandIkkeTilgaengelig", motor?.KilometerstandIkkeTilgaengelig);
            SetParam(cmd, "@MotorInnovativTeknik", motor?.InnovativTeknik);
            SetParam(cmd, "@MotorInnovativTeknikAntal", motor?.InnovativTeknikAntal);
            SetParam(cmd, "@MotorKoerselStoej", motor?.KoerselStoej);
            SetParam(cmd, "@MotorStandStoej", motor?.StandStoej);
            SetParam(cmd, "@MotorStandStoejOmdrejningstal", motor?.StandStoejOmdrejningstal);
            SetParam(cmd, "@MotorBraendselscelle", motor?.Braendselscelle);
            SetParam(cmd, "@MotorMaerkning", motor?.Maerkning);
            SetParam(cmd, "@SynSynsType", syn?.SynsType);
            SetParam(cmd, "@SynSynsDato", syn?.SynsDato);
            SetParam(cmd, "@SynSynsResultat", syn?.SynsResultat);
            SetParam(cmd, "@SynStatus", syn?.SynStatus);
            SetParam(cmd, "@SynStatusDato", syn?.SynStatusDato);
            SetParam(cmd, "@RegistreringStatus", v.KoeretoejRegistreringStatus);
            SetParam(cmd, "@RegistreringStatusDato", v.KoeretoejRegistreringStatusDato);

            cmd.ExecuteNonQuery();
        }

        private void WriteAnvendelseList(StatistikXml v)
        {
            var items = v.KoeretoejAnvendelseSamlingStruktur?.KoeretoejAnvendelseSamling;
            if (items is null)
                return;

            foreach (var item in items)
            {
                var s = item.Struktur;
                if (s is null)
                    continue;

                SetParam(_insertAnvendelse, "@KoeretoejIdent", v.KoeretoejIdent);
                SetParam(_insertAnvendelse, "@AnvendelseNummer", s.Nummer);
                SetParam(_insertAnvendelse, "@AnvendelseNavn", s.Navn);
                _insertAnvendelse.ExecuteNonQuery();
            }
        }

        private void WriteSupplerendeKarrosseriList(StatistikXml v)
        {
            var items = v.KoeretoejOplysningGrundStruktur?.KoeretoejSupplerendeKarrosseriSamlingStruktur?.KoeretoejSupplerendeKarrosseriSamling;
            if (items is null)
                return;

            foreach (var item in items)
            {
                var t = item.TypeStruktur;
                if (t is null)
                    continue;

                SetParam(_insertSupplerendeKarrosseri, "@KoeretoejIdent", v.KoeretoejIdent);
                SetParam(_insertSupplerendeKarrosseri, "@TypeNummer", t.Nummer);
                SetParam(_insertSupplerendeKarrosseri, "@TypeNavn", t.Navn);
                _insertSupplerendeKarrosseri.ExecuteNonQuery();
            }
        }

        private void WriteDrivmiddelList(StatistikXml v)
        {
            var items = v.KoeretoejOplysningGrundStruktur?.KoeretoejMotorStruktur?.KoeretoejDrivmiddelSamlingStruktur?.KoeretoejDrivmiddelSamling;
            if (items is null)
                return;

            foreach (var item in items)
            {
                var d = item.DrivmiddelStruktur;
                if (d is null)
                    continue;

                SetParam(_insertDrivmiddel, "@KoeretoejIdent", v.KoeretoejIdent);
                SetParam(_insertDrivmiddel, "@DrivkraftTypeNummer", d.DrivkraftTypeStruktur?.Nummer);
                SetParam(_insertDrivmiddel, "@DrivkraftTypeNavn", d.DrivkraftTypeStruktur?.Navn);
                SetParam(_insertDrivmiddel, "@KmPerLiter", d.KoeretoejBraendstofStruktur?.KmPerLiter);
                SetParam(_insertDrivmiddel, "@CO2Udslip", d.KoeretoejBraendstofStruktur?.CO2Udslip);
                SetParam(_insertDrivmiddel, "@ElektriskForbrug", d.KoeretoejElforbrugStruktur?.ElektriskForbrug);
                SetParam(_insertDrivmiddel, "@MaaleNormNummer", d.MaaleNormStruktur?.Nummer);
                SetParam(_insertDrivmiddel, "@MaaleNormNavn", d.MaaleNormStruktur?.Navn);
                SetParam(_insertDrivmiddel, "@ErPrimaer", d.DrivmiddelPrimaer);
                _insertDrivmiddel.ExecuteNonQuery();
            }
        }

        private void WriteUdstyrList(StatistikXml v)
        {
            var items = v.KoeretoejOplysningGrundStruktur?.KoeretoejUdstyrSamlingStruktur?.KoeretoejUdstyrSamling;
            if (items is null)
                return;

            foreach (var item in items)
            {
                var u = item.UdstyrStruktur;
                if (u is null)
                    continue;

                SetParam(_insertUdstyr, "@KoeretoejIdent", v.KoeretoejIdent);
                SetParam(_insertUdstyr, "@UdstyrTypeNummer", u.TypeStruktur?.Nummer);
                SetParam(_insertUdstyr, "@UdstyrTypeNavn", u.TypeStruktur?.Navn);
                SetParam(_insertUdstyr, "@Antal", u.Antal);
                SetParam(_insertUdstyr, "@VisesVedSyn", u.TypeStruktur?.VisesVedSyn);
                SetParam(_insertUdstyr, "@VisesVedForespoergsel", u.TypeStruktur?.VisesVedForespoergsel);
                SetParam(_insertUdstyr, "@VisesVedStandardOprettelse", u.TypeStruktur?.VisesVedStandardOprettelse);
                _insertUdstyr.ExecuteNonQuery();
            }
        }

        private void WriteBlokeringAarsagList(StatistikXml v)
        {
            var items = v.KoeretoejOplysningGrundStruktur?.KoeretoejBlokeringAarsagListeStruktur?.KoeretoejBlokeringAarsagListe?.KoeretoejBlokeringAarsag;
            if (items is null)
                return;

            foreach (var a in items)
            {
                SetParam(_insertBlokeringAarsag, "@KoeretoejIdent", v.KoeretoejIdent);
                SetParam(_insertBlokeringAarsag, "@TypeNummer", a.TypeNummer);
                SetParam(_insertBlokeringAarsag, "@TypeNavn", a.TypeNavn);
                _insertBlokeringAarsag.ExecuteNonQuery();
            }
        }

        private void WriteTilladelseList(StatistikXml v)
        {
            var items = v.TilladelseSamling?.Tilladelse;
            if (items is null)
                return;

            foreach (var item in items)
            {
                var t = item.TilladelseStruktur;
                if (t is null)
                    continue;

                var reference = t.TypeDetaljeValg?.VariabelKombination?.KoeretoejGenerelIdentifikatorStruktur?.Valg?.KoeretoejIdent;

                SetParam(_insertTilladelse, "@KoeretoejIdent", v.KoeretoejIdent);
                SetParam(_insertTilladelse, "@GyldigFra", t.GyldigFra);
                SetParam(_insertTilladelse, "@Kommentar", t.Kommentar);
                SetParam(_insertTilladelse, "@TypeNummer", t.TypeStruktur?.Nummer);
                SetParam(_insertTilladelse, "@TypeNavn", t.TypeStruktur?.Navn);
                SetParam(_insertTilladelse, "@DetaljeReferenceKoeretoejIdent", reference);
                _insertTilladelse.ExecuteNonQuery();
            }
        }

        public void Dispose()
        {
            _transaction?.Dispose();
            _insertKoeretoej.Dispose();
            _insertAnvendelse.Dispose();
            _insertSupplerendeKarrosseri.Dispose();
            _insertDrivmiddel.Dispose();
            _insertUdstyr.Dispose();
            _insertBlokeringAarsag.Dispose();
            _insertTilladelse.Dispose();
            _deleteKoeretoej.Dispose();
            _deleteAnvendelse.Dispose();
            _deleteSupplerendeKarrosseri.Dispose();
            _deleteDrivmiddel.Dispose();
            _deleteUdstyr.Dispose();
            _deleteBlokeringAarsag.Dispose();
            _deleteTilladelse.Dispose();
        }
    }
}
