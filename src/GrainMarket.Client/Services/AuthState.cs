using GrainMarket.Application.Auth;
using GrainMarket.Application.Roles;
using GrainMarket.Domain.Entities;

namespace GrainMarket.Client.Services;

/// <summary>
/// Holds the current session's JWT + role permissions in memory, and mirrors the token to
/// SecureStorage so it survives an app restart. Nav menu items and page-level guards read
/// Permissions to decide what to show — a mirror of the server-side ModulePermission checks, not
/// a substitute for them (the Api still enforces everything server-side).
/// </summary>
public class AuthState
{
    private const string TokenStorageKey = "auth_token";

    public string? Token { get; private set; }
    public string? Username { get; private set; }
    public string? FullName { get; private set; }
    public string? RoleName { get; private set; }
    public List<RolePermissionDto> Permissions { get; private set; } = new();

    public bool IsAuthenticated => !string.IsNullOrEmpty(Token);

    public event Action? Changed;

    public void SetSession(LoginResponse login, List<RolePermissionDto> permissions)
    {
        Token = login.Token;
        Username = login.Username;
        FullName = login.FullName;
        RoleName = login.RoleName;
        Permissions = permissions;
        Changed?.Invoke();
    }

    public void Clear()
    {
        Token = null;
        Username = null;
        FullName = null;
        RoleName = null;
        Permissions = new();
        Changed?.Invoke();
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

    public void ClearPersistedToken() => SecureStorage.Default.Remove(TokenStorageKey);
}
