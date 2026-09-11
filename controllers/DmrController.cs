using DMR.Services.Dmr.Dtos;
using DMR.Services.Dmr.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DMR.Controllers;

/// <summary>
/// Looks up individual vehicles in the newest DMR dataset (see <c>bgServices/DmrWorker.cs</c> for how that
/// dataset is downloaded, converted, and kept up to date). Read-only — this controller has no way to
/// trigger an ingest itself.
/// </summary>
[ApiController]
[Route("[controller]/[action]")]
public class DmrController(IDmrService dmrService) : ControllerBase
{
    /// <summary>Looks up a single vehicle by its Danish registration number (e.g. "AB12345") in the newest
    /// DMR dataset. Case-insensitive. Returns 404 if no dataset has been ingested yet, or no vehicle with
    /// that registration number exists in the current one.</summary>
    [HttpGet("{registreringNummer}")]
    public async Task<ActionResult<DmrLookupVehicleResponseDto>> GetVehicleAsync(string registreringNummer, CancellationToken cancellationToken)
    {
        var response = await dmrService.LookupVehicleAsync(
            new DmrLookupVehicleRequestDto { RegistreringNummer = registreringNummer }, cancellationToken);

        return response.Success ? Ok(response) : NotFound(response);
    }
}
