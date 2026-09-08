namespace GrainMarket.Application.Users;

public record UserDto(int Id, string Username, string FullName, int RoleId, string RoleName, bool IsActive, DateTime? LastLoginAtUtc);

public record CreateUserRequest(string Username, string FullName, string Password, int RoleId, bool IsActive);

public record UpdateUserRequest(string FullName, int RoleId, bool IsActive, string? NewPassword);
