using System.Security.Cryptography;
using GrainMarket.Application.Common.Interfaces;
using GrainMarket.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GrainMarket.Application.Auth;

public class AuthService : IAuthService
{
    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IDateTimeProvider _clock;

    public AuthService(IApplicationDbContext db, IPasswordHasher passwordHasher, IJwtTokenService jwtTokenService, IDateTimeProvider clock)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _clock = clock;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await _db.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Username == request.Username && !u.IsDeleted, ct);

        if (user is null || !user.IsActive || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid username or password.");
        }

        user.LastLoginAtUtc = _clock.UtcNow;
        var response = await IssueTokenPairAsync(user, ct);
        await _db.SaveChangesAsync(ct);
        return response;
    }

    public async Task<LoginResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken ct = default)
    {
        var hash = Hash(request.RefreshToken);
        var existing = await _db.RefreshTokens
            .Include(t => t.User).ThenInclude(u => u.Role)
            .FirstOrDefaultAsync(t => t.TokenHash == hash, ct);

        if (existing is null || existing.RevokedAtUtc is not null || existing.ExpiresAtUtc <= _clock.UtcNow)
        {
            throw new UnauthorizedAccessException("Refresh token is invalid or expired. Please sign in again.");
        }

        if (!existing.User.IsActive || existing.User.IsDeleted)
        {
            throw new UnauthorizedAccessException("This account is no longer active.");
        }

        var response = await IssueTokenPairAsync(existing.User, ct, revoking: existing);
        await _db.SaveChangesAsync(ct);
        return response;
    }

    public async Task LogoutAsync(RefreshTokenRequest request, CancellationToken ct = default)
    {
        var hash = Hash(request.RefreshToken);
        var existing = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, ct);
        if (existing is not null && existing.RevokedAtUtc is null)
        {
            existing.RevokedAtUtc = _clock.UtcNow;
            await _db.SaveChangesAsync(ct);
        }
    }

    private async Task<LoginResponse> IssueTokenPairAsync(User user, CancellationToken ct, RefreshToken? revoking = null)
    {
        var accessToken = _jwtTokenService.GenerateToken(user);
        var refreshTokenPlainText = GenerateRefreshTokenPlainText();
        var newTokenHash = Hash(refreshTokenPlainText);

        if (revoking is not null)
        {
            revoking.RevokedAtUtc = _clock.UtcNow;
            revoking.ReplacedByTokenHash = newTokenHash;
        }

        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = newTokenHash,
            ExpiresAtUtc = _clock.UtcNow.Add(_jwtTokenService.RefreshTokenLifetime)
        });

        return new LoginResponse(
            accessToken,
            refreshTokenPlainText,
            user.Id,
            user.Username,
            user.FullName,
            user.Role.Name,
            _clock.UtcNow.Add(_jwtTokenService.AccessTokenLifetime));
    }

    private static string GenerateRefreshTokenPlainText() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value)));
}
