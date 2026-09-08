using GrainMarket.Application.Common.Exceptions;
using GrainMarket.Application.Common.Interfaces;
using GrainMarket.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GrainMarket.Application.Users;

public class UserService : IUserService
{
    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IDateTimeProvider _clock;

    public UserService(IApplicationDbContext db, IPasswordHasher passwordHasher, IDateTimeProvider clock)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _clock = clock;
    }

    public async Task<List<UserDto>> GetAllAsync(CancellationToken ct = default)
    {
        var rows = await _db.Users.Include(u => u.Role).Where(u => !u.IsDeleted).OrderBy(u => u.Username).ToListAsync(ct);
        return rows.Select(ToDto).ToList();
    }

    public async Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken ct = default)
    {
        if (await _db.Users.AnyAsync(u => u.Username == request.Username && !u.IsDeleted, ct))
            throw new InvalidCalculationException($"Username '{request.Username}' is already taken.");
        if (!await _db.Roles.AnyAsync(r => r.Id == request.RoleId && !r.IsDeleted, ct))
            throw new NotFoundException(nameof(Role), request.RoleId);

        var user = new User
        {
            Username = request.Username,
            FullName = request.FullName,
            PasswordHash = _passwordHasher.Hash(request.Password),
            RoleId = request.RoleId,
            IsActive = request.IsActive
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        return ToDto(await _db.Users.Include(u => u.Role).FirstAsync(u => u.Id == user.Id, ct));
    }

    public async Task<UserDto> UpdateAsync(int id, UpdateUserRequest request, CancellationToken ct = default)
    {
        var user = await _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted, ct)
            ?? throw new NotFoundException(nameof(User), id);
        if (!await _db.Roles.AnyAsync(r => r.Id == request.RoleId && !r.IsDeleted, ct))
            throw new NotFoundException(nameof(Role), request.RoleId);

        user.FullName = request.FullName;
        user.RoleId = request.RoleId;
        user.IsActive = request.IsActive;
        user.UpdatedAtUtc = _clock.UtcNow;

        if (!string.IsNullOrWhiteSpace(request.NewPassword))
        {
            user.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        }

        await _db.SaveChangesAsync(ct);
        return ToDto(await _db.Users.Include(u => u.Role).FirstAsync(u => u.Id == id, ct));
    }

    private static UserDto ToDto(User u) => new(u.Id, u.Username, u.FullName, u.RoleId, u.Role.Name, u.IsActive, u.LastLoginAtUtc);
}
