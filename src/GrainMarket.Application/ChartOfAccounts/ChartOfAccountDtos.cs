using GrainMarket.Domain.Enums;

namespace GrainMarket.Application.ChartOfAccounts;

public record ChartOfAccountDto(
    int Id, string Code, string Name, string? NameUrdu, AccountType AccountType,
    int? ParentAccountId, bool IsProtected, bool IsActive, decimal CurrentBalance, List<int> AllowedRoleIds);

public record UpsertChartOfAccountRequest(
    string Code, string Name, string? NameUrdu, AccountType AccountType,
    int? ParentAccountId, bool IsProtected, bool IsActive, List<int> AllowedRoleIds);
