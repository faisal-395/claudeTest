using GrainMarket.Domain.Common;
using GrainMarket.Domain.Enums;

namespace GrainMarket.Domain.Entities;

/// <summary>Final sale invoice, raised against a Kachi once the buyer and rate are settled.</summary>
public class Pakki : BaseEntity
{
    public string InvoiceNo { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public int SeasonId { get; set; }
    public Season Season { get; set; } = null!;

    /// <summary>Nullable: a Pakki can in principle be raised standalone; normally set from a converted Kachi.</summary>
    public int? KachiId { get; set; }
    public Kachi? Kachi { get; set; }

    public int BuyerId { get; set; }
    public Party Buyer { get; set; } = null!;

    /// <summary>Payee for NetPayableToFarmer. Copied from Kachi.FarmerId when converting a Kachi;
    /// required and editable when the Pakki is raised standalone (KachiId is null).</summary>
    public int FarmerId { get; set; }
    public Party Farmer { get; set; } = null!;

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public decimal? ManQty { get; set; }
    public decimal? KiloQty { get; set; }
    public decimal? GramQty { get; set; }
    public decimal? BoriQty { get; set; }
    public decimal NetWeightKg { get; set; }

    public decimal RatePerUnit { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal NetPayableToFarmer { get; set; }

    public string? VehicleNumber { get; set; }
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Open;
    public string? Notes { get; set; }

    public ICollection<PakkiDeductionLine> DeductionLines { get; set; } = new List<PakkiDeductionLine>();
}

public class PakkiDeductionLine : BaseEntity
{
    public int PakkiId { get; set; }
    public Pakki Pakki { get; set; } = null!;

    public int DeductionRuleId { get; set; }
    public DeductionRule DeductionRule { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public string NameUrdu { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? VehicleNumber { get; set; }
}
