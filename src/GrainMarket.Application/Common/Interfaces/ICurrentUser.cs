namespace GrainMarket.Application.Common.Interfaces;

/// <summary>Resolved from the authenticated request's JWT claims by the Api layer.</summary>
public interface ICurrentUser
{
    int? UserId { get; }
    string? Username { get; }
    int? RoleId { get; }
    string? RoleName { get; }
    bool IsAuthenticated { get; }

    /// <summary>Role ids allowed to see/select IsProtected=true chart-of-accounts.</summary>
    bool HasAllowedAccountRole(IEnumerable<int> allowedRoleIds);
}
