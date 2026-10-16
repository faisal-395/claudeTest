using GrainMarket.Application.Common.Interfaces;
using GrainMarket.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GrainMarket.Application.ProductTypes;

public class ProductTypeService : IProductTypeService
{
    private readonly IApplicationDbContext _db;

    public ProductTypeService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<ProductTypeDto>> GetAllAsync(CancellationToken ct = default)
    {
        var rows = await _db.ProductTypes.Where(t => !t.IsDeleted).OrderBy(t => t.Name).ToListAsync(ct);
        return rows.Select(ToDto).ToList();
    }

    public async Task<ProductTypeDto> CreateAsync(CreateProductTypeRequest request, CancellationToken ct = default)
    {
        var type = new ProductType { Name = request.Name, NameUrdu = request.NameUrdu };
        _db.ProductTypes.Add(type);
        await _db.SaveChangesAsync(ct);
        return ToDto(type);
    }

    private static ProductTypeDto ToDto(ProductType t) => new(t.Id, t.Name, t.NameUrdu, t.IsActive);
}
