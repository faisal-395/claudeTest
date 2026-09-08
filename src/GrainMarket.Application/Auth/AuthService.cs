using GrainMarket.Application.Common.Interfaces;
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
        await _db.SaveChangesAsync(ct);

        var token = _jwtTokenService.GenerateToken(user);
        return new LoginResponse(token, user.Id, user.Username, user.FullName, user.Role.Name, _clock.UtcNow.AddHours(8));
    }
}
