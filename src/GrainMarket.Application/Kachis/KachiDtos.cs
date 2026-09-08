using GrainMarket.Domain.Enums;

namespace GrainMarket.Application.Kachis;

public record KachiDeductionLineDto(int DeductionRuleId, string Name, string NameUrdu, decimal Amount, string? VehicleNumber);

public record KachiDto(
    int Id, string InvoiceNo, DateTime Date, int SeasonId, string SeasonName,
    int FarmerId, string FarmerName, int ProductId, string ProductName,
    decimal? ManQty, decimal? KiloQty, decimal? GramQty, decimal? BoriQty, decimal NetWeightKg,
    decimal? RatePerUnit, decimal GrossAmount, decimal TotalDeductions, decimal Total,
    InvoiceStatus Status, int? ConvertedToPakkiId, string? Notes,
    List<KachiDeductionLineDto> DeductionLines);

public record CreateKachiRequest(
    DateTime Date, int SeasonId, int FarmerId, int ProductId,
    decimal? ManQty, decimal? KiloQty, decimal? GramQty, decimal? BoriQty,
    decimal? RatePerUnit, string? VehicleNumber, string? Notes);

public record UpdateKachiRequest(
    DateTime Date, int SeasonId, int FarmerId, int ProductId,
    decimal? ManQty, decimal? KiloQty, decimal? GramQty, decimal? BoriQty,
    decimal? RatePerUnit, string? VehicleNumber, string? Notes);
