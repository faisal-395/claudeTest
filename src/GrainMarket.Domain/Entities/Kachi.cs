using GrainMarket.Domain.Common;
using GrainMarket.Domain.Enums;

namespace GrainMarket.Domain.Entities;

/// <summary>Provisional receipt: a farmer's produce is received, weighed and priced, before the
/// sale is finalized (converted to a Pakki) — a deliberate, separate later step. Rate is required
/// at entry (see CreateKachiRequestValidator); the nullable type here is only for historical rows
/// created before that rule existed.</summary>
public class Kachi : BaseEntity
{
    public string InvoiceNo { get; set; } = string.Empty;

    /// <summary>System-generated, read-only identifier assigned at creation (via the same number
    /// generator as InvoiceNo, prefix "KR") — never entered manually. Nullable only for rows
    /// created before this field existed.</summary>
    public string? ReceiptNumber { get; set; }

    public DateTime Date { get; set; }
    public int SeasonId { get; set; }
    public Season Season { get; set; } = null!;

    public int FarmerId { get; set; }
    public Party Farmer { get; set; } = null!;

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    /// <summary>Required at entry (validated, not enforced by the column) — nullable only for
    /// rows created before Buyer became mandatory at Kachi stage.</summary>
    public int? BuyerId { get; set; }
    public Party? Buyer { get; set; }

    // Raw entered weights, as keyed in by the operator (nulls where not used).
    public decimal? ManQty { get; set; }
    public decimal? KiloQty { get; set; }
    public decimal? GramQty { get; set; }
    public decimal? BoriQty { get; set; }

    /// <summary>Computed total weight in the base unit (kg), derived from the raw quantities via UnitConversion. Never recomputed silently after posting.</summary>
    public decimal NetWeightKg { get; set; }

    /// <summary>Required at entry (validated, not enforced by the column) — nullable only for rows
    /// created before rate became mandatory.</summary>
    public decimal? RatePerUnit { get; set; }

    public decimal GrossAmount { get; set; }

    /// <summary>Sum of farmer-charged deduction lines only — this is what actually reduces Total.
    /// Buyer-charged lines (see DeductionRule.ChargedTo) are in DeductionLines but excluded here
    /// and from BuyerChargesTotal instead.</summary>
    public decimal TotalDeductions { get; set; }

    /// <summary>Sum of buyer-charged deduction lines — what the buyer owes on top of the farmer's
    /// price, calculated and shown but not netted into Total and not posted to any ledger.</summary>
    public decimal BuyerChargesTotal { get; set; }

    /// <summary>Farmer's net payable: GrossAmount minus TotalDeductions (farmer-charged lines only).</summary>
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

    /// <summary>Copied from DeductionRule.ChargedTo at posting time, so historical lines keep
    /// reading correctly even if the rule's ChargedTo is changed later.</summary>
    public DeductionChargedTo ChargedTo { get; set; } = DeductionChargedTo.Farmer;
}
