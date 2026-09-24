using GrainMarket.Domain.Enums;

namespace GrainMarket.Application.SaleInvoices;

public record SaleInvoiceLineDto(int ProductId, string ProductName, decimal Quantity, decimal Price, decimal DiscountPercent, decimal NetPrice);
public record SaleInvoiceLineRequest(int ProductId, decimal Quantity, decimal Price, decimal DiscountPercent);

// A bare string returned via Ok(...) gets picked up by ASP.NET Core's built-in StringOutputFormatter
// as text/plain instead of JSON, which the client's JSON deserializer then fails to parse — wrapping
// it in a record forces the normal JSON formatter regardless of the request's Accept header.
public record NextSaleInvoiceNoDto(string InvoiceNo);

public record SaleInvoiceDto(
    int Id, string InvoiceNo, string? BillNo, DateTime Date, string? Description, int CustomerId, string CustomerName,
    decimal TotalBill, decimal TotalDiscount, decimal NetBill, decimal ReceivedCash, decimal PayCash,
    PrintFormat PrintFormat, PrintLanguage PrintLanguage, bool IsCancelled, List<SaleInvoiceLineDto> Lines);

public record CreateSaleInvoiceRequest(
    DateTime Date, string? BillNo, int CustomerId, List<SaleInvoiceLineRequest> Lines,
    decimal ReceivedCash, PrintFormat PrintFormat, PrintLanguage PrintLanguage,
    string? Description = null,
    // Set when the client already reserved a number via GET api/sale-invoices/next-invoice-no (shown
    // on screen before Save, matching the legacy software) — left null anywhere else, which
    // self-generates a fresh one as before.
    string? InvoiceNo = null);
