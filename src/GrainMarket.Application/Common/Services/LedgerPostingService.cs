using GrainMarket.Application.Common.Interfaces;
using GrainMarket.Domain.Entities;
using GrainMarket.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GrainMarket.Application.Common.Services;

public class LedgerPostingService : ILedgerPostingService
{
    private readonly IApplicationDbContext _db;

    // Scoped to one request, same lifetime as the DbContext. Once this instance has posted an entry
    // for a party/account, every later posting for that same key within the same request must chain
    // off that balance — not re-query the database, which can't see an entry this same instance has
    // already Add()-ed but not yet SaveChanges'd. Purchase and Sale Invoice, for example, each post
    // two party entries per request (the transaction itself, then the cash applied against it); the
    // second one used to compute its "previous balance" against the pre-request database state
    // instead of the first entry it had just staged, silently corrupting every RunningBalance from
    // that point on.
    private readonly Dictionary<int, decimal> _partyBalances = new();
    private readonly Dictionary<int, decimal> _accountBalances = new();

    public LedgerPostingService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<decimal> PostPartyEntryAsync(int partyId, DateTime date, decimal debit, decimal credit, LedgerSourceType sourceType, int sourceId, string? description, CancellationToken ct = default)
    {
        if (!_partyBalances.TryGetValue(partyId, out var previous))
        {
            previous = await _db.LedgerEntries
                .Where(e => e.PartyId == partyId)
                .OrderByDescending(e => e.Date).ThenByDescending(e => e.Id)
                .Select(e => e.RunningBalance)
                .FirstOrDefaultAsync(ct);
        }

        var newBalance = LedgerBalanceCalculator.ComputeNewBalance(previous, debit, credit);
        _partyBalances[partyId] = newBalance;

        _db.LedgerEntries.Add(new LedgerEntry
        {
            Date = date,
            PartyId = partyId,
            Debit = debit,
            Credit = credit,
            RunningBalance = newBalance,
            SourceType = sourceType,
            SourceId = sourceId,
            Description = description
        });

        return newBalance;
    }

    public async Task<decimal> PostAccountEntryAsync(int chartOfAccountId, DateTime date, decimal debit, decimal credit, LedgerSourceType sourceType, int sourceId, string? description, CancellationToken ct = default)
    {
        if (!_accountBalances.TryGetValue(chartOfAccountId, out var previous))
        {
            previous = await _db.LedgerEntries
                .Where(e => e.ChartOfAccountId == chartOfAccountId)
                .OrderByDescending(e => e.Date).ThenByDescending(e => e.Id)
                .Select(e => e.RunningBalance)
                .FirstOrDefaultAsync(ct);
        }

        var newBalance = LedgerBalanceCalculator.ComputeNewBalance(previous, debit, credit);
        _accountBalances[chartOfAccountId] = newBalance;

        _db.LedgerEntries.Add(new LedgerEntry
        {
            Date = date,
            ChartOfAccountId = chartOfAccountId,
            Debit = debit,
            Credit = credit,
            RunningBalance = newBalance,
            SourceType = sourceType,
            SourceId = sourceId,
            Description = description
        });

        return newBalance;
    }
}
