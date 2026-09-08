using GrainMarket.Application.Kachis;
using GrainMarket.Application.Pakkis;

namespace GrainMarket.Application.DualInvoice;

public class DualInvoiceService : IDualInvoiceService
{
    private readonly IKachiService _kachiService;
    private readonly IPakkiService _pakkiService;

    public DualInvoiceService(IKachiService kachiService, IPakkiService pakkiService)
    {
        _kachiService = kachiService;
        _pakkiService = pakkiService;
    }

    public async Task<DualInvoiceResultDto> CreateAsync(CreateDualInvoiceRequest request, CancellationToken ct = default)
    {
        var kachi = await _kachiService.CreateAsync(new CreateKachiRequest(
            request.Date, request.SeasonId, request.FarmerId, request.ProductId,
            request.ManQty, request.KiloQty, request.GramQty, request.BoriQty,
            request.RatePerUnit, request.VehicleNumber, request.Notes), ct);

        var pakki = await _pakkiService.CreateFromKachiAsync(new CreatePakkiFromKachiRequest(
            kachi.Id, request.Date, request.BuyerId, request.RatePerUnit, request.VehicleNumber, request.Notes), ct);

        return new DualInvoiceResultDto(kachi.Id, kachi.InvoiceNo, pakki.Id, pakki.InvoiceNo, pakki.NetPayableToFarmer);
    }
}
