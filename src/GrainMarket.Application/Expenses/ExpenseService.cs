using GrainMarket.Application.Common;
using GrainMarket.Application.Common.Exceptions;
using GrainMarket.Application.Common.Interfaces;
using GrainMarket.Domain.Entities;
using GrainMarket.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GrainMarket.Application.Expenses;

public class ExpenseService : IExpenseService
{
    private readonly IApplicationDbContext _db;
    private readonly ILedgerPostingService _ledger;
    private readonly IInvoiceNumberGenerator _numberGenerator;

    public ExpenseService(IApplicationDbContext db, ILedgerPostingService ledger, IInvoiceNumberGenerator numberGenerator)
    {
        _db = db;
        _ledger = ledger;
        _numberGenerator = numberGenerator;
    }

    public async Task<NextExpenseNoDto> PeekNextExpenseNoAsync(CancellationToken ct = default) =>
        new(await _numberGenerator.PeekNextAsync("EX", 0, ct));

    public async Task<List<ExpenseDto>> GetAllAsync(CancellationToken ct = default)
    {
        var rows = await _db.Expenses.Include(e => e.ExpenseAccount).Include(e => e.PaidFromAccount)
            .Where(e => !e.IsDeleted).OrderByDescending(e => e.Date).ThenByDescending(e => e.Id).ToListAsync(ct);
        return rows.Select(ToDto).ToList();
    }

    public async Task<ExpenseDto> CreateAsync(CreateExpenseRequest request, CancellationToken ct = default)
    {
        if (!await _db.ChartOfAccounts.AnyAsync(a => a.Id == request.ExpenseAccountId && !a.IsDeleted, ct))
            throw new NotFoundException(nameof(ChartOfAccount), request.ExpenseAccountId);

        // Cash/Bank are no longer a single fixed account each (see VoucherService's identical
        // comment) — request.PaidFromAccountId carries the specific one the client picked, when
        // it picked one; only fall back to the seeded code-based account if it didn't.
        if (request.PaidFromAccountId is not null
            && !await _db.ChartOfAccounts.AnyAsync(a => a.Id == request.PaidFromAccountId && !a.IsDeleted, ct))
        {
            throw new NotFoundException(nameof(ChartOfAccount), request.PaidFromAccountId.Value);
        }

        var paidFromAccountId = request.PaidFrom switch
        {
            LedgerPartyRefType.Cash => request.PaidFromAccountId ?? await GetAccountIdByCodeAsync(DomainConstants.CashAccountCode, ct),
            LedgerPartyRefType.Bank => request.PaidFromAccountId ?? await GetAccountIdByCodeAsync(DomainConstants.BankAccountCode, ct),
            _ => request.PaidFromAccountId!.Value
        };

        var expense = new Expense
        {
            ExpenseNo = await _numberGenerator.NextAsync("EX", ct),
            Date = request.Date,
            ExpenseAccountId = request.ExpenseAccountId,
            Amount = request.Amount,
            Description = request.Description,
            RefNo = request.RefNo,
            PaidFrom = request.PaidFrom,
            PaidFromAccountId = paidFromAccountId
        };

        _db.Expenses.Add(expense);
        await _db.SaveChangesAsync(ct);

        var description = request.Description ?? "Expense";
        await _ledger.PostAccountEntryAsync(request.ExpenseAccountId, expense.Date, request.Amount, 0, LedgerSourceType.Expense, expense.Id, description, ct);
        await _ledger.PostAccountEntryAsync(paidFromAccountId, expense.Date, 0, request.Amount, LedgerSourceType.Expense, expense.Id, description, ct);

        await _db.SaveChangesAsync(ct);

        var reloaded = await _db.Expenses.Include(e => e.ExpenseAccount).Include(e => e.PaidFromAccount).FirstAsync(e => e.Id == expense.Id, ct);
        return ToDto(reloaded);
    }

    public async Task<ExpenseDto> UpdateAsync(int id, UpdateExpenseRequest request, CancellationToken ct = default)
    {
        var expense = await _db.Expenses.FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted, ct)
            ?? throw new NotFoundException(nameof(Expense), id);

        if (!await _db.ChartOfAccounts.AnyAsync(a => a.Id == request.ExpenseAccountId && !a.IsDeleted, ct))
            throw new NotFoundException(nameof(ChartOfAccount), request.ExpenseAccountId);

        if (request.PaidFromAccountId is not null
            && !await _db.ChartOfAccounts.AnyAsync(a => a.Id == request.PaidFromAccountId && !a.IsDeleted, ct))
        {
            throw new NotFoundException(nameof(ChartOfAccount), request.PaidFromAccountId.Value);
        }

        var paidFromAccountId = request.PaidFrom switch
        {
            LedgerPartyRefType.Cash => request.PaidFromAccountId ?? await GetAccountIdByCodeAsync(DomainConstants.CashAccountCode, ct),
            LedgerPartyRefType.Bank => request.PaidFromAccountId ?? await GetAccountIdByCodeAsync(DomainConstants.BankAccountCode, ct),
            _ => request.PaidFromAccountId!.Value
        };

        // Reverse the existing postings before anything on the expense is mutated — same
        // reverse-then-repost convention as VoucherService.UpdatePaymentOrReceiptAsync. Dated with
        // the expense's own (pre-mutation) business Date, not "now" — see that method's comment for
        // why a wall-clock timestamp here would scramble same-day balance ordering.
        var reversalReason = $"Reversal: {expense.ExpenseNo} edited";
        await _ledger.PostAccountEntryAsync(expense.ExpenseAccountId, expense.Date, 0, expense.Amount, LedgerSourceType.Expense, expense.Id, reversalReason, ct);
        await _ledger.PostAccountEntryAsync(expense.PaidFromAccountId!.Value, expense.Date, expense.Amount, 0, LedgerSourceType.Expense, expense.Id, reversalReason, ct);

        expense.Date = request.Date;
        expense.ExpenseAccountId = request.ExpenseAccountId;
        expense.Amount = request.Amount;
        expense.Description = request.Description;
        expense.RefNo = request.RefNo;
        expense.PaidFrom = request.PaidFrom;
        expense.PaidFromAccountId = paidFromAccountId;

        await _db.SaveChangesAsync(ct);

        var description = request.Description ?? "Expense";
        await _ledger.PostAccountEntryAsync(request.ExpenseAccountId, expense.Date, request.Amount, 0, LedgerSourceType.Expense, expense.Id, description, ct);
        await _ledger.PostAccountEntryAsync(paidFromAccountId, expense.Date, 0, request.Amount, LedgerSourceType.Expense, expense.Id, description, ct);

        await _db.SaveChangesAsync(ct);

        var reloaded = await _db.Expenses.Include(e => e.ExpenseAccount).Include(e => e.PaidFromAccount).FirstAsync(e => e.Id == expense.Id, ct);
        return ToDto(reloaded);
    }

    private async Task<int> GetAccountIdByCodeAsync(string code, CancellationToken ct)
    {
        var account = await _db.ChartOfAccounts.FirstOrDefaultAsync(a => a.Code == code, ct)
            ?? throw new InvalidCalculationException($"Required chart-of-accounts row with code {code} is missing. Re-run seed data.");
        return account.Id;
    }

    private static ExpenseDto ToDto(Expense e) => new(
        e.Id, e.ExpenseNo, e.Date, e.ExpenseAccountId, e.ExpenseAccount.Name, e.Amount, e.Description, e.RefNo,
        e.PaidFrom, e.PaidFromAccountId, e.PaidFromAccount?.Name);
}
