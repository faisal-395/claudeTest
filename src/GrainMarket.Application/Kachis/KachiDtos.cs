using GrainMarket.Domain.Enums;

namespace GrainMarket.Application.Kachis;

public record KachiDeductionLineDto(int DeductionRuleId, string Name, string NameUrdu, decimal Amount, string? VehicleNumber, DeductionChargedTo ChargedTo);

public record KachiDto(
    int Id, string InvoiceNo, string? ReceiptNumber, DateTime Date, int SeasonId, string SeasonName,
    int FarmerId, string FarmerName, int? BuyerId, string? BuyerName, int ProductId, string ProductName,
    decimal? BhartiKgPerBag, decimal? TotalWeightKg, decimal? DhrnKg, decimal? BoriQty, decimal NetWeightKg,
    decimal? RatePerUnit, decimal GrossAmount, decimal TotalDeductions, decimal BuyerChargesTotal, decimal Total,
    InvoiceStatus Status, int? ConvertedToPakkiId, string? Notes,
    List<KachiDeductionLineDto> DeductionLines);

public record CreateKachiRequest(
    DateTime Date, int SeasonId, int FarmerId, int? BuyerId, int ProductId,
    decimal? BhartiKgPerBag, decimal? TotalWeightKg, decimal? DhrnKg,
    decimal? RatePerUnit, string? VehicleNumber, string? Notes);

public record UpdateKachiRequest(
    DateTime Date, int SeasonId, int FarmerId, int? BuyerId, int ProductId,
    decimal? BhartiKgPerBag, decimal? TotalWeightKg, decimal? DhrnKg,
    decimal? RatePerUnit, string? VehicleNumber, string? Notes);
