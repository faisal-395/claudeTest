namespace GrainMarket.Application.ProductUnits;

public interface IProductUnitService
{
    Task<List<ProductUnitDto>> GetAllAsync(CancellationToken ct = default);
    Task<ProductUnitDto> CreateAsync(CreateProductUnitRequest request, CancellationToken ct = default);
}
