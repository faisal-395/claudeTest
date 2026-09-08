namespace GrainMarket.Application.DualInvoice;

/// <summary>
/// "Dual Invoice" (undefined in the legacy app's menu list) is implemented here as a single-screen
/// shortcut that raises a Kachi and immediately converts it to a Pakki in one submit — for the
/// common case where the farmer, buyer and rate are all known up front and there is no need to
/// keep the produce in provisional/unpriced Kachi state.
/// </summary>
public record CreateDualInvoiceRequest(
    DateTime Date, int SeasonId, int FarmerId, int BuyerId, int ProductId,
    decimal? ManQty, decimal? KiloQty, decimal? GramQty, decimal? BoriQty,
    decimal RatePerUnit, string? VehicleNumber, string? Notes);

public record DualInvoiceResultDto(int KachiId, string KachiInvoiceNo, int PakkiId, string PakkiInvoiceNo, decimal NetPayableToFarmer);
