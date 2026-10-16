namespace GrainMarket.Application.ProductTypes;

public interface IProductTypeService
{
    Task<List<ProductTypeDto>> GetAllAsync(CancellationToken ct = default);
    Task<ProductTypeDto> CreateAsync(CreateProductTypeRequest request, CancellationToken ct = default);
}
