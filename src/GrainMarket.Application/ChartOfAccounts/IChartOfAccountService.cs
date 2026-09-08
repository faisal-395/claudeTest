namespace GrainMarket.Application.ChartOfAccounts;

public interface IChartOfAccountService
{
    /// <summary>
    /// Returns accounts visible to the current caller. Any account with IsProtected = true is
    /// filtered out unless the caller's role is in that account's AllowedRoleIds. This is the
    /// single choke point every dropdown, the Ledger screen, and every report must go through —
    /// there is no other code path that reads chart-of-accounts rows.
    /// </summary>
    Task<List<ChartOfAccountDto>> GetVisibleAsync(bool includeInactive = false, CancellationToken ct = default);
    Task<ChartOfAccountDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<ChartOfAccountDto> CreateAsync(UpsertChartOfAccountRequest request, CancellationToken ct = default);
    Task<ChartOfAccountDto> UpdateAsync(int id, UpsertChartOfAccountRequest request, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
