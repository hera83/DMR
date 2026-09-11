using DMR.Services.Health.Dtos;

namespace DMR.Services.Health.Interfaces;

/// <summary>
/// Reports whether the API is up and how its data is doing — see services/health/docs. Backs the anonymous
/// keepalive endpoint (<c>HealthController</c>): fast enough to poll frequently (a single indexed read
/// against <see cref="Data.AppIdentityDbContext"/>, no I/O against the DMR SQLite databases themselves), so
/// it's safe to await directly inside an HTTP request.
/// </summary>
public interface IHealthService
{
    /// <summary>Runs the health check: the standard keepalive fields (uptime, environment, version, response
    /// time) plus a freshness check on the newest ingested <see cref="Data.Models.DmrDataset"/> against
    /// Motorstyrelsen's weekly update cadence. Never throws for a stale/missing dataset — that's reported as
    /// <see cref="HealthStatus.Warning"/>/<see cref="HealthStatus.Unhealthy"/> on the response, not an
    /// exception; only a failure to reach the database itself is caught and reported via
    /// <see cref="HealthCheckResponseDto.ErrorMessage"/>.</summary>
    Task<HealthCheckResponseDto> CheckAsync(CancellationToken cancellationToken = default);
}
