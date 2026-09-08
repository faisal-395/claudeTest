namespace GrainMarket.Application.Common;

public static class DomainConstants
{
    /// <summary>Chart-of-accounts code for deductions whose DeductionRule has no explicit IncomeAccountId.
    /// Guarantees every Kachi/Pakki ledger posting balances even before Setup > Format has been
    /// fully configured with per-deduction income accounts. Seeded by Infrastructure.</summary>
    public const string UnallocatedDeductionsAccountCode = "4900";

    public const string CashAccountCode = "1000";
    public const string BankAccountCode = "1010";
    public const string SalesIncomeAccountCode = "4000";
    public const string PurchaseExpenseAccountCode = "5000";
}
