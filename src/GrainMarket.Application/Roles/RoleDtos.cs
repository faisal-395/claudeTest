using GrainMarket.Domain.Entities;

namespace GrainMarket.Application.Roles;

public record RolePermissionDto(ModuleName Module, bool CanView, bool CanCreate, bool CanEdit, bool CanDelete);

public record RoleDto(int Id, string Name, string? NameUrdu, bool IsSystemRole, List<RolePermissionDto> Permissions);

public record UpsertRoleRequest(string Name, string? NameUrdu, List<RolePermissionDto> Permissions);
