using System.Security.Claims;
using GrainMarket.Application.Common.Interfaces;

namespace GrainMarket.Api.Common;

public class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public int? UserId
    {
        get
        {
            var value = Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(value, out var id) ? id : null;
        }
    }

    public string? Username => Principal?.FindFirst(ClaimTypes.Name)?.Value;

    public int? RoleId
    {
        get
        {
            var value = Principal?.FindFirst("roleId")?.Value;
            return int.TryParse(value, out var id) ? id : null;
        }
    }

    public string? RoleName => Principal?.FindFirst(ClaimTypes.Role)?.Value;

    public bool HasAllowedAccountRole(IEnumerable<int> allowedRoleIds)
    {
        if (RoleId is null) return false;
        return allowedRoleIds.Contains(RoleId.Value);
    }
}
