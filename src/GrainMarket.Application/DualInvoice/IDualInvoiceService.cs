namespace GrainMarket.Application.DualInvoice;

public interface IDualInvoiceService
{
    Task<DualInvoiceResultDto> CreateAsync(CreateDualInvoiceRequest request, CancellationToken ct = default);
}
