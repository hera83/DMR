namespace DMR.Services.Dmr.Dtos;

/// <summary>One row of a vehicle's equipment, from the <c>KoeretoejUdstyr</c> table. Supporting item type
/// nested inside <see cref="DmrVehicleDto"/>, not a standalone request/response pair — it never travels on
/// its own.</summary>
public class DmrVehicleUdstyrDto
{
    public long? UdstyrTypeNummer { get; set; }
    public string? UdstyrTypeNavn { get; set; }
    public long? Antal { get; set; }
    public long? VisesVedSyn { get; set; }
    public long? VisesVedForespoergsel { get; set; }
    public long? VisesVedStandardOprettelse { get; set; }
}
