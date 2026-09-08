using GrainMarket.Domain.Enums;

namespace GrainMarket.Application.Expenses;

public record ExpenseDto(
    int Id, string ExpenseNo, DateTime Date, int ExpenseAccountId, string ExpenseAccountName,
    decimal Amount, string? Description, LedgerPartyRefType PaidFrom, int? PaidFromAccountId, string? PaidFromAccountName);

public record CreateExpenseRequest(
    DateTime Date, int ExpenseAccountId, decimal Amount, string? Description,
    LedgerPartyRefType PaidFrom, int? PaidFromAccountId);
