namespace DMR.Services.Dmr.Dtos;

/// <summary>One row of a vehicle's full usage ("anvendelse") list, from the <c>KoeretoejAnvendelse</c>
/// table — only populated for multi-usage vehicles (see services/dmr/docs "Cardinality decisions"). This is
/// a supporting item type nested inside <see cref="DmrVehicleDto"/>, not a standalone request/response
/// pair — it never travels on its own.</summary>
public class DmrVehicleAnvendelseDto
{
    public long? AnvendelseNummer { get; set; }
    public string? AnvendelseNavn { get; set; }
}
