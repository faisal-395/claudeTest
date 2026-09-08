namespace GrainMarket.Application.Roles;

public interface IRoleService
{
    Task<List<RoleDto>> GetAllAsync(CancellationToken ct = default);
    Task<RoleDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<RoleDto> CreateAsync(UpsertRoleRequest request, CancellationToken ct = default);
    Task<RoleDto> UpdateAsync(int id, UpsertRoleRequest request, CancellationToken ct = default);
}
