using GrainMarket.Domain.Common;
using GrainMarket.Domain.Enums;

namespace GrainMarket.Domain.Entities;

/// <summary>
/// Configurable conversion factor from a local weight unit (Man/Bori/Gram/Kilo) to the base
/// storage unit (kg). A row with ProductId = null is the global default for that unit; a row
/// with ProductId set overrides it for that product only (e.g. a rice Bori vs. a wheat Bori).
/// </summary>
public class UnitConversion : BaseEntity
{
    public WeightUnit Unit { get; set; }
    public decimal FactorToKg { get; set; }
    public int? ProductId { get; set; }
    public Product? Product { get; set; }
    public bool IsActive { get; set; } = true;
}
