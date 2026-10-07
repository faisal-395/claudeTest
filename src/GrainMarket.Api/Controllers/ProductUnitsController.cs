using GrainMarket.Api.Common;
using GrainMarket.Application.ProductUnits;
using GrainMarket.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GrainMarket.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/product-units")]
public class ProductUnitsController : ControllerBase
{
    private readonly IProductUnitService _service;

    public ProductUnitsController(IProductUnitService service)
    {
        _service = service;
    }

    // Deliberately not gated by ModulePermission: the Products form needs this list to populate
    // its Unit dropdown regardless of the caller's Setup access.
    [HttpGet]
    public async Task<ActionResult<List<ProductUnitDto>>> GetAll(CancellationToken ct) => Ok(await _service.GetAllAsync(ct));

    [ModulePermission(ModuleName.SetupProducts, PermissionAction.Create)]
    [HttpPost]
    public async Task<ActionResult<ProductUnitDto>> Create(CreateProductUnitRequest request, CancellationToken ct)
        => Ok(await _service.CreateAsync(request, ct));
}
