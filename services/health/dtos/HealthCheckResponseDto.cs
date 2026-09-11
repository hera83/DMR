namespace DMR.Services.Health.Dtos;

/// <summary>Result of a keepalive/health check — the standard "is this API up and how's it doing" fields
/// (status, timestamp, response time, uptime, environment, version) plus <see cref="DmrDataFreshness"/>, the
/// project's one extra validation parameter. Response-only: <c>IHealthService.CheckAsync</c> takes no
/// request parameters (there's nothing a caller could meaningfully supply for a health check), so per
/// <c>IIdentityService.EnsureSeededAsync</c>'s precedent there's no paired request DTO — exception to the
/// request/response pairing rule.</summary>
public class HealthCheckResponseDto
{
    /// <summary>Overall result — the worst of the individual checks below (currently just
    /// <see cref="DmrDataFreshness"/>, or <see cref="HealthStatus.Unhealthy"/> if the check itself threw).
    /// <c>HealthController</c> returns HTTP 503 when this is <see cref="HealthStatus.Unhealthy"/>, 200
    /// otherwise (a <see cref="HealthStatus.Warning"/> result is still a 200 — the API itself is fine,
    /// something just needs attention).</summary>
    public HealthStatus Status { get; set; }

    /// <summary>When this check ran, in UTC.</summary>
    public DateTime TimestampUtc { get; set; }

    /// <summary>How long the check itself took to run, in milliseconds.</summary>
    public double ResponseTimeMs { get; set; }

    /// <summary>How long this process has been running, in seconds.</summary>
    public double UptimeSeconds { get; set; }

    /// <summary>ASPNETCORE_ENVIRONMENT this process is running as (e.g. "Development", "Production").</summary>
    public string Environment { get; set; } = string.Empty;

    /// <summary>Assembly version of the running build, e.g. "1.0.0.0". Null if it couldn't be read.</summary>
    public string? Version { get; set; }

    /// <summary>The DMR data-freshness check — see <see cref="DmrDataFreshnessDto"/>. Null only if the check
    /// itself failed before it could run (see <see cref="ErrorMessage"/>).</summary>
    public DmrDataFreshnessDto? DmrDataFreshness { get; set; }

    /// <summary>Set only if the health check itself threw (e.g. the database couldn't be reached) — distinct
    /// from <see cref="DmrDataFreshnessDto.Message"/>, which describes stale-but-readable data.</summary>
    public string? ErrorMessage { get; set; }
}
