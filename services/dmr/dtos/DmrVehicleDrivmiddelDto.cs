namespace DMR.Services.Dmr.Dtos;

/// <summary>One row of a vehicle's fuel/power sources (e.g. two rows for a hybrid), from the
/// <c>KoeretoejDrivmiddel</c> table. Supporting item type nested inside <see cref="DmrVehicleDto"/>, not a
/// standalone request/response pair — it never travels on its own.</summary>
public class DmrVehicleDrivmiddelDto
{
    public long? DrivkraftTypeNummer { get; set; }
    public string? DrivkraftTypeNavn { get; set; }
    public double? KmPerLiter { get; set; }
    public double? CO2Udslip { get; set; }
    public double? ElektriskForbrug { get; set; }
    public long? MaaleNormNummer { get; set; }
    public string? MaaleNormNavn { get; set; }
    public long? ErPrimaer { get; set; }
}
