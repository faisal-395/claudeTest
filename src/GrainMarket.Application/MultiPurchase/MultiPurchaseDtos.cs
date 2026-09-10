namespace GrainMarket.Application.MultiPurchase;

/// <summary>
/// One farmer's lot within a multi-farmer purchase.
/// </summary>
public record MultiPurchaseRowRequest(
    int FarmerId, int ProductId,
    decimal? BhartiKgPerBag, decimal? TotalWeightKg,
    decimal? RatePerUnit, string? VehicleNumber, string? Notes);

/// <summary>
/// One buyer purchasing from multiple farmers in a single sitting. Each row becomes its own Kachi
/// (provisional receipt) with the shared buyer already attached — Kachi and Pakki are handled as
/// separate stages with separate, independently configurable deductions (Setup &#8250; Format,
/// AppliesTo = Kachi vs Pakki): this does not post a Pakki or charge Pakki-stage deductions.
/// Finalizing the sale (converting to a Pakki) stays a deliberate, separate step per farmer, same
/// as converting any other Kachi.
/// </summary>
public record CreateMultiPurchaseRequest(DateTime Date, int SeasonId, int BuyerId, List<MultiPurchaseRowRequest> Rows, string? BillNumber = null);

public record MultiPurchaseRowResultDto(int KachiId, string KachiInvoiceNo);

public record MultiPurchaseResultDto(
    int BuyerId, string BuyerName, DateTime Date,
    List<MultiPurchaseRowResultDto> Rows, decimal GrandGross, decimal GrandDeductions, decimal GrandTotal);
