using GrainMarket.Api.Common;
using GrainMarket.Application.Roles;
using GrainMarket.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GrainMarket.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/roles")]
[ModulePermission(ModuleName.SetupUsersRoles)]
public class RolesController : ControllerBase
{
    private readonly IRoleService _service;

    public RolesController(IRoleService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<List<RoleDto>>> GetAll(CancellationToken ct) => Ok(await _service.GetAllAsync(ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<RoleDto>> GetById(int id, CancellationToken ct) => Ok(await _service.GetByIdAsync(id, ct));

    [ModulePermission(ModuleName.SetupUsersRoles, PermissionAction.Create)]
    [HttpPost]
    public async Task<ActionResult<RoleDto>> Create(UpsertRoleRequest request, CancellationToken ct)
    {
        var result = await _service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [ModulePermission(ModuleName.SetupUsersRoles, PermissionAction.Edit)]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<RoleDto>> Update(int id, UpsertRoleRequest request, CancellationToken ct)
        => Ok(await _service.UpdateAsync(id, request, ct));
}
