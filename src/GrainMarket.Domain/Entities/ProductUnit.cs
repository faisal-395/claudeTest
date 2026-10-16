using GrainMarket.Domain.Common;

namespace GrainMarket.Domain.Entities;

/// <summary>The managed list of choices behind Setup > Products' "Unit" field (kg, Litre, Bag,
/// Pack, Pcs, …), replacing the old free-text-with-suggestions input. Deliberately NOT a foreign
/// key on Product: Product.BaseUnit stays the plain string it always was (every read site —
/// Stock, Dashboard, print templates — keeps working unchanged); this table only supplies the
/// dropdown's choices and lets Setup add more without a code change, same role AccountTypeDefinition
/// plays for Chart of Accounts. Distinct from UnitConversion/WeightUnit, which is a fixed
/// Man/Bori/Gram/Kilo-to-kg conversion factor used only by Kachi/Pakki's Grain weighing — this is
/// just a display label for an Input product (pesticide, fertilizer, seed, …).</summary>
public class ProductUnit : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? NameUrdu { get; set; }
    public bool IsActive { get; set; } = true;
}
