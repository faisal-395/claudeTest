using GrainMarket.Application.Common;
using GrainMarket.Application.Common.Exceptions;
using GrainMarket.Application.Common.Interfaces;
using GrainMarket.Domain.Entities;
using GrainMarket.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GrainMarket.Application.Vouchers;

public class VoucherService : IVoucherService
{
    private readonly IApplicationDbContext _db;
    private readonly ILedgerPostingService _ledger;
    private readonly IInvoiceNumberGenerator _numberGenerator;
    private readonly IDateTimeProvider _clock;

    public VoucherService(IApplicationDbContext db, ILedgerPostingService ledger, IInvoiceNumberGenerator numberGenerator, IDateTimeProvider clock)
    {
        _db = db;
        _ledger = ledger;
        _numberGenerator = numberGenerator;
        _clock = clock;
    }

    public async Task<List<VoucherDto>> GetAllAsync(VoucherType? type = null, int? seasonId = null, CancellationToken ct = default)
    {
        var query = IncludeAll(_db.Vouchers).Where(v => !v.IsDeleted);
        if (type is not null) query = query.Where(v => v.VoucherType == type);
        if (seasonId is not null) query = query.Where(v => v.SeasonId == seasonId);

        var rows = await query.OrderByDescending(v => v.Date).ThenByDescending(v => v.Id).ToListAsync(ct);
        var balances = await GetBalancesAsync(rows, ct);
        return rows.Select(v => ToDto(v, balances)).ToList();
    }

    public async Task<VoucherDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var voucher = await LoadAsync(id, ct);
        var balances = await GetBalancesAsync(new[] { voucher }, ct);
        return ToDto(voucher, balances);
    }

    public async Task<VoucherDto> CreatePaymentOrReceiptAsync(CreatePaymentOrReceiptRequest request, CancellationToken ct = default)
    {
        await EnsureRefValidAsync(request.FromType, request.FromPartyId, request.FromAccountId, ct);
        await EnsureRefValidAsync(request.ToType, request.ToPartyId, request.ToAccountId, ct);

        var prefix = request.VoucherType == VoucherType.Payment ? "PV" : "RV";
        var voucher = new Voucher
        {
            VoucherType = request.VoucherType,
            VoucherNo = await _numberGenerator.NextAsync(prefix, ct),
            Date = request.Date,
            SeasonId = request.SeasonId,
            Amount = request.Amount,
            RefNo = request.RefNo,
            Description = request.Description,
            FromType = request.FromType,
            FromPartyId = request.FromType == LedgerPartyRefType.Party ? request.FromPartyId : null,
            FromAccountId = request.FromType is LedgerPartyRefType.Account or LedgerPartyRefType.Cash or LedgerPartyRefType.Bank
                ? await ResolveAccountIdAsync(request.FromType, request.FromAccountId, ct) : null,
            ToType = request.ToType,
            ToPartyId = request.ToType == LedgerPartyRefType.Party ? request.ToPartyId : null,
            ToAccountId = request.ToType is LedgerPartyRefType.Account or LedgerPartyRefType.Cash or LedgerPartyRefType.Bank
                ? await ResolveAccountIdAsync(request.ToType, request.ToAccountId, ct) : null
        };

        _db.Vouchers.Add(voucher);
        await _db.SaveChangesAsync(ct);

        var sourceType = request.VoucherType == VoucherType.Payment ? LedgerSourceType.Payment : LedgerSourceType.Receipt;
        var description = request.Description ?? $"{request.VoucherType} {voucher.VoucherNo}";

        // Convention: Dr the "To" side, Cr the "From" side, for both Payment and Receipt.
        await PostRefAsync(request.ToType, voucher.ToPartyId, voucher.ToAccountId, voucher.Date, request.Amount, 0m, sourceType, voucher.Id, description, ct);
        await PostRefAsync(request.FromType, voucher.FromPartyId, voucher.FromAccountId, voucher.Date, 0m, request.Amount, sourceType, voucher.Id, description, ct);

        await _db.SaveChangesAsync(ct);
        return await GetByIdAsync(voucher.Id, ct);
    }

    public async Task<VoucherDto> CreateJournalAsync(CreateJournalRequest request, CancellationToken ct = default)
    {
        if (!await _db.ChartOfAccounts.AnyAsync(a => a.Id == request.DebitAccountId && !a.IsDeleted, ct))
            throw new NotFoundException(nameof(ChartOfAccount), request.DebitAccountId);
        if (!await _db.ChartOfAccounts.AnyAsync(a => a.Id == request.CreditAccountId && !a.IsDeleted, ct))
            throw new NotFoundException(nameof(ChartOfAccount), request.CreditAccountId);

        var voucher = new Voucher
        {
            VoucherType = VoucherType.Journal,
            VoucherNo = await _numberGenerator.NextAsync("JV", ct),
            Date = request.Date,
            SeasonId = request.SeasonId,
            Amount = request.Amount,
            RefNo = request.RefNo,
            DebitAccountId = request.DebitAccountId,
            DebitDescription = request.DebitDescription,
            CreditAccountId = request.CreditAccountId,
            CreditDescription = request.CreditDescription
        };

        _db.Vouchers.Add(voucher);
        await _db.SaveChangesAsync(ct);

        await _ledger.PostAccountEntryAsync(request.DebitAccountId, voucher.Date, request.Amount, 0, LedgerSourceType.Journal, voucher.Id, request.DebitDescription ?? voucher.VoucherNo, ct);
        await _ledger.PostAccountEntryAsync(request.CreditAccountId, voucher.Date, 0, request.Amount, LedgerSourceType.Journal, voucher.Id, request.CreditDescription ?? voucher.VoucherNo, ct);

        await _db.SaveChangesAsync(ct);
        return await GetByIdAsync(voucher.Id, ct);
    }

    public async Task CancelAsync(int id, CancellationToken ct = default)
    {
        var voucher = await LoadAsync(id, ct);
        if (voucher.IsCancelled) return;

        voucher.IsCancelled = true;
        voucher.UpdatedAtUtc = _clock.UtcNow;

        var reason = $"Reversal: {voucher.VoucherNo} cancelled";
        if (voucher.VoucherType == VoucherType.Journal)
        {
            await _ledger.PostAccountEntryAsync(voucher.DebitAccountId!.Value, _clock.UtcNow, 0, voucher.Amount, LedgerSourceType.Journal, voucher.Id, reason, ct);
            await _ledger.PostAccountEntryAsync(voucher.CreditAccountId!.Value, _clock.UtcNow, voucher.Amount, 0, LedgerSourceType.Journal, voucher.Id, reason, ct);
        }
        else
        {
            var sourceType = voucher.VoucherType == VoucherType.Payment ? LedgerSourceType.Payment : LedgerSourceType.Receipt;
            await PostRefAsync(voucher.ToType!.Value, voucher.ToPartyId, voucher.ToAccountId, _clock.UtcNow, 0m, voucher.Amount, sourceType, voucher.Id, reason, ct);
            await PostRefAsync(voucher.FromType!.Value, voucher.FromPartyId, voucher.FromAccountId, _clock.UtcNow, voucher.Amount, 0m, sourceType, voucher.Id, reason, ct);
        }

        await _db.SaveChangesAsync(ct);
    }

    private async Task<int?> ResolveAccountIdAsync(LedgerPartyRefType type, int? explicitAccountId, CancellationToken ct)
    {
        return type switch
        {
            LedgerPartyRefType.Cash => await GetAccountIdByCodeAsync(DomainConstants.CashAccountCode, ct),
            LedgerPartyRefType.Bank => await GetAccountIdByCodeAsync(DomainConstants.BankAccountCode, ct),
            LedgerPartyRefType.Account => explicitAccountId,
            _ => null
        };
    }

    private async Task<int> GetAccountIdByCodeAsync(string code, CancellationToken ct)
    {
        var account = await _db.ChartOfAccounts.FirstOrDefaultAsync(a => a.Code == code, ct)
            ?? throw new InvalidCalculationException($"Required chart-of-accounts row with code {code} is missing. Re-run seed data.");
        return account.Id;
    }

    private async Task EnsureRefValidAsync(LedgerPartyRefType type, int? partyId, int? accountId, CancellationToken ct)
    {
        if (type == LedgerPartyRefType.Party)
        {
            if (partyId is null || !await _db.Parties.AnyAsync(p => p.Id == partyId && !p.IsDeleted, ct))
                throw new NotFoundException(nameof(Party), partyId ?? 0);
        }
        else if (type == LedgerPartyRefType.Account)
        {
            if (accountId is null || !await _db.ChartOfAccounts.AnyAsync(a => a.Id == accountId && !a.IsDeleted, ct))
                throw new NotFoundException(nameof(ChartOfAccount), accountId ?? 0);
        }
    }

    private async Task PostRefAsync(LedgerPartyRefType type, int? partyId, int? accountId, DateTime date, decimal debit, decimal credit, LedgerSourceType sourceType, int sourceId, string description, CancellationToken ct)
    {
        if (type == LedgerPartyRefType.Party)
        {
            await _ledger.PostPartyEntryAsync(partyId!.Value, date, debit, credit, sourceType, sourceId, description, ct);
        }
        else
        {
            await _ledger.PostAccountEntryAsync(accountId!.Value, date, debit, credit, sourceType, sourceId, description, ct);
        }
    }

    private static IQueryable<Voucher> IncludeAll(IQueryable<Voucher> query) => query
        .Include(v => v.Season).Include(v => v.FromParty).Include(v => v.FromAccount)
        .Include(v => v.ToParty).Include(v => v.ToAccount).Include(v => v.DebitAccount).Include(v => v.CreditAccount);

    private async Task<Voucher> LoadAsync(int id, CancellationToken ct)
    {
        return await IncludeAll(_db.Vouchers).FirstOrDefaultAsync(v => v.Id == id && !v.IsDeleted, ct)
            ?? throw new NotFoundException(nameof(Voucher), id);
    }

    private async Task<Dictionary<(bool isParty, int id), decimal>> GetBalancesAsync(IEnumerable<Voucher> vouchers, CancellationToken ct)
    {
        var partyIds = new HashSet<int>();
        var accountIds = new HashSet<int>();
        foreach (var v in vouchers)
        {
            if (v.FromPartyId is not null) partyIds.Add(v.FromPartyId.Value);
            if (v.ToPartyId is not null) partyIds.Add(v.ToPartyId.Value);
            if (v.FromAccountId is not null) accountIds.Add(v.FromAccountId.Value);
            if (v.ToAccountId is not null) accountIds.Add(v.ToAccountId.Value);
            if (v.DebitAccountId is not null) accountIds.Add(v.DebitAccountId.Value);
            if (v.CreditAccountId is not null) accountIds.Add(v.CreditAccountId.Value);
        }

        var result = new Dictionary<(bool, int), decimal>();

        if (partyIds.Count > 0)
        {
            var partyIdList = partyIds.ToList();
            var latestParty = await _db.LedgerEntries
                .Where(e => e.PartyId != null && partyIdList.Contains(e.PartyId.Value))
                .GroupBy(e => e.PartyId!.Value)
                .Select(g => new { Id = g.Key, Latest = g.OrderByDescending(e => e.Date).ThenByDescending(e => e.Id).First() })
                .ToListAsync(ct);
            foreach (var x in latestParty) result[(true, x.Id)] = x.Latest.RunningBalance;
        }

        if (accountIds.Count > 0)
        {
            var accountIdList = accountIds.ToList();
            var latestAccount = await _db.LedgerEntries
                .Where(e => e.ChartOfAccountId != null && accountIdList.Contains(e.ChartOfAccountId.Value))
                .GroupBy(e => e.ChartOfAccountId!.Value)
                .Select(g => new { Id = g.Key, Latest = g.OrderByDescending(e => e.Date).ThenByDescending(e => e.Id).First() })
                .ToListAsync(ct);
            foreach (var x in latestAccount) result[(false, x.Id)] = x.Latest.RunningBalance;
        }

        return result;
    }

    private static VoucherDto ToDto(Voucher v, Dictionary<(bool isParty, int id), decimal> balances) => new(
        v.Id, v.VoucherType, v.VoucherNo, v.Date, v.SeasonId, v.Season.Name, v.Amount, v.RefNo, v.Description,
        v.FromType, v.FromPartyId, v.FromParty?.Name, v.FromAccountId, v.FromAccount?.Name,
        v.FromPartyId is not null ? balances.GetValueOrDefault((true, v.FromPartyId.Value)) : (v.FromAccountId is not null ? balances.GetValueOrDefault((false, v.FromAccountId.Value)) : null),
        v.ToType, v.ToPartyId, v.ToParty?.Name, v.ToAccountId, v.ToAccount?.Name,
        v.ToPartyId is not null ? balances.GetValueOrDefault((true, v.ToPartyId.Value)) : (v.ToAccountId is not null ? balances.GetValueOrDefault((false, v.ToAccountId.Value)) : null),
        v.DebitAccountId, v.DebitAccount?.Name, v.DebitDescription,
        v.CreditAccountId, v.CreditAccount?.Name, v.CreditDescription,
        v.IsCancelled);
}
