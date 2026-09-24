using GrainMarket.Domain.Enums;

namespace GrainMarket.Application.Purchases;

public record PurchaseLineDto(int ProductId, string ProductName, decimal Quantity, decimal Price, decimal DiscountPercent, decimal NetPrice, DateTime? ExpiryDate);
public record PurchaseLineRequest(int ProductId, decimal Quantity, decimal Price, decimal DiscountPercent, DateTime? ExpiryDate = null);

// A bare string returned via Ok(...) gets picked up by ASP.NET Core's built-in StringOutputFormatter
// as text/plain instead of JSON, which the client's JSON deserializer then fails to parse — wrapping
// it in a record forces the normal JSON formatter regardless of the request's Accept header.
public record NextPurchaseInvoiceNoDto(string InvoiceNo);

public record PurchaseDto(
    int Id, string InvoiceNo, string? BillNo, DateTime Date, string? Description, int SupplierId, string SupplierName,
    decimal TotalBill, decimal TotalDiscount, decimal NetBill, decimal PaidCash,
    PrintFormat PrintFormat, PrintLanguage PrintLanguage, bool IsCancelled, List<PurchaseLineDto> Lines);

public record CreatePurchaseRequest(
    DateTime Date, string? BillNo, int SupplierId, List<PurchaseLineRequest> Lines,
    decimal PaidCash, PrintFormat PrintFormat, PrintLanguage PrintLanguage,
    string? Description = null,
    // Set when the client already reserved a number via GET api/purchases/next-invoice-no (shown on
    // screen before Save, matching the legacy software) — left null anywhere else, which
    // self-generates a fresh one as before.
    string? InvoiceNo = null);
