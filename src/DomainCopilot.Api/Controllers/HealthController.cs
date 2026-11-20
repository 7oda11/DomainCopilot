using DomainCopilot.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DomainCopilot.Api.Controllers;

[ApiController]
[Route("health")]
public sealed class HealthController(AppDbContext dbContext) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("live")]
    public IActionResult Live() => Ok(new { status = "live" });

    [AllowAnonymous]
    [HttpGet("ready")]
    public async Task<IActionResult> Ready(CancellationToken cancellationToken)
    {
        if (!await dbContext.Database.CanConnectAsync(cancellationToken))
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { status = "not-ready", dependency = "sql-server" });
        }

        return Ok(new { status = "ready", dependencies = new[] { "sql-server" } });
    }
}
