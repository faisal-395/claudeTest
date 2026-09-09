namespace GrainMarket.Application.Auth;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<LoginResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken ct = default);
    Task LogoutAsync(RefreshTokenRequest request, CancellationToken ct = default);
}
