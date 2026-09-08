using GrainMarket.Api.Common;
using GrainMarket.Application.Dashboard;
using GrainMarket.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GrainMarket.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/dashboard")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [ModulePermission(ModuleName.Dashboard)]
    [HttpGet("summary")]
    public async Task<ActionResult<DashboardSummaryDto>> GetSummary([FromQuery] DateTime? date, CancellationToken ct)
    {
        return Ok(await _dashboardService.GetSummaryAsync(date ?? DateTime.UtcNow, ct));
    }
}
