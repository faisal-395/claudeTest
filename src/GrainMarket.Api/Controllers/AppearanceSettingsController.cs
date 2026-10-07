using GrainMarket.Api.Common;
using GrainMarket.Application.Appearance;
using GrainMarket.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GrainMarket.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/appearance-settings")]
public class AppearanceSettingsController : ControllerBase
{
    private readonly IAppearanceSettingsService _service;

    public AppearanceSettingsController(IAppearanceSettingsService service)
    {
        _service = service;
    }

    // Deliberately not gated by ModulePermission: every authenticated page needs every scope's
    // current theme colors to render itself, regardless of the caller's Setup access.
    [HttpGet]
    public async Task<ActionResult<List<AppearanceSettingsDto>>> GetAll(CancellationToken ct) => Ok(await _service.GetAllAsync(ct));

    [ModulePermission(ModuleName.SetupAppearance, PermissionAction.Edit)]
    [HttpPut("{scope}")]
    public async Task<ActionResult<AppearanceSettingsDto>> Update(ThemeScope scope, UpdateAppearanceSettingsRequest request, CancellationToken ct)
        => Ok(await _service.UpdateAsync(scope, request, ct));
}
