using GrainMarket.Application.Common;
using GrainMarket.Domain.Enums;

namespace GrainMarket.Application.Pakkis;

public record PakkiDeductionLineDto(int DeductionRuleId, string Name, string NameUrdu, decimal Amount, string? VehicleNumber);

public record PakkiDto(
    int Id, string InvoiceNo, string? BillNumber, DateTime Date, int SeasonId, string SeasonName, int? KachiId, string? KachiInvoiceNo,
    int BuyerId, string BuyerName, int FarmerId, string FarmerName, int ProductId, string ProductName,
    decimal? BhartiKgPerBag, decimal? TotalWeightKg, decimal? BoriQty, decimal NetWeightKg,
    decimal RatePerUnit, decimal GrossAmount, decimal TotalDeductions, decimal NetPayableToFarmer,
    string? VehicleNumber, InvoiceStatus Status, string? Notes, List<PakkiDeductionLineDto> DeductionLines);

/// <summary>Creates a Pakki from an existing open Kachi, carrying its farmer/product/weight forward.</summary>
public record CreatePakkiFromKachiRequest(
    int KachiId, DateTime Date, int BuyerId, decimal RatePerUnit, string? VehicleNumber, string? Notes);

/// <summary>Creates a standalone Pakki (no prior Kachi) — e.g. a direct sale settled in one step.</summary>
public record CreateStandalonePakkiRequest(
    DateTime Date, int SeasonId, int BuyerId, int FarmerId, int ProductId,
    decimal? BhartiKgPerBag, decimal? TotalWeightKg,
    decimal RatePerUnit, string? VehicleNumber, string? Notes, string? BillNumber = null,
    List<DeductionOverrideRequest>? DeductionOverrides = null);

/// <summary>Edits an open Pakki's buyer/rate/vehicle/notes. Weight, product, farmer and season stay
/// fixed (they carry the Kachi's identity forward, or fix the standalone sale's own identity) —
/// only the commercial terms are editable. Reverses and re-posts the ledger under the hood since
/// this is a posted financial document, not a plain field edit.</summary>
public record UpdatePakkiRequest(int BuyerId, decimal RatePerUnit, string? VehicleNumber, string? Notes);
