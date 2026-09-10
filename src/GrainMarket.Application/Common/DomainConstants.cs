namespace GrainMarket.Application.Common;

public static class DomainConstants
{
    /// <summary>Chart-of-accounts code for deductions whose DeductionRule has no explicit IncomeAccountId.
    /// Guarantees every Kachi/Pakki ledger posting balances even before Setup > Format has been
    /// fully configured with per-deduction income accounts. Seeded by Infrastructure.</summary>
    public const string UnallocatedDeductionsAccountCode = "4900";

    /// <summary>Default Dhrn (tare/wastage) deduction applied to a Kachi's Total Weight when no
    /// value is entered — 5kg, per the market's current standard. Applied only when DhrnKg is
    /// null; an explicit 0 (or any other value) always wins. See KachiService.CalculateWeights.</summary>
    public const decimal DefaultDhrnKg = 5m;

    public const string CashAccountCode = "1000";
    public const string BankAccountCode = "1010";
    public const string SalesIncomeAccountCode = "4000";
    public const string PurchaseExpenseAccountCode = "5000";
}
