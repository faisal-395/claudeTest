using GrainMarket.Application.Common.Exceptions;
using GrainMarket.Application.Common.Interfaces;
using GrainMarket.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GrainMarket.Application.Products;

public class ProductService : IProductService
{
    private readonly IApplicationDbContext _db;
    private readonly IDateTimeProvider _clock;

    public ProductService(IApplicationDbContext db, IDateTimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<List<ProductDto>> GetAllAsync(bool includeInactive = false, CancellationToken ct = default)
    {
        var query = _db.Products.Where(p => !p.IsDeleted);
        if (!includeInactive) query = query.Where(p => p.IsActive);
        var products = await query.OrderBy(p => p.Name).ToListAsync(ct);
        return products.Select(ToDto).ToList();
    }

    public async Task<ProductDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, ct)
            ?? throw new NotFoundException(nameof(Product), id);
        return ToDto(product);
    }

    public async Task<ProductDto> CreateAsync(UpsertProductRequest request, CancellationToken ct = default)
    {
        var product = new Product
        {
            Name = request.Name,
            NameUrdu = request.NameUrdu,
            Category = request.Category,
            BaseUnit = "kg",
            DefaultRate = request.DefaultRate,
            IsActive = request.IsActive
        };
        _db.Products.Add(product);
        await _db.SaveChangesAsync(ct);
        return ToDto(product);
    }

    public async Task<ProductDto> UpdateAsync(int id, UpsertProductRequest request, CancellationToken ct = default)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, ct)
            ?? throw new NotFoundException(nameof(Product), id);

        product.Name = request.Name;
        product.NameUrdu = request.NameUrdu;
        product.Category = request.Category;
        product.DefaultRate = request.DefaultRate;
        product.IsActive = request.IsActive;
        product.UpdatedAtUtc = _clock.UtcNow;

        await _db.SaveChangesAsync(ct);
        return ToDto(product);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, ct)
            ?? throw new NotFoundException(nameof(Product), id);
        product.IsDeleted = true;
        product.IsActive = false;
        await _db.SaveChangesAsync(ct);
    }

    private static ProductDto ToDto(Product p) => new(p.Id, p.Name, p.NameUrdu, p.Category, p.BaseUnit, p.DefaultRate, p.IsActive);
}
