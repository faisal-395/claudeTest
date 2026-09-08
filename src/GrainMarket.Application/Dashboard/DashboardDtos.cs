namespace GrainMarket.Application.Dashboard;

public record DashboardSummaryDto(
    DateTime Date,
    int TodayKachiCount, decimal TodayKachiWeightKg,
    int TodayPakkiCount, decimal TodayPakkiGrossAmount,
    int TodaySaleInvoiceCount, decimal TodaySaleNetBill,
    int TodayPurchaseCount, decimal TodayPurchaseNetBill,
    decimal CashBalance, decimal BankBalance,
    decimal TotalOutstandingRecovery);
