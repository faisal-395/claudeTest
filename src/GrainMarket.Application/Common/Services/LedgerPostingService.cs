using GrainMarket.Application.Common.Interfaces;
using GrainMarket.Domain.Entities;
using GrainMarket.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GrainMarket.Application.Common.Services;

public class LedgerPostingService : ILedgerPostingService
{
    private readonly IApplicationDbContext _db;

    public LedgerPostingService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<decimal> PostPartyEntryAsync(int partyId, DateTime date, decimal debit, decimal credit, LedgerSourceType sourceType, int sourceId, string? description, CancellationToken ct = default)
    {
        var previous = await _db.LedgerEntries
            .Where(e => e.PartyId == partyId)
            .OrderByDescending(e => e.Date).ThenByDescending(e => e.Id)
            .Select(e => e.RunningBalance)
            .FirstOrDefaultAsync(ct);

        var newBalance = LedgerBalanceCalculator.ComputeNewBalance(previous, debit, credit);

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
        var previous = await _db.LedgerEntries
            .Where(e => e.ChartOfAccountId == chartOfAccountId)
            .OrderByDescending(e => e.Date).ThenByDescending(e => e.Id)
            .Select(e => e.RunningBalance)
            .FirstOrDefaultAsync(ct);

        var newBalance = LedgerBalanceCalculator.ComputeNewBalance(previous, debit, credit);

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
