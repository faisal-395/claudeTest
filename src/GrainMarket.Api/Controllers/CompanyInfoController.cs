using GrainMarket.Api.Common;
using GrainMarket.Application.Company;
using GrainMarket.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GrainMarket.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/company-info")]
public class CompanyInfoController : ControllerBase
{
    private readonly ICompanyInfoService _service;

    public CompanyInfoController(ICompanyInfoService service)
    {
        _service = service;
    }

    // Deliberately not gated by ModulePermission: every printed receipt needs the letterhead
    // regardless of the caller's Setup access, so any authenticated user can read it.
    [HttpGet]
    public async Task<ActionResult<CompanyInfoDto>> Get(CancellationToken ct) => Ok(await _service.GetAsync(ct));

    [ModulePermission(ModuleName.SetupCompanyInfo, PermissionAction.Edit)]
    [HttpPut]
    public async Task<ActionResult<CompanyInfoDto>> Update(UpdateCompanyInfoRequest request, CancellationToken ct)
        => Ok(await _service.UpdateAsync(request, ct));
}
