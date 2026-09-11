using System.Text.Json.Serialization;

namespace DMR.Services.Health.Dtos;

/// <summary>Three-level severity used both as <see cref="HealthCheckResponseDto.Status"/> (the overall
/// result) and as <see cref="DmrDataFreshnessDto.Status"/> (the DMR data-freshness check alone) — see
/// services/health/docs for how each check maps onto these levels and what HTTP status code
/// <c>HealthController</c> returns for each. Serialized as its member name (e.g. "Healthy") rather than the
/// default numeric value, via <see cref="JsonStringEnumConverter"/>, so a response reads as
/// <c>"status": "Warning"</c> rather than <c>"status": 1</c>.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum HealthStatus
{
    /// <summary>Everything checked is within its expected bounds.</summary>
    Healthy,

    /// <summary>Something needs attention but isn't yet considered a failure — e.g. the newest DMR dataset
    /// is a bit older than the weekly update cadence would suggest, but not old enough to call stale.</summary>
    Warning,

    /// <summary>A check failed outright — e.g. the newest DMR dataset is far older than the weekly update
    /// cadence would ever explain, or no dataset has been ingested yet, or the health check itself threw.</summary>
    Unhealthy
}
