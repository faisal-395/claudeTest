using GrainMarket.Domain.Common;
using GrainMarket.Domain.Enums;

namespace GrainMarket.Domain.Entities;

public class Expense : BaseEntity
{
    public string ExpenseNo { get; set; } = string.Empty;
    public DateTime Date { get; set; }

    public int ExpenseAccountId { get; set; }
    public ChartOfAccount ExpenseAccount { get; set; } = null!;

    public decimal Amount { get; set; }
    public string? Description { get; set; }

    public LedgerPartyRefType PaidFrom { get; set; }
    public int? PaidFromAccountId { get; set; }
    public ChartOfAccount? PaidFromAccount { get; set; }
}
