using GrainMarket.Application.Auth;
using GrainMarket.Application.Roles;
using GrainMarket.Domain.Entities;

namespace GrainMarket.Client.Services;

/// <summary>
/// Holds the current session's JWT + refresh token + role permissions in memory, and mirrors them
/// to SecureStorage so they survive an app restart. Nav menu items and page-level guards read
/// Permissions to decide what to show — a mirror of the server-side ModulePermission checks, not
/// a substitute for them (the Api still enforces everything server-side).
/// </summary>
public class AuthState
{
    private const string TokenStorageKey = "auth_token";
    private const string RefreshTokenStorageKey = "auth_refresh_token";
    private const string ExpiryStorageKey = "auth_token_expiry";

    public string? Token { get; private set; }
    public string? RefreshToken { get; private set; }
    public DateTime? ExpiresAtUtc { get; private set; }
    public string? Username { get; private set; }
    public string? FullName { get; private set; }
    public string? RoleName { get; private set; }
    public List<RolePermissionDto> Permissions { get; private set; } = new();

    public bool IsAuthenticated => !string.IsNullOrEmpty(Token);

    /// <summary>True once the access token is missing, already expired, or about to expire within
    /// <paramref name="buffer"/> — used to trigger a proactive silent refresh before it actually lapses.</summary>
    public bool IsExpiringSoon(TimeSpan buffer) =>
        !IsAuthenticated || ExpiresAtUtc is null || DateTime.UtcNow >= ExpiresAtUtc.Value - buffer;

    public event Action? Changed;

    /// <summary>Raised when the session could not be kept alive (refresh token missing, expired, or
    /// revoked) — subscribers should send the user back to the login screen instead of letting the
    /// next API call surface as an unhandled error.</summary>
    public event Action? SessionExpired;

    /// <summary>Attaches the token immediately after login, before any follow-up authenticated call
    /// (e.g. fetching role permissions) — TokenAuthHandler reads Token on every outgoing request,
    /// so a follow-up call made before this runs goes out unauthenticated and gets a 401.</summary>
    public void SetToken(LoginResponse login)
    {
        Token = login.Token;
        RefreshToken = login.RefreshToken;
        ExpiresAtUtc = login.ExpiresAtUtc;
        Username = login.Username;
        FullName = login.FullName;
        RoleName = login.RoleName;
        Changed?.Invoke();
    }

    public void SetPermissions(List<RolePermissionDto> permissions)
    {
        Permissions = permissions;
        Changed?.Invoke();
    }

    public void Clear()
    {
        Token = null;
        RefreshToken = null;
        ExpiresAtUtc = null;
        Username = null;
        FullName = null;
        RoleName = null;
        Permissions = new();
        Changed?.Invoke();
    }

    /// <summary>Clears the session because it couldn't be kept alive (as opposed to an explicit
    /// user logout) and notifies subscribers so they can redirect to the login screen.</summary>
    public void ClearDueToExpiry()
    {
        Clear();
        ClearPersistedToken();
        SessionExpired?.Invoke();
    }

    public bool CanView(ModuleName module) => HasFlag(module, p => p.CanView);
    public bool CanCreate(ModuleName module) => HasFlag(module, p => p.CanCreate);
    public bool CanEdit(ModuleName module) => HasFlag(module, p => p.CanEdit);
    public bool CanDelete(ModuleName module) => HasFlag(module, p => p.CanDelete);

    private bool HasFlag(ModuleName module, Func<RolePermissionDto, bool> selector)
    {
        var permission = Permissions.FirstOrDefault(p => p.Module == module);
        return permission is not null && selector(permission);
    }

    public async Task PersistTokenAsync()
    {
        if (Token is not null)
        {
            await SecureStorage.Default.SetAsync(TokenStorageKey, Token);
        }
        if (RefreshToken is not null)
        {
            await SecureStorage.Default.SetAsync(RefreshTokenStorageKey, RefreshToken);
        }
        if (ExpiresAtUtc is not null)
        {
            await SecureStorage.Default.SetAsync(ExpiryStorageKey, ExpiresAtUtc.Value.ToString("o"));
        }
    }

    public async Task<string?> LoadPersistedTokenAsync()
    {
        try
        {
            return await SecureStorage.Default.GetAsync(TokenStorageKey);
        }
        catch
        {
            return null;
        }
    }

    public void ClearPersistedToken()
    {
        SecureStorage.Default.Remove(TokenStorageKey);
        SecureStorage.Default.Remove(RefreshTokenStorageKey);
        SecureStorage.Default.Remove(ExpiryStorageKey);
    }
}
