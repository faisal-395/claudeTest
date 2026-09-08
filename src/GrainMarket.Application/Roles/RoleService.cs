using GrainMarket.Application.Common.Exceptions;
using GrainMarket.Application.Common.Interfaces;
using GrainMarket.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GrainMarket.Application.Roles;

public class RoleService : IRoleService
{
    private readonly IApplicationDbContext _db;
    private readonly IDateTimeProvider _clock;

    public RoleService(IApplicationDbContext db, IDateTimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<List<RoleDto>> GetAllAsync(CancellationToken ct = default)
    {
        var roles = await _db.Roles.Include(r => r.Permissions).Where(r => !r.IsDeleted).OrderBy(r => r.Name).ToListAsync(ct);
        return roles.Select(ToDto).ToList();
    }

    public async Task<RoleDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var role = await _db.Roles.Include(r => r.Permissions).FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted, ct)
            ?? throw new NotFoundException(nameof(Role), id);
        return ToDto(role);
    }

    public async Task<RoleDto> CreateAsync(UpsertRoleRequest request, CancellationToken ct = default)
    {
        var role = new Role { Name = request.Name, NameUrdu = request.NameUrdu, IsSystemRole = false };
        foreach (var p in request.Permissions)
        {
            role.Permissions.Add(new RolePermission { Module = p.Module, CanView = p.CanView, CanCreate = p.CanCreate, CanEdit = p.CanEdit, CanDelete = p.CanDelete });
        }
        _db.Roles.Add(role);
        await _db.SaveChangesAsync(ct);
        return ToDto(role);
    }

    public async Task<RoleDto> UpdateAsync(int id, UpsertRoleRequest request, CancellationToken ct = default)
    {
        var role = await _db.Roles.Include(r => r.Permissions).FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted, ct)
            ?? throw new NotFoundException(nameof(Role), id);

        role.Name = request.Name;
        role.NameUrdu = request.NameUrdu;
        role.UpdatedAtUtc = _clock.UtcNow;

        foreach (var existing in role.Permissions.ToList())
        {
            _db.RolePermissions.Remove(existing);
        }
        role.Permissions.Clear();
        foreach (var p in request.Permissions)
        {
            role.Permissions.Add(new RolePermission { Module = p.Module, CanView = p.CanView, CanCreate = p.CanCreate, CanEdit = p.CanEdit, CanDelete = p.CanDelete });
        }

        await _db.SaveChangesAsync(ct);
        return ToDto(role);
    }

    private static RoleDto ToDto(Role r) => new(
        r.Id, r.Name, r.NameUrdu, r.IsSystemRole,
        r.Permissions.Select(p => new RolePermissionDto(p.Module, p.CanView, p.CanCreate, p.CanEdit, p.CanDelete)).ToList());
}
