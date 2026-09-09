using GrainMarket.Domain.Enums;

namespace GrainMarket.Application.Purchases;

public record PurchaseLineDto(int ProductId, string ProductName, decimal Quantity, decimal Price, decimal DiscountPercent, decimal NetPrice);
public record PurchaseLineRequest(int ProductId, decimal Quantity, decimal Price, decimal DiscountPercent);

public record PurchaseDto(
    int Id, string InvoiceNo, string? BillNo, DateTime Date, int SupplierId, string SupplierName,
    decimal TotalBill, decimal TotalDiscount, decimal NetBill, decimal PaidCash,
    PrintFormat PrintFormat, PrintLanguage PrintLanguage, bool IsCancelled, List<PurchaseLineDto> Lines);

public record CreatePurchaseRequest(
    DateTime Date, string? BillNo, int SupplierId, List<PurchaseLineRequest> Lines,
    decimal PaidCash, PrintFormat PrintFormat, PrintLanguage PrintLanguage);
