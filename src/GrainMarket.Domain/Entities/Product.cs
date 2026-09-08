using GrainMarket.Domain.Common;

namespace GrainMarket.Domain.Entities;

public class Product : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? NameUrdu { get; set; }
    public string? Category { get; set; }

    /// <summary>Internal storage unit for all stock/weight math. Always kilograms.</summary>
    public string BaseUnit { get; set; } = "kg";

    public decimal DefaultRate { get; set; }
    public bool IsActive { get; set; } = true;
}
