namespace GrainMarket.Application.Products;

public interface IProductService
{
    Task<List<ProductDto>> GetAllAsync(bool includeInactive = false, CancellationToken ct = default);
    Task<ProductDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<ProductDto> CreateAsync(UpsertProductRequest request, CancellationToken ct = default);
    Task<ProductDto> UpdateAsync(int id, UpsertProductRequest request, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
