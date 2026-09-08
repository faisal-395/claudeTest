namespace GrainMarket.Application.Auth;

public record LoginRequest(string Username, string Password);

public record LoginResponse(string Token, int UserId, string Username, string FullName, string RoleName, DateTime ExpiresAtUtc);
