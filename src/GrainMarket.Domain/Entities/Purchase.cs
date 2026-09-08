using GrainMarket.Domain.Common;
using GrainMarket.Domain.Enums;

namespace GrainMarket.Domain.Entities;

/// <summary>Outright stock purchase (not commission-based) — separate from the farmer payout flow.</summary>
public class Purchase : BaseEntity
{
    public string InvoiceNo { get; set; } = string.Empty;
    public string? BillNo { get; set; }
    public DateTime Date { get; set; }

    public int SupplierId { get; set; }
    public Party Supplier { get; set; } = null!;

    public decimal TotalBill { get; set; }
    public decimal TotalDiscount { get; set; }
    public decimal NetBill { get; set; }
    public decimal PaidCash { get; set; }

    public PrintFormat PrintFormat { get; set; } = PrintFormat.Thermal;
    public PrintLanguage PrintLanguage { get; set; } = PrintLanguage.English;

    public ICollection<PurchaseLine> Lines { get; set; } = new List<PurchaseLine>();
}

public class PurchaseLine : BaseEntity
{
    public int PurchaseId { get; set; }
    public Purchase Purchase { get; set; } = null!;

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal NetPrice { get; set; }
}
