namespace DMR.Services.Dmr.Dtos;

/// <summary>One row of a vehicle's blocking reasons, from the <c>KoeretoejBlokeringAarsag</c> table.
/// Supporting item type nested inside <see cref="DmrVehicleDto"/>, not a standalone request/response pair —
/// it never travels on its own.</summary>
public class DmrVehicleBlokeringAarsagDto
{
    public long? TypeNummer { get; set; }
    public string? TypeNavn { get; set; }
}
