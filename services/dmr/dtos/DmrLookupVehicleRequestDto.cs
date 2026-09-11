namespace DMR.Services.Dmr.Dtos;

/// <summary>Request for <see cref="Interfaces.IDmrService.LookupVehicleAsync"/>. Looked up in whichever
/// <see cref="Data.Models.DmrDataset"/> is newest (see services/dmr/docs) — not a field here, since callers
/// can't target a specific historical dataset, only ever "whatever is current".</summary>
public class DmrLookupVehicleRequestDto
{
    /// <summary>Danish vehicle registration number ("nummerplade"), e.g. "AB12345". Matched
    /// case-insensitively against <c>Koeretoej.RegistreringNummer</c>.</summary>
    public string RegistreringNummer { get; set; } = string.Empty;
}
