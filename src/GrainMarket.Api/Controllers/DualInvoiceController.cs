using GrainMarket.Api.Common;
using GrainMarket.Application.DualInvoice;
using GrainMarket.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GrainMarket.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/dual-invoice")]
[ModulePermission(ModuleName.DualInvoice)]
public class DualInvoiceController : ControllerBase
{
    private readonly IDualInvoiceService _service;

    public DualInvoiceController(IDualInvoiceService service)
    {
        _service = service;
    }

    [ModulePermission(ModuleName.DualInvoice, PermissionAction.Create)]
    [HttpPost]
    public async Task<ActionResult<DualInvoiceResultDto>> Create(CreateDualInvoiceRequest request, CancellationToken ct)
        => Ok(await _service.CreateAsync(request, ct));
}
