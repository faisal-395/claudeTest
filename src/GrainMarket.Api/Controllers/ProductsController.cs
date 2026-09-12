using GrainMarket.Api.Common;
using GrainMarket.Application.Products;
using GrainMarket.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GrainMarket.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/products")]
[ModulePermission(ModuleName.SetupProducts)]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    [HttpGet]
    public async Task<ActionResult<List<ProductDto>>> GetAll([FromQuery] bool includeInactive, [FromQuery] string? category, CancellationToken ct)
        => Ok(await _productService.GetAllAsync(includeInactive, category, ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductDto>> GetById(int id, CancellationToken ct)
        => Ok(await _productService.GetByIdAsync(id, ct));

    [ModulePermission(ModuleName.SetupProducts, PermissionAction.Create)]
    [HttpPost]
    public async Task<ActionResult<ProductDto>> Create(UpsertProductRequest request, CancellationToken ct)
    {
        var result = await _productService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [ModulePermission(ModuleName.SetupProducts, PermissionAction.Edit)]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ProductDto>> Update(int id, UpsertProductRequest request, CancellationToken ct)
        => Ok(await _productService.UpdateAsync(id, request, ct));

    [ModulePermission(ModuleName.SetupProducts, PermissionAction.Delete)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _productService.DeleteAsync(id, ct);
        return NoContent();
    }
}
