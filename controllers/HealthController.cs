using DMR.Services.Health.Dtos;
using DMR.Services.Health.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DMR.Controllers;

/// <summary>
/// Keepalive/health endpoint — see services/health/docs. Anonymous by design: this is meant to be pollable
/// by external uptime monitors and load balancers, which typically can't attach an API key, so it opts out
/// of the app-wide fallback policy that otherwise requires one on every endpoint (see
/// <see cref="Program"/>/<c>ApiKeyAuthenticationHandler</c>).
/// </summary>
[ApiController]
[Route("[controller]/[action]")]
[AllowAnonymous]
public class HealthController(IHealthService healthService) : ControllerBase
{
    /// <summary>Runs the health check and returns it. HTTP 503 when <see cref="HealthCheckResponseDto.Status"/>
    /// is <see cref="HealthStatus.Unhealthy"/> (so uptime monitors/load balancers can alert on it the normal
    /// way), 200 otherwise — a <see cref="HealthStatus.Warning"/> result ("Afventer ny data fra
    /// Motorregisteret.") is still a 200, since the API itself is working fine.</summary>
    [HttpGet]
    public async Task<ActionResult<HealthCheckResponseDto>> Check(CancellationToken cancellationToken)
    {
        var response = await healthService.CheckAsync(cancellationToken);
        return response.Status == HealthStatus.Unhealthy
            ? StatusCode(StatusCodes.Status503ServiceUnavailable, response)
            : Ok(response);
    }
}
