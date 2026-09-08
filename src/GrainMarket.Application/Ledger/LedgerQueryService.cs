using GrainMarket.Application.Common.Exceptions;
using GrainMarket.Application.Common.Interfaces;
using GrainMarket.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GrainMarket.Application.Ledger;

public class LedgerQueryService : ILedgerQueryService
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public LedgerQueryService(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<PartyLedgerDto> GetPartyLedgerAsync(int partyId, DateTime? from, DateTime? to, CancellationToken ct = default)
    {
        var party = await _db.Parties.FirstOrDefaultAsync(p => p.Id == partyId && !p.IsDeleted, ct)
            ?? throw new NotFoundException(nameof(Party), partyId);

        var query = _db.LedgerEntries.Where(e => e.PartyId == partyId && !e.IsDeleted);
        if (from is not null) query = query.Where(e => e.Date >= from);
        if (to is not null) query = query.Where(e => e.Date <= to);

        var rows = await query.OrderBy(e => e.Date).ThenBy(e => e.Id).ToListAsync(ct);

        var opening = from is null
            ? 0m
            : await _db.LedgerEntries.Where(e => e.PartyId == partyId && e.Date < from && !e.IsDeleted)
                .OrderByDescending(e => e.Date).ThenByDescending(e => e.Id)
                .Select(e => e.RunningBalance).FirstOrDefaultAsync(ct);

        var closing = rows.Count > 0 ? rows[^1].RunningBalance : opening;

        return new PartyLedgerDto(partyId, party.Name, opening, closing, rows.Select(ToRowDto).ToList());
    }

    public async Task<AccountLedgerDto> GetAccountLedgerAsync(int accountId, DateTime? from, DateTime? to, CancellationToken ct = default)
    {
        var account = await _db.ChartOfAccounts.Include(a => a.AllowedRoles)
            .FirstOrDefaultAsync(a => a.Id == accountId && !a.IsDeleted, ct)
            ?? throw new NotFoundException(nameof(ChartOfAccount), accountId);

        if (account.IsProtected && !_currentUser.HasAllowedAccountRole(account.AllowedRoles.Select(r => r.RoleId)))
        {
            throw new ForbiddenAccessException("You do not have permission to view this account.");
        }

        var query = _db.LedgerEntries.Where(e => e.ChartOfAccountId == accountId && !e.IsDeleted);
        if (from is not null) query = query.Where(e => e.Date >= from);
        if (to is not null) query = query.Where(e => e.Date <= to);

        var rows = await query.OrderBy(e => e.Date).ThenBy(e => e.Id).ToListAsync(ct);
        var closing = rows.Count > 0 ? rows[^1].RunningBalance : 0m;

        return new AccountLedgerDto(accountId, account.Code, account.Name, closing, rows.Select(ToRowDto).ToList());
    }

    private static LedgerRowDto ToRowDto(LedgerEntry e) => new(e.Id, e.Date, e.Debit, e.Credit, e.RunningBalance, e.SourceType, e.SourceId, e.Description);
}
