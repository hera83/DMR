namespace DMR.Services.Dmr.Dtos;

/// <summary>One row of a vehicle's permits ("tilladelser"), from the <c>KoeretoejTilladelse</c> table.
/// Supporting item type nested inside <see cref="DmrVehicleDto"/>, not a standalone request/response pair —
/// it never travels on its own.</summary>
public class DmrVehicleTilladelseDto
{
    public string? GyldigFra { get; set; }
    public string? Kommentar { get; set; }
    public long? TypeNummer { get; set; }
    public string? TypeNavn { get; set; }

    /// <summary>When this permit references another vehicle (e.g. a coupled trailer), that vehicle's
    /// <c>KoeretoejIdent</c> — see <c>DmrService.WriteTilladelseList</c>.</summary>
    public string? DetaljeReferenceKoeretoejIdent { get; set; }
}
