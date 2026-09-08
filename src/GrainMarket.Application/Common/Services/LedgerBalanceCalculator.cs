namespace GrainMarket.Application.Common.Services;

/// <summary>
/// Pure running-balance arithmetic for LedgerEntry rows. Convention: RunningBalance accumulates
/// as (previous + debit - credit) — a positive balance is a net debit position for that
/// party/account, a negative balance is a net credit position. Pure/stateless: safe to unit test.
/// </summary>
public static class LedgerBalanceCalculator
{
    public static decimal ComputeNewBalance(decimal previousRunningBalance, decimal debit, decimal credit)
    {
        if (debit < 0 || credit < 0)
        {
            throw new Common.Exceptions.InvalidCalculationException("Debit and credit amounts must not be negative.");
        }

        if (debit > 0 && credit > 0)
        {
            throw new Common.Exceptions.InvalidCalculationException("A single ledger row cannot carry both a debit and a credit amount.");
        }

        return previousRunningBalance + debit - credit;
    }
}
