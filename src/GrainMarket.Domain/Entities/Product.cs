using GrainMarket.Domain.Common;

namespace GrainMarket.Domain.Entities;

public class Product : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? NameUrdu { get; set; }
    public string? Category { get; set; }

    /// <summary>Display/counting unit — always "kg" for a Grain product (Kachi/Pakki weigh in
    /// kilograms). An Input product (pesticide, fertilizer, seed) can use any short unit label
    /// (Litre, Bag, Pack, Pcs, …); it's purely descriptive and doesn't affect any calculation.</summary>
    public string BaseUnit { get; set; } = "kg";

    public decimal DefaultRate { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>Fixed selling price used to auto-fill Sale Invoice lines. Takes precedence over
    /// SaleMarkupPercent when both are set; null means the operator must still type a price.</summary>
    public decimal? SalePrice { get; set; }

    /// <summary>Percentage markup over the FIFO-weighted purchase cost, used to auto-fill Sale
    /// Invoice lines when SalePrice isn't set.</summary>
    public decimal? SaleMarkupPercent { get; set; }
}
