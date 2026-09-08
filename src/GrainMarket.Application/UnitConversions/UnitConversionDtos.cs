using GrainMarket.Domain.Enums;

namespace GrainMarket.Application.UnitConversions;

public record UnitConversionDto(int Id, WeightUnit Unit, decimal FactorToKg, int? ProductId, string? ProductName, bool IsActive);

public record UpsertUnitConversionRequest(WeightUnit Unit, decimal FactorToKg, int? ProductId, bool IsActive);
