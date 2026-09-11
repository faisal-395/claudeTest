namespace GrainMarket.Application.MultiSale;

/// <summary>
/// One farmer's lot within a multi-farmer sale. Weight fields are Man/Kilo/Gram/Bori — Pakki's own
/// weighing model — not Kachi's Bharti model.
/// </summary>
public record MultiSaleRowRequest(
    int FarmerId, int ProductId,
    decimal? ManQty, decimal? KiloQty, decimal? GramQty, decimal? BoriQty,
    decimal? RatePerUnit, string? VehicleNumber, string? Notes);

/// <summary>
/// One vendor/buyer settling with multiple farmers in a single sitting. Each row becomes its own
/// standalone Pakki (final sale invoice) with the shared vendor already attached — mirrors
/// MultiPurchaseService/CreateMultiPurchaseRequest for Kachi, but raises Pakkis via
/// IPakkiService.CreateStandaloneAsync per row instead.
/// </summary>
public record CreateMultiSaleRequest(DateTime Date, int SeasonId, int BuyerId, List<MultiSaleRowRequest> Rows);

public record MultiSaleRowResultDto(int PakkiId, string PakkiInvoiceNo);

public record MultiSaleResultDto(
    int BuyerId, string BuyerName, DateTime Date,
    List<MultiSaleRowResultDto> Rows, decimal GrandGross, decimal GrandDeductions, decimal GrandTotal);
