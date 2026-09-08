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

        var paidFromAccountId = request.PaidFrom switch
        {
            LedgerPartyRefType.Cash => await GetAccountIdByCodeAsync(DomainConstants.CashAccountCode, ct),
            LedgerPartyRefType.Bank => await GetAccountIdByCodeAsync(DomainConstants.BankAccountCode, ct),
            _ => request.PaidFromAccountId!.Value
        };

        var expense = new Expense
        {
            ExpenseNo = await _numberGenerator.NextAsync("EX", ct),
            Date = request.Date,
            ExpenseAccountId = request.ExpenseAccountId,
            Amount = request.Amount,
            Description = request.Description,
            PaidFrom = request.PaidFrom,
            PaidFromAccountId = paidFromAccountId
        };

        _db.Expenses.Add(expense);
        await _db.SaveChangesAsync(ct);

        var description = request.Description ?? expense.ExpenseNo;
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
        e.Id, e.ExpenseNo, e.Date, e.ExpenseAccountId, e.ExpenseAccount.Name, e.Amount, e.Description,
        e.PaidFrom, e.PaidFromAccountId, e.PaidFromAccount?.Name);
}
