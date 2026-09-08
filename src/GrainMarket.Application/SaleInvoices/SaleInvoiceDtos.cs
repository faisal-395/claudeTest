using GrainMarket.Domain.Enums;

namespace GrainMarket.Application.SaleInvoices;

public record SaleInvoiceLineDto(int ProductId, string ProductName, decimal Quantity, decimal Price, decimal DiscountPercent, decimal NetPrice);
public record SaleInvoiceLineRequest(int ProductId, decimal Quantity, decimal Price, decimal DiscountPercent);

public record SaleInvoiceDto(
    int Id, string InvoiceNo, string? BillNo, DateTime Date, int CustomerId, string CustomerName,
    decimal TotalBill, decimal TotalDiscount, decimal NetBill, decimal ReceivedCash, decimal PayCash,
    PrintFormat PrintFormat, PrintLanguage PrintLanguage, List<SaleInvoiceLineDto> Lines);

public record CreateSaleInvoiceRequest(
    DateTime Date, string? BillNo, int CustomerId, List<SaleInvoiceLineRequest> Lines,
    decimal ReceivedCash, PrintFormat PrintFormat, PrintLanguage PrintLanguage);
