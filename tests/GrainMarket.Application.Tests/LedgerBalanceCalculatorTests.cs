using GrainMarket.Application.Common.Exceptions;
using GrainMarket.Application.Common.Services;
using Xunit;

namespace GrainMarket.Application.Tests;

public class LedgerBalanceCalculatorTests
{
    [Fact]
    public void ComputeNewBalance_Debit_IncreasesBalance()
    {
        var result = LedgerBalanceCalculator.ComputeNewBalance(previousRunningBalance: 100m, debit: 50m, credit: 0m);
        Assert.Equal(150m, result);
    }

    [Fact]
    public void ComputeNewBalance_Credit_DecreasesBalance()
    {
        var result = LedgerBalanceCalculator.ComputeNewBalance(previousRunningBalance: 100m, debit: 0m, credit: 30m);
        Assert.Equal(70m, result);
    }

    [Fact]
    public void ComputeNewBalance_FromZero_TracksSignCorrectly()
    {
        var result = LedgerBalanceCalculator.ComputeNewBalance(previousRunningBalance: 0m, debit: 0m, credit: 500m);
        Assert.Equal(-500m, result);
    }

    [Fact]
    public void ComputeNewBalance_BothDebitAndCredit_Throws()
    {
        Assert.Throws<InvalidCalculationException>(() =>
            LedgerBalanceCalculator.ComputeNewBalance(0m, 10m, 10m));
    }

    [Fact]
    public void ComputeNewBalance_NegativeDebit_Throws()
    {
        Assert.Throws<InvalidCalculationException>(() =>
            LedgerBalanceCalculator.ComputeNewBalance(0m, -10m, 0m));
    }

    [Fact]
    public void ComputeNewBalance_NegativeCredit_Throws()
    {
        Assert.Throws<InvalidCalculationException>(() =>
            LedgerBalanceCalculator.ComputeNewBalance(0m, 0m, -5m));
    }
}
