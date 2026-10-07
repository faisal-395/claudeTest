using GrainMarket.Application.Common.Interfaces;
using GrainMarket.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GrainMarket.Application.ProductUnits;

public class ProductUnitService : IProductUnitService
{
    private readonly IApplicationDbContext _db;

    public ProductUnitService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<ProductUnitDto>> GetAllAsync(CancellationToken ct = default)
    {
        var rows = await _db.ProductUnits.Where(u => !u.IsDeleted).OrderBy(u => u.Name).ToListAsync(ct);
        return rows.Select(ToDto).ToList();
    }

    public async Task<ProductUnitDto> CreateAsync(CreateProductUnitRequest request, CancellationToken ct = default)
    {
        var unit = new ProductUnit { Name = request.Name, NameUrdu = request.NameUrdu };
        _db.ProductUnits.Add(unit);
        await _db.SaveChangesAsync(ct);
        return ToDto(unit);
    }

    private static ProductUnitDto ToDto(ProductUnit u) => new(u.Id, u.Name, u.NameUrdu, u.IsActive);
}
