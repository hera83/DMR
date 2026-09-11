namespace DMR.Services.Dmr.Dtos;

/// <summary>Response from <see cref="Interfaces.IDmrService.LookupVehicleAsync"/>.</summary>
public class DmrLookupVehicleResponseDto
{
    public bool Success { get; set; }

    /// <summary>Set when <see cref="Success"/> is false — e.g. no dataset has been ingested yet, or no
    /// vehicle with the requested registration number exists in the current one.</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>The matched vehicle, its child collections included. Null when <see cref="Success"/> is
    /// false.</summary>
    public DmrVehicleDto? Vehicle { get; set; }

    /// <summary>Name of the source export file the current dataset was built from — see
    /// <see cref="Data.Models.DmrDataset.SourceFileName"/>. Lets a caller tell how fresh the data is.</summary>
    public string? DatasetSourceFileName { get; set; }

    /// <summary>When the current dataset was registered (UTC) — see
    /// <see cref="Data.Models.DmrDataset.CreatedAtUtc"/>.</summary>
    public DateTime? DatasetCreatedAtUtc { get; set; }
}
