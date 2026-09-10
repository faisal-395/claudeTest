namespace GrainMarket.Domain.Enums;

public enum PartyType
{
    Farmer = 1,
    Buyer = 2,
    Agent = 3,
    Other = 4
}

public enum BalanceSide
{
    Debit = 1,
    Credit = 2
}

public enum AccountType
{
    Asset = 1,
    Liability = 2,
    Income = 3,
    Expense = 4,
    Equity = 5
}

public enum DeductionCalculationType
{
    FixedAmount = 1,
    PercentOfGross = 2,
    PerUnitWeight = 3
}

public enum DeductionAppliesTo
{
    Kachi = 1,
    Pakki = 2,
    Both = 3
}

/// <summary>Which side of the transaction a deduction rule is charged to. Farmer-charged rules
/// reduce what the farmer receives (the existing, default behavior). Buyer-charged rules are
/// calculated and shown but never reduce the farmer's payable — they're what the buyer owes on
/// top, informational only for now (no ledger posting).</summary>
public enum DeductionChargedTo
{
    Farmer = 1,
    Buyer = 2
}

public enum WeightUnit
{
    Kilo = 1,
    Gram = 2,
    Man = 3,
    Bori = 4,

    /// <summary>A traditional sub-Man weight denomination (5kg by default, configurable under
    /// Setup &gt; Unit Conversions like every other unit) — used to express the remainder of a
    /// Kachi's net weight after whole Mans are extracted, e.g. 52kg = 1 Man + 2 Dhrn + 2 Kg. Purely
    /// a display breakdown (see UnitConversionCalculator.BreakdownIntoManDhrnKg); it never affects
    /// GrossAmount, which is still computed from the continuous net weight.</summary>
    Dhrn = 5
}

public enum InvoiceStatus
{
    Open = 1,
    ConvertedToPakki = 2,
    Cancelled = 3,
    Posted = 4
}

public enum VoucherType
{
    Payment = 1,
    Receipt = 2,
    Journal = 3
}

public enum LedgerPartyRefType
{
    Cash = 1,
    Bank = 2,
    Party = 3,
    Account = 4
}

public enum LedgerSourceType
{
    Kachi = 1,
    Pakki = 2,
    Sale = 3,
    Purchase = 4,
    Payment = 5,
    Receipt = 6,
    Journal = 7,
    Expense = 8,
    OpeningBalance = 9
}

public enum PrintFormat
{
    Thermal = 1,
    A4 = 2
}

public enum PrintLanguage
{
    English = 1,
    Urdu = 2
}
