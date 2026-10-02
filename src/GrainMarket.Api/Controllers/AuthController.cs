using GrainMarket.Application.Auth;
using GrainMarket.Application.Common.Interfaces;
using GrainMarket.Application.Roles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GrainMarket.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IRoleService _roleService;
    private readonly ICurrentUser _currentUser;

    public AuthController(IAuthService authService, IRoleService roleService, ICurrentUser currentUser)
    {
        _authService = authService;
        _roleService = roleService;
        _currentUser = currentUser;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken ct)
    {
        var response = await _authService.LoginAsync(request, ct);
        return Ok(response);
    }

    /// <summary>Exchanges a still-valid refresh token for a new access/refresh token pair —
    /// lets the client silently recover from an expired JWT (e.g. after the app sat idle)
    /// instead of forcing the user back to the login screen.</summary>
    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<ActionResult<LoginResponse>> Refresh(RefreshTokenRequest request, CancellationToken ct)
    {
        var response = await _authService.RefreshAsync(request, ct);
        return Ok(response);
    }

    [AllowAnonymous]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(RefreshTokenRequest request, CancellationToken ct)
    {
        await _authService.LogoutAsync(request, ct);
        return NoContent();
    }

    /// <summary>What the just-logged-in (or already-authenticated) user is themselves allowed to
    /// do — used by the client right after login to populate AuthState.Permissions, which drives
    /// the nav menu and page guards. Deliberately NOT gated by ModulePermission(SetupUsersRoles):
    /// every authenticated user needs their own permissions to use the app at all, regardless of
    /// whether their role can manage Setup &gt; Users &amp; Roles — RolesController's GetAll (the
    /// full role/permission matrix for every role) is the one that stays admin-only.</summary>
    [Authorize]
    [HttpGet("me/permissions")]
    public async Task<ActionResult<List<RolePermissionDto>>> GetMyPermissions(CancellationToken ct)
    {
        if (_currentUser.RoleId is null) return Ok(new List<RolePermissionDto>());
        var role = await _roleService.GetByIdAsync(_currentUser.RoleId.Value, ct);
        return Ok(role.Permissions);
    }
}
