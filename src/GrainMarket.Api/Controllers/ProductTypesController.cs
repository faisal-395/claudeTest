using GrainMarket.Api.Common;
using GrainMarket.Application.ProductTypes;
using GrainMarket.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GrainMarket.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/product-types")]
public class ProductTypesController : ControllerBase
{
    private readonly IProductTypeService _service;

    public ProductTypesController(IProductTypeService service)
    {
        _service = service;
    }

    // Deliberately not gated by ModulePermission: Purchase/Sale Invoice/Stock need this list to
    // populate their Product Type filter/dropdown regardless of the caller's Setup access.
    [HttpGet]
    public async Task<ActionResult<List<ProductTypeDto>>> GetAll(CancellationToken ct) => Ok(await _service.GetAllAsync(ct));

    [ModulePermission(ModuleName.SetupProducts, PermissionAction.Create)]
    [HttpPost]
    public async Task<ActionResult<ProductTypeDto>> Create(CreateProductTypeRequest request, CancellationToken ct)
        => Ok(await _service.CreateAsync(request, ct));
}
