using GrainMarket.Application.Common;
using GrainMarket.Domain.Enums;

namespace GrainMarket.Application.Kachis;

public record KachiDeductionLineDto(int DeductionRuleId, string Name, string NameUrdu, decimal Amount, string? VehicleNumber, DeductionChargedTo ChargedTo);

public record KachiDto(
    int Id, string InvoiceNo, string? ReceiptNumber, string? BillNumber, DateTime Date, int SeasonId, string SeasonName,
    int FarmerId, string FarmerName, int? BuyerId, string? BuyerName, int ProductId, string ProductName,
    decimal? BhartiKgPerBag, decimal? TotalWeightKg, decimal? BoriQty, decimal NetWeightKg,
    decimal? RatePerUnit, decimal GrossAmount, decimal TotalDeductions, decimal BuyerChargesTotal, decimal Total,
    InvoiceStatus Status, int? ConvertedToPakkiId, string? Notes,
    List<KachiDeductionLineDto> DeductionLines);

public record CreateKachiRequest(
    DateTime Date, int SeasonId, int FarmerId, int? BuyerId, int ProductId,
    decimal? BhartiKgPerBag, decimal? TotalWeightKg,
    decimal? RatePerUnit, string? VehicleNumber, string? Notes, string? BillNumber = null,
    List<DeductionOverrideRequest>? DeductionOverrides = null,
    // Set only by MultiPurchaseService, which generates one InvoiceNo per batch (one buyer,
    // several farmer rows) and passes it to every row so the whole batch shares a single invoice
    // number — left null everywhere else, which self-generates a fresh one as before.
    string? InvoiceNo = null);

public record UpdateKachiRequest(
    DateTime Date, int SeasonId, int FarmerId, int? BuyerId, int ProductId,
    decimal? BhartiKgPerBag, decimal? TotalWeightKg,
    decimal? RatePerUnit, string? VehicleNumber, string? Notes, string? BillNumber = null);
