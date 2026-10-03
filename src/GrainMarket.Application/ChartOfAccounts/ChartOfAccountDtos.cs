namespace GrainMarket.Application.ChartOfAccounts;

public record ChartOfAccountDto(
    int Id, string Code, string Name, string? NameUrdu, int AccountTypeId, string AccountTypeName,
    int? ParentAccountId, bool IsProtected, bool IsActive, decimal CurrentBalance, List<int> AllowedRoleIds);

public record UpsertChartOfAccountRequest(
    string Code, string Name, string? NameUrdu, int AccountTypeId,
    int? ParentAccountId, bool IsProtected, bool IsActive, List<int> AllowedRoleIds);
