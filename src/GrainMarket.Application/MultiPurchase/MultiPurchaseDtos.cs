namespace GrainMarket.Application.MultiPurchase;

/// <summary>
/// One farmer's lot within a multi-farmer purchase — same shape as a Dual Invoice row, minus the
/// buyer/date/season, which are shared across every row in the batch.
/// </summary>
public record MultiPurchaseRowRequest(
    int FarmerId, int ProductId,
    decimal? ManQty, decimal? KiloQty, decimal? GramQty, decimal? BoriQty,
    decimal RatePerUnit, string? VehicleNumber, string? Notes);

/// <summary>
/// One buyer purchasing multiple products from multiple farmers in a single sitting — each row
/// still becomes its own Kachi+Pakki (every farmer is paid individually against their own net
/// weight and deductions, same as always), but raised together in one submit and printed together
/// as one consolidated receipt.
/// </summary>
public record CreateMultiPurchaseRequest(DateTime Date, int SeasonId, int BuyerId, List<MultiPurchaseRowRequest> Rows);

/// <summary>Every field the consolidated print template needs for one row — deliberately just the
/// created Pakki's id plus its invoice number; the print page fetches the full PakkiDto (name,
/// deduction lines, etc.) the same way the existing Pakki print page already does.</summary>
public record MultiPurchaseRowResultDto(int PakkiId, string PakkiInvoiceNo);

public record MultiPurchaseResultDto(
    int BuyerId, string BuyerName, DateTime Date,
    List<MultiPurchaseRowResultDto> Rows, decimal GrandGross, decimal GrandDeductions, decimal GrandNetPayable);
