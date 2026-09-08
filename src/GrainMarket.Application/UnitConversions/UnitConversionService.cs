using GrainMarket.Application.Common.Exceptions;
using GrainMarket.Application.Common.Interfaces;
using GrainMarket.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GrainMarket.Application.UnitConversions;

public class UnitConversionService : IUnitConversionService
{
    private readonly IApplicationDbContext _db;
    private readonly IDateTimeProvider _clock;

    public UnitConversionService(IApplicationDbContext db, IDateTimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<List<UnitConversionDto>> GetAllAsync(CancellationToken ct = default)
    {
        var rows = await _db.UnitConversions
            .Include(u => u.Product)
            .Where(u => !u.IsDeleted)
            .OrderBy(u => u.Unit).ThenBy(u => u.ProductId)
            .ToListAsync(ct);
        return rows.Select(ToDto).ToList();
    }

    public async Task<UnitConversionDto> CreateAsync(UpsertUnitConversionRequest request, CancellationToken ct = default)
    {
        var row = new UnitConversion
        {
            Unit = request.Unit,
            FactorToKg = request.FactorToKg,
            ProductId = request.ProductId,
            IsActive = request.IsActive
        };
        _db.UnitConversions.Add(row);
        await _db.SaveChangesAsync(ct);
        return await GetByIdAsync(row.Id, ct);
    }

    public async Task<UnitConversionDto> UpdateAsync(int id, UpsertUnitConversionRequest request, CancellationToken ct = default)
    {
        var row = await _db.UnitConversions.FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted, ct)
            ?? throw new NotFoundException(nameof(UnitConversion), id);

        row.Unit = request.Unit;
        row.FactorToKg = request.FactorToKg;
        row.ProductId = request.ProductId;
        row.IsActive = request.IsActive;
        row.UpdatedAtUtc = _clock.UtcNow;

        await _db.SaveChangesAsync(ct);
        return await GetByIdAsync(id, ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var row = await _db.UnitConversions.FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted, ct)
            ?? throw new NotFoundException(nameof(UnitConversion), id);
        row.IsDeleted = true;
        row.IsActive = false;
        await _db.SaveChangesAsync(ct);
    }

    private async Task<UnitConversionDto> GetByIdAsync(int id, CancellationToken ct)
    {
        var row = await _db.UnitConversions.Include(u => u.Product).FirstAsync(u => u.Id == id, ct);
        return ToDto(row);
    }

    private static UnitConversionDto ToDto(UnitConversion u) => new(u.Id, u.Unit, u.FactorToKg, u.ProductId, u.Product?.Name, u.IsActive);
}
