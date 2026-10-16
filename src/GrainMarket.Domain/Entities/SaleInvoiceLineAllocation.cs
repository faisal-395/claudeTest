using GrainMarket.Domain.Common;

namespace GrainMarket.Domain.Entities;

/// <summary>Records which Purchase lot(s) a Sale Invoice line's quantity was drawn from under FIFO —
/// a line can span more than one lot when the oldest lot doesn't cover the full quantity. UnitCost is
/// copied from the PurchaseLine at allocation time so historical costing never drifts if a later
/// purchase changes the product's rate.</summary>
public class SaleInvoiceLineAllocation : BaseEntity
{
    public int SaleInvoiceLineId { get; set; }
    public SaleInvoiceLine SaleInvoiceLine { get; set; } = null!;

    public int PurchaseLineId { get; set; }
    public PurchaseLine PurchaseLine { get; set; } = null!;

    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
}
