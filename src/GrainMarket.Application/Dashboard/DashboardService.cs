using GrainMarket.Application.Common;
using GrainMarket.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GrainMarket.Application.Dashboard;

public class DashboardService : IDashboardService
{
    private readonly IApplicationDbContext _db;

    public DashboardService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync(DateTime date, CancellationToken ct = default)
    {
        var day = date.Date;
        var nextDay = day.AddDays(1);

        var kachis = await _db.Kachis.Where(k => k.Date >= day && k.Date < nextDay && !k.IsDeleted).ToListAsync(ct);
        var pakkis = await _db.Pakkis.Where(p => p.Date >= day && p.Date < nextDay && !p.IsDeleted).ToListAsync(ct);
        var sales = await _db.SaleInvoices.Where(s => s.Date >= day && s.Date < nextDay && !s.IsDeleted).ToListAsync(ct);
        var purchases = await _db.Purchases.Where(p => p.Date >= day && p.Date < nextDay && !p.IsDeleted).ToListAsync(ct);

        var cashBalance = await GetAccountBalanceByCodeAsync(DomainConstants.CashAccountCode, ct);
        var bankBalance = await GetAccountBalanceByCodeAsync(DomainConstants.BankAccountCode, ct);

        var outstanding = await _db.LedgerEntries
            .Where(e => e.PartyId != null && !e.IsDeleted)
            .GroupBy(e => e.PartyId!.Value)
            .Select(g => g.OrderByDescending(e => e.Date).ThenByDescending(e => e.Id).First().RunningBalance)
            .Where(b => b > 0)
            .ToListAsync(ct);

        return new DashboardSummaryDto(
            day,
            kachis.Count, kachis.Sum(k => k.NetWeightKg),
            pakkis.Count, pakkis.Sum(p => p.GrossAmount),
            sales.Count, sales.Sum(s => s.NetBill),
            purchases.Count, purchases.Sum(p => p.NetBill),
            cashBalance, bankBalance,
            outstanding.Sum());
    }

    private async Task<decimal> GetAccountBalanceByCodeAsync(string code, CancellationToken ct)
    {
        var account = await _db.ChartOfAccounts.FirstOrDefaultAsync(a => a.Code == code, ct);
        if (account is null) return 0m;

        return await _db.LedgerEntries.Where(e => e.ChartOfAccountId == account.Id)
            .OrderByDescending(e => e.Date).ThenByDescending(e => e.Id)
            .Select(e => e.RunningBalance).FirstOrDefaultAsync(ct);
    }
}
