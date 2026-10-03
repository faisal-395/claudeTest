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

    /// <summary>Product.Category values. Grain products feed Kachi/Pakki and UnitConversions; Input
    /// products (pesticides, seeds, fertilizer) feed Purchase (bought from a Supplier) and Sale
    /// Invoice (sold to a Farmer) — the two catalogs are never shown in each other's pickers.</summary>
    public const string ProductCategoryGrain = "Grain";
    public const string ProductCategoryInput = "Input";

    /// <summary>Dashboard flags a purchase lot once its expiry date is this many days away (or past).</summary>
    public const int ExpiryAlertWindowDays = 15;

    /// <summary>AccountTypeDefinition ids seeded by the AddAccountTypeDefinitions migration, in this
    /// exact order so they land on these ids — matches the old fixed AccountType enum's ordinals,
    /// so existing ChartOfAccount rows carry over unchanged. Setup &gt; Account Types can add more
    /// after these five; only this handful of well-known ids is ever referenced by code.</summary>
    public const int AssetAccountTypeId = 1;
    public const int LiabilityAccountTypeId = 2;
    public const int IncomeAccountTypeId = 3;
    public const int ExpenseAccountTypeId = 4;
    public const int EquityAccountTypeId = 5;
}
