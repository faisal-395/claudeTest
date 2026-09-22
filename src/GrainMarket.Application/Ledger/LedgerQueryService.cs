using GrainMarket.Application.Common.Exceptions;
using GrainMarket.Application.Common.Interfaces;
using GrainMarket.Domain.Entities;
using GrainMarket.Domain.Enums;
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

        return new PartyLedgerDto(partyId, party.Name, opening, closing, await ToRowDtosAsync(rows, ct));
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

        return new AccountLedgerDto(accountId, account.Code, account.Name, closing, await ToRowDtosAsync(rows, ct));
    }

    // Payment/Receipt vouchers let the operator type a free-text description that replaces the
    // default "Payment PV-000123" — silently dropping the voucher number the statement/ledger needs
    // to trace the entry back to its voucher. Backfill it here for display so this works for every
    // entry already posted, not just ones created after VoucherService started keeping the number.
    private async Task<List<LedgerRowDto>> ToRowDtosAsync(List<LedgerEntry> rows, CancellationToken ct)
    {
        var voucherIds = rows.Where(r => r.SourceType is LedgerSourceType.Payment or LedgerSourceType.Receipt)
            .Select(r => r.SourceId).Distinct().ToList();

        var voucherNumbers = voucherIds.Count == 0
            ? new Dictionary<int, string>()
            : await _db.Vouchers.Where(v => voucherIds.Contains(v.Id)).ToDictionaryAsync(v => v.Id, v => v.VoucherNo, ct);

        return rows.Select(e =>
        {
            var description = e.Description;
            if (e.SourceType is LedgerSourceType.Payment or LedgerSourceType.Receipt
                && voucherNumbers.TryGetValue(e.SourceId, out var voucherNo)
                && !(description ?? string.Empty).Contains(voucherNo))
            {
                description = string.IsNullOrWhiteSpace(description) ? voucherNo : $"{description} ({voucherNo})";
            }
            return new LedgerRowDto(e.Id, e.Date, e.Debit, e.Credit, e.RunningBalance, e.SourceType, e.SourceId, description);
        }).ToList();
    }
}
