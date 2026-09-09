namespace GrainMarket.Application.Auth;

public record LoginRequest(string Username, string Password);

public record LoginResponse(string Token, string RefreshToken, int UserId, string Username, string FullName, string RoleName, DateTime ExpiresAtUtc);

public record RefreshTokenRequest(string RefreshToken);
