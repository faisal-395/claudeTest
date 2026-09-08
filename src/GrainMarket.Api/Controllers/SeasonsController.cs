using GrainMarket.Api.Common;
using GrainMarket.Application.Seasons;
using GrainMarket.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GrainMarket.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/seasons")]
[ModulePermission(ModuleName.SetupSeasons)]
public class SeasonsController : ControllerBase
{
    private readonly ISeasonService _service;

    public SeasonsController(ISeasonService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<List<SeasonDto>>> GetAll(CancellationToken ct) => Ok(await _service.GetAllAsync(ct));

    [ModulePermission(ModuleName.SetupSeasons, PermissionAction.Create)]
    [HttpPost]
    public async Task<ActionResult<SeasonDto>> Create(UpsertSeasonRequest request, CancellationToken ct)
        => Ok(await _service.CreateAsync(request, ct));

    [ModulePermission(ModuleName.SetupSeasons, PermissionAction.Edit)]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<SeasonDto>> Update(int id, UpsertSeasonRequest request, CancellationToken ct)
        => Ok(await _service.UpdateAsync(id, request, ct));
}
