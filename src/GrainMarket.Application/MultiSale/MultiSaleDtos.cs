namespace GrainMarket.Application.MultiSale;

/// <summary>
/// One farmer's lot within a multi-farmer sale. Weight fields are Bharti/TotalWeight — Pakki's
/// weighing model, matching Kachi's own MultiPurchaseRowRequest shape exactly (Bori is computed,
/// never entered).
/// </summary>
public record MultiSaleRowRequest(
    int FarmerId, int ProductId,
    decimal? BhartiKgPerBag, decimal? TotalWeightKg,
    decimal? RatePerUnit, string? VehicleNumber, string? Notes);

/// <summary>
/// One buyer settling with multiple farmers/vendors in a single sitting. Each row becomes its own
/// standalone Pakki (final sale invoice) with the shared buyer already attached — mirrors
/// MultiPurchaseService/CreateMultiPurchaseRequest for Kachi, but raises Pakkis via
/// IPakkiService.CreateStandaloneAsync per row instead.
/// </summary>
public record CreateMultiSaleRequest(DateTime Date, int SeasonId, int BuyerId, List<MultiSaleRowRequest> Rows, string? BillNumber = null);

public record MultiSaleRowResultDto(int PakkiId, string PakkiInvoiceNo);

public record MultiSaleResultDto(
    int BuyerId, string BuyerName, DateTime Date,
    List<MultiSaleRowResultDto> Rows, decimal GrandGross, decimal GrandDeductions, decimal GrandTotal);
