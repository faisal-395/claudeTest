using GrainMarket.Domain.Common;

namespace GrainMarket.Domain.Entities;

/// <summary>What kind of Input product this is (Seed, Fertilizer, Pesticide, …) — only meaningful
/// for a Product whose Category is "Input" (DomainConstants.ProductCategoryInput); a Grain product
/// leaves this null. Kept as a database row (like AccountTypeDefinition) so Setup > Product Types
/// can add more later without a code change, and so Stock can filter/group by it.</summary>
public class ProductType : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? NameUrdu { get; set; }
    public bool IsActive { get; set; } = true;
}
