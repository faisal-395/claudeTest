using GrainMarket.Domain.Common;
using GrainMarket.Domain.Enums;

namespace GrainMarket.Domain.Entities;

/// <summary>General (retail-style, barcoded) sale — the "New Sale General" screen.</summary>
public class SaleInvoice : BaseEntity
{
    public string InvoiceNo { get; set; } = string.Empty;
    public string? BillNo { get; set; }
    public DateTime Date { get; set; }

    public int CustomerId { get; set; }
    public Party Customer { get; set; } = null!;

    public decimal TotalBill { get; set; }
    public decimal TotalDiscount { get; set; }
    public decimal NetBill { get; set; }
    public decimal ReceivedCash { get; set; }
    public decimal PayCash { get; set; }

    public PrintFormat PrintFormat { get; set; } = PrintFormat.Thermal;
    public PrintLanguage PrintLanguage { get; set; } = PrintLanguage.English;

    public ICollection<SaleInvoiceLine> Lines { get; set; } = new List<SaleInvoiceLine>();
}

public class SaleInvoiceLine : BaseEntity
{
    public int SaleInvoiceId { get; set; }
    public SaleInvoice SaleInvoice { get; set; } = null!;

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal NetPrice { get; set; }
}
