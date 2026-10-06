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

    // Read-only preview — never consumes a number. The real one is only generated, and the
    // sequence only advances, inside CreatePaymentOrReceiptAsync when the voucher actually saves;
    // simply opening the Payment/Receipt screen (or abandoning it unsaved) no longer burns one.
    public async Task<NextVoucherNoDto> PeekNextVoucherNoAsync(VoucherType type, CancellationToken ct = default) =>
        new(await _numberGenerator.PeekNextAsync(PrefixFor(type), 0, ct));

    public async Task<VoucherDto> CreatePaymentOrReceiptAsync(CreatePaymentOrReceiptRequest request, CancellationToken ct = default)
    {
        await EnsureRefValidAsync(request.FromType, request.FromPartyId, request.FromAccountId, ct);
        await EnsureRefValidAsync(request.ToType, request.ToPartyId, request.ToAccountId, ct);

        var voucher = new Voucher
        {
            VoucherType = request.VoucherType,
            VoucherNo = await _numberGenerator.NextAsync(PrefixFor(request.VoucherType), 0, ct),
            Date = request.Date,
            SeasonId = request.SeasonId,
            Amount = request.Amount,
            RefNo = request.RefNo,
            Description = request.Description,
            CreditDescription = request.CreditDescription,
            DebitDescription = request.DebitDescription,
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

        var sourceType = SourceTypeFor(request.VoucherType);
        // The voucher number itself is surfaced separately (LedgerRowDto.ReferenceNo, resolved from
        // SourceId), so the description here is just the human-readable text.
        var description = request.Description ?? request.VoucherType.ToString();

        // Convention: Dr the "To" side, Cr the "From" side, for Payment, Receipt, and Journal alike.
        await PostRefAsync(request.ToType, voucher.ToPartyId, voucher.ToAccountId, voucher.Date, request.Amount, 0m, sourceType, voucher.Id, request.DebitDescription ?? description, ct);
        await PostRefAsync(request.FromType, voucher.FromPartyId, voucher.FromAccountId, voucher.Date, 0m, request.Amount, sourceType, voucher.Id, request.CreditDescription ?? description, ct);

        await _db.SaveChangesAsync(ct);
        return await GetByIdAsync(voucher.Id, ct);
    }

    public async Task<VoucherDto> UpdatePaymentOrReceiptAsync(int id, UpdatePaymentOrReceiptRequest request, CancellationToken ct = default)
    {
        var voucher = await LoadAsync(id, ct);
        if (voucher.VoucherType is not (VoucherType.Payment or VoucherType.Receipt or VoucherType.Journal) || voucher.FromType is null)
            throw new InvalidCalculationException("Only a Payment, Receipt, or Journal voucher raised with a From/To party or account can be edited here.");
        if (voucher.IsCancelled)
            throw new InvalidCalculationException("A cancelled voucher cannot be edited.");

        await EnsureRefValidAsync(request.FromType, request.FromPartyId, request.FromAccountId, ct);
        await EnsureRefValidAsync(request.ToType, request.ToPartyId, request.ToAccountId, ct);

        var sourceType = SourceTypeFor(voucher.VoucherType);
        var reversalReason = $"Reversal: {voucher.VoucherNo} edited";

        // Reverse the existing postings before anything on the voucher is mutated — mirrors
        // KachiService/PakkiService.UpdateAsync (reverse what was posted, then post fresh entries
        // for the new terms below). Never mutates or deletes the original LedgerEntry rows. Dated
        // with the voucher's own (pre-mutation) business Date, not _clock.UtcNow: LedgerQueryService
        // and LedgerPostingService's "find the latest balance" lookup both sort primarily by Date,
        // so a reversal dated with the real wall-clock time (which carries a time-of-day, unlike the
        // midnight-only business Date on every normal entry) would sort as "later" than same-day
        // entries that were actually posted after it — scrambling both the displayed order and the
        // running balance chain on any same-day edit.
        await PostRefAsync(voucher.ToType!.Value, voucher.ToPartyId, voucher.ToAccountId, voucher.Date, 0m, voucher.Amount, sourceType, voucher.Id, reversalReason, ct);
        await PostRefAsync(voucher.FromType!.Value, voucher.FromPartyId, voucher.FromAccountId, voucher.Date, voucher.Amount, 0m, sourceType, voucher.Id, reversalReason, ct);

        voucher.Date = request.Date;
        voucher.SeasonId = request.SeasonId;
        voucher.Amount = request.Amount;
        voucher.RefNo = request.RefNo;
        voucher.Description = request.Description;
        voucher.CreditDescription = request.CreditDescription;
        voucher.DebitDescription = request.DebitDescription;
        voucher.FromType = request.FromType;
        voucher.FromPartyId = request.FromType == LedgerPartyRefType.Party ? request.FromPartyId : null;
        voucher.FromAccountId = request.FromType is LedgerPartyRefType.Account or LedgerPartyRefType.Cash or LedgerPartyRefType.Bank
            ? await ResolveAccountIdAsync(request.FromType, request.FromAccountId, ct) : null;
        voucher.ToType = request.ToType;
        voucher.ToPartyId = request.ToType == LedgerPartyRefType.Party ? request.ToPartyId : null;
        voucher.ToAccountId = request.ToType is LedgerPartyRefType.Account or LedgerPartyRefType.Cash or LedgerPartyRefType.Bank
            ? await ResolveAccountIdAsync(request.ToType, request.ToAccountId, ct) : null;
        voucher.UpdatedAtUtc = _clock.UtcNow;

        await _db.SaveChangesAsync(ct);

        var description = request.Description ?? voucher.VoucherType.ToString();
        await PostRefAsync(request.ToType, voucher.ToPartyId, voucher.ToAccountId, voucher.Date, request.Amount, 0m, sourceType, voucher.Id, request.DebitDescription ?? description, ct);
        await PostRefAsync(request.FromType, voucher.FromPartyId, voucher.FromAccountId, voucher.Date, 0m, request.Amount, sourceType, voucher.Id, request.CreditDescription ?? description, ct);

        await _db.SaveChangesAsync(ct);
        return await GetByIdAsync(id, ct);
    }

    public async Task CancelAsync(int id, CancellationToken ct = default)
    {
        var voucher = await LoadAsync(id, ct);
        if (voucher.IsCancelled) return;

        voucher.IsCancelled = true;
        voucher.UpdatedAtUtc = _clock.UtcNow;

        var reason = $"Reversal: {voucher.VoucherNo} cancelled";

        // A Journal voucher raised before General Voucher gained party/account picking via
        // FromType/ToType is the only case still carrying DebitAccountId/CreditAccountId instead —
        // reverse that pair directly. Every other voucher (Payment, Receipt, and a Journal entry
        // raised the new way) goes through the shared FromType/ToType path below.
        if (voucher.VoucherType == VoucherType.Journal && voucher.FromType is null)
        {
            await _ledger.PostAccountEntryAsync(voucher.DebitAccountId!.Value, voucher.Date, 0, voucher.Amount, LedgerSourceType.Journal, voucher.Id, reason, ct);
            await _ledger.PostAccountEntryAsync(voucher.CreditAccountId!.Value, voucher.Date, voucher.Amount, 0, LedgerSourceType.Journal, voucher.Id, reason, ct);
        }
        else
        {
            var sourceType = SourceTypeFor(voucher.VoucherType);
            await PostRefAsync(voucher.ToType!.Value, voucher.ToPartyId, voucher.ToAccountId, voucher.Date, 0m, voucher.Amount, sourceType, voucher.Id, reason, ct);
            await PostRefAsync(voucher.FromType!.Value, voucher.FromPartyId, voucher.FromAccountId, voucher.Date, voucher.Amount, 0m, sourceType, voucher.Id, reason, ct);
        }

        await _db.SaveChangesAsync(ct);
    }

    private static string PrefixFor(VoucherType type) => type switch
    {
        VoucherType.Payment => "PV",
        VoucherType.Receipt => "RV",
        VoucherType.Journal => "JV",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown voucher type.")
    };

    private static LedgerSourceType SourceTypeFor(VoucherType type) => type switch
    {
        VoucherType.Payment => LedgerSourceType.Payment,
        VoucherType.Receipt => LedgerSourceType.Receipt,
        VoucherType.Journal => LedgerSourceType.Journal,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown voucher type.")
    };

    // Cash and Bank are no longer a single fixed account each — a site can set up several (e.g.
    // "Cash in Hand" and "Dasti Cash", or any number of bank accounts) as children of the seeded
    // root account, and the client lets the user pick which one. explicitAccountId is that choice;
    // the code-based lookup is only a fallback for requests that don't supply one.
    private async Task<int?> ResolveAccountIdAsync(LedgerPartyRefType type, int? explicitAccountId, CancellationToken ct)
    {
        return type switch
        {
            LedgerPartyRefType.Cash => explicitAccountId ?? await GetAccountIdByCodeAsync(DomainConstants.CashAccountCode, ct),
            LedgerPartyRefType.Bank => explicitAccountId ?? await GetAccountIdByCodeAsync(DomainConstants.BankAccountCode, ct),
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
        else if (type is LedgerPartyRefType.Cash or LedgerPartyRefType.Bank && accountId is not null)
        {
            // Cash/Bank may omit the account (falls back to the seeded root account in
            // ResolveAccountIdAsync), but a specific choice must be a real, non-deleted account.
            if (!await _db.ChartOfAccounts.AnyAsync(a => a.Id == accountId && !a.IsDeleted, ct))
                throw new NotFoundException(nameof(ChartOfAccount), accountId.Value);
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
