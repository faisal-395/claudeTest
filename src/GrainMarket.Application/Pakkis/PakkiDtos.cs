using GrainMarket.Domain.Enums;

namespace GrainMarket.Application.Pakkis;

public record PakkiDeductionLineDto(int DeductionRuleId, string Name, string NameUrdu, decimal Amount, string? VehicleNumber);

public record PakkiDto(
    int Id, string InvoiceNo, DateTime Date, int SeasonId, string SeasonName, int? KachiId, string? KachiInvoiceNo,
    int BuyerId, string BuyerName, int FarmerId, string FarmerName, int ProductId, string ProductName,
    decimal? ManQty, decimal? KiloQty, decimal? GramQty, decimal? BoriQty, decimal NetWeightKg,
    decimal RatePerUnit, decimal GrossAmount, decimal TotalDeductions, decimal NetPayableToFarmer,
    string? VehicleNumber, InvoiceStatus Status, string? Notes, List<PakkiDeductionLineDto> DeductionLines);

/// <summary>Creates a Pakki from an existing open Kachi, carrying its farmer/product/weight forward.</summary>
public record CreatePakkiFromKachiRequest(
    int KachiId, DateTime Date, int BuyerId, decimal RatePerUnit, string? VehicleNumber, string? Notes);

/// <summary>Creates a standalone Pakki (no prior Kachi) — e.g. a direct sale settled in one step.</summary>
public record CreateStandalonePakkiRequest(
    DateTime Date, int SeasonId, int BuyerId, int FarmerId, int ProductId,
    decimal? ManQty, decimal? KiloQty, decimal? GramQty, decimal? BoriQty,
    decimal RatePerUnit, string? VehicleNumber, string? Notes);
