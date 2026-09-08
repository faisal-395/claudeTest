using GrainMarket.Api.Common;
using GrainMarket.Application.Users;
using GrainMarket.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GrainMarket.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/users")]
[ModulePermission(ModuleName.SetupUsersRoles)]
public class UsersController : ControllerBase
{
    private readonly IUserService _service;

    public UsersController(IUserService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<List<UserDto>>> GetAll(CancellationToken ct) => Ok(await _service.GetAllAsync(ct));

    [ModulePermission(ModuleName.SetupUsersRoles, PermissionAction.Create)]
    [HttpPost]
    public async Task<ActionResult<UserDto>> Create(CreateUserRequest request, CancellationToken ct)
        => Ok(await _service.CreateAsync(request, ct));

    [ModulePermission(ModuleName.SetupUsersRoles, PermissionAction.Edit)]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<UserDto>> Update(int id, UpdateUserRequest request, CancellationToken ct)
        => Ok(await _service.UpdateAsync(id, request, ct));
}
