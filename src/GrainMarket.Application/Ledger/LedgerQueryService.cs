using GrainMarket.Application.Common.Exceptions;
using GrainMarket.Application.Common.Interfaces;
using GrainMarket.Domain.Common;
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

        var rows = await query.OrderBy(e => e.Date).ThenBy(e => e.CreatedAtUtc).ThenBy(e => e.Id).ToListAsync(ct);

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

        var rows = await query.OrderBy(e => e.Date).ThenBy(e => e.CreatedAtUtc).ThenBy(e => e.Id).ToListAsync(ct);

        var opening = from is null
            ? 0m
            : await _db.LedgerEntries.Where(e => e.ChartOfAccountId == accountId && e.Date < from && !e.IsDeleted)
                .OrderByDescending(e => e.Date).ThenByDescending(e => e.Id)
                .Select(e => e.RunningBalance).FirstOrDefaultAsync(ct);

        var closing = rows.Count > 0 ? rows[^1].RunningBalance : opening;

        return new AccountLedgerDto(accountId, account.Code, account.Name, opening, closing, await ToRowDtosAsync(rows, ct));
    }

    public async Task<List<DayBookRowDto>> GetDayBookAsync(DateTime date, CancellationToken ct = default)
    {
        var rows = await _db.LedgerEntries
            .Include(e => e.Party).Include(e => e.ChartOfAccount)
            .Where(e => !e.IsDeleted && e.Date.Date == date.Date)
            .OrderBy(e => e.CreatedAtUtc).ThenBy(e => e.Id)
            .ToListAsync(ct);

        var rowDtos = await ToRowDtosAsync(rows, ct);
        return rows.Zip(rowDtos, (e, dto) => new DayBookRowDto(
            dto.Id, dto.Date, e.Party?.Name ?? e.ChartOfAccount?.Name ?? "—", e.PartyId is not null,
            dto.Debit, dto.Credit, dto.SourceType, dto.SourceId, dto.Description, dto.ReferenceNo)).ToList();
    }

    public async Task<PeriodBalancesDto> GetPeriodBalancesAsync(DateTime? from, DateTime? to, CancellationToken ct = default)
    {
        var accounts = await _db.ChartOfAccounts.Include(a => a.AccountType)
            .Where(a => !a.IsDeleted && a.IsActive).ToListAsync(ct);
        var parties = await _db.Parties.Where(p => !p.IsDeleted && p.IsActive).ToListAsync(ct);

        var accountOpening = await LatestAccountBalancesAsync(from, inclusive: false, ct);
        var accountClosing = await LatestAccountBalancesAsync(to, inclusive: true, ct);
        var partyOpening = await LatestPartyBalancesAsync(from, inclusive: false, ct);
        var partyClosing = await LatestPartyBalancesAsync(to, inclusive: true, ct);

        var accountDtos = accounts.Select(a =>
        {
            var opening = accountOpening.GetValueOrDefault(a.Id);
            var closing = accountClosing.TryGetValue(a.Id, out var c) ? c : opening;
            return new AccountBalanceDto(a.Id, a.Code, a.Name, a.NameUrdu, a.AccountType.Name, opening, closing);
        }).ToList();

        var partyDtos = parties.Select(p =>
        {
            var opening = partyOpening.GetValueOrDefault(p.Id);
            var closing = partyClosing.TryGetValue(p.Id, out var c) ? c : opening;
            return new PartyBalanceDto(p.Id, p.Name, p.NameUrdu, p.PartyType, opening, closing);
        }).ToList();

        return new PeriodBalancesDto(accountDtos, partyDtos);
    }

    // "Latest RunningBalance on or before (inclusive) / strictly before (exclusive) a cutoff date"
    // per account/party, for every one of them in a single query — the bulk equivalent of the
    // single-entity "opening balance" lookup GetAccountLedgerAsync/GetPartyLedgerAsync already do.
    // A null cutoff means "no bound" (every entry counts, i.e. "as of right now" for Closing, or
    // "from inception" — no opening offset — for Opening since nothing is ever before it).
    private async Task<Dictionary<int, decimal>> LatestAccountBalancesAsync(DateTime? cutoff, bool inclusive, CancellationToken ct)
    {
        var query = _db.LedgerEntries.Where(e => !e.IsDeleted && e.ChartOfAccountId != null);
        if (cutoff is not null) query = inclusive ? query.Where(e => e.Date <= cutoff) : query.Where(e => e.Date < cutoff);

        var latest = await query.GroupBy(e => e.ChartOfAccountId)
            .Select(g => g.OrderByDescending(e => e.Date).ThenByDescending(e => e.Id).First())
            .ToListAsync(ct);
        return latest.ToDictionary(e => e.ChartOfAccountId!.Value, e => e.RunningBalance);
    }

    private async Task<Dictionary<int, decimal>> LatestPartyBalancesAsync(DateTime? cutoff, bool inclusive, CancellationToken ct)
    {
        var query = _db.LedgerEntries.Where(e => !e.IsDeleted && e.PartyId != null);
        if (cutoff is not null) query = inclusive ? query.Where(e => e.Date <= cutoff) : query.Where(e => e.Date < cutoff);

        var latest = await query.GroupBy(e => e.PartyId)
            .Select(g => g.OrderByDescending(e => e.Date).ThenByDescending(e => e.Id).First())
            .ToListAsync(ct);
        return latest.ToDictionary(e => e.PartyId!.Value, e => e.RunningBalance);
    }

    // Description is free text (or an auto-generated label); ReferenceNo is the actual
    // invoice/voucher number, kept separate so the statement/ledger can show and trace each entry
    // to its source document without depending on what ended up in the description.
    private async Task<List<LedgerRowDto>> ToRowDtosAsync(List<LedgerEntry> rows, CancellationToken ct)
    {
        async Task<Dictionary<int, string>> LookupAsync<TEntity>(LedgerSourceType sourceType, IQueryable<TEntity> source, Func<TEntity, string> numberSelector) where TEntity : BaseEntity
        {
            var ids = rows.Where(r => r.SourceType == sourceType).Select(r => r.SourceId).Distinct().ToList();
            if (ids.Count == 0) return new Dictionary<int, string>();

            var entities = await source.Where(x => ids.Contains(x.Id)).ToListAsync(ct);
            return entities.ToDictionary(x => x.Id, numberSelector);
        }

        var kachiNumbers = await LookupAsync(LedgerSourceType.Kachi, _db.Kachis, k => k.InvoiceNo);
        var pakkiNumbers = await LookupAsync(LedgerSourceType.Pakki, _db.Pakkis, p => p.InvoiceNo);
        var purchaseNumbers = await LookupAsync(LedgerSourceType.Purchase, _db.Purchases, p => p.InvoiceNo);
        var saleNumbers = await LookupAsync(LedgerSourceType.Sale, _db.SaleInvoices, s => s.InvoiceNo);
        var expenseNumbers = await LookupAsync(LedgerSourceType.Expense, _db.Expenses, e => e.ExpenseNo);

        var voucherIds = rows.Where(r => r.SourceType is LedgerSourceType.Payment or LedgerSourceType.Receipt or LedgerSourceType.Journal)
            .Select(r => r.SourceId).Distinct().ToList();
        var voucherNumbers = voucherIds.Count == 0
            ? new Dictionary<int, string>()
            : await _db.Vouchers.Where(v => voucherIds.Contains(v.Id)).ToDictionaryAsync(v => v.Id, v => v.VoucherNo, ct);

        string? ReferenceFor(LedgerEntry e) => e.SourceType switch
        {
            LedgerSourceType.Kachi => kachiNumbers.GetValueOrDefault(e.SourceId),
            LedgerSourceType.Pakki => pakkiNumbers.GetValueOrDefault(e.SourceId),
            LedgerSourceType.Purchase => purchaseNumbers.GetValueOrDefault(e.SourceId),
            LedgerSourceType.Sale => saleNumbers.GetValueOrDefault(e.SourceId),
            LedgerSourceType.Expense => expenseNumbers.GetValueOrDefault(e.SourceId),
            LedgerSourceType.Payment or LedgerSourceType.Receipt or LedgerSourceType.Journal => voucherNumbers.GetValueOrDefault(e.SourceId),
            _ => null
        };

        return rows.Select(e => new LedgerRowDto(e.Id, e.Date, e.CreatedAtUtc, e.Debit, e.Credit, e.RunningBalance, e.SourceType, e.SourceId, e.Description, ReferenceFor(e))).ToList();
    }
}
