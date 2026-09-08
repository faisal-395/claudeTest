using GrainMarket.Domain.Common;
using GrainMarket.Domain.Enums;

namespace GrainMarket.Domain.Entities;

/// <summary>Provisional receipt: a farmer's produce is received and weighed before a final sale rate is agreed.</summary>
public class Kachi : BaseEntity
{
    public string InvoiceNo { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public int SeasonId { get; set; }
    public Season Season { get; set; } = null!;

    public int FarmerId { get; set; }
    public Party Farmer { get; set; } = null!;

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    // Raw entered weights, as keyed in by the operator (nulls where not used).
    public decimal? ManQty { get; set; }
    public decimal? KiloQty { get; set; }
    public decimal? GramQty { get; set; }
    public decimal? BoriQty { get; set; }

    /// <summary>Computed total weight in the base unit (kg), derived from the raw quantities via UnitConversion. Never recomputed silently after posting.</summary>
    public decimal NetWeightKg { get; set; }

    /// <summary>Often unset at Kachi stage; filled in once negotiated.</summary>
    public decimal? RatePerUnit { get; set; }

    public decimal GrossAmount { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal Total { get; set; }

    public InvoiceStatus Status { get; set; } = InvoiceStatus.Open;

    public int? ConvertedToPakkiId { get; set; }
    public Pakki? ConvertedToPakki { get; set; }

    public string? Notes { get; set; }

    public ICollection<KachiDeductionLine> DeductionLines { get; set; } = new List<KachiDeductionLine>();
}

public class KachiDeductionLine : BaseEntity
{
    public int KachiId { get; set; }
    public Kachi Kachi { get; set; } = null!;

    public int DeductionRuleId { get; set; }
    public DeductionRule DeductionRule { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public string NameUrdu { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? VehicleNumber { get; set; }
}
