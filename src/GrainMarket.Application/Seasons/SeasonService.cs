using GrainMarket.Application.Common.Exceptions;
using GrainMarket.Application.Common.Interfaces;
using GrainMarket.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GrainMarket.Application.Seasons;

public class SeasonService : ISeasonService
{
    private readonly IApplicationDbContext _db;
    private readonly IDateTimeProvider _clock;

    public SeasonService(IApplicationDbContext db, IDateTimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<List<SeasonDto>> GetAllAsync(CancellationToken ct = default)
    {
        var rows = await _db.Seasons.Where(s => !s.IsDeleted).OrderByDescending(s => s.StartDate).ToListAsync(ct);
        return rows.Select(ToDto).ToList();
    }

    public async Task<SeasonDto> CreateAsync(UpsertSeasonRequest request, CancellationToken ct = default)
    {
        var season = new Season { Name = request.Name, StartDate = request.StartDate, EndDate = request.EndDate, IsActive = request.IsActive };
        _db.Seasons.Add(season);
        await _db.SaveChangesAsync(ct);
        return ToDto(season);
    }

    public async Task<SeasonDto> UpdateAsync(int id, UpsertSeasonRequest request, CancellationToken ct = default)
    {
        var season = await _db.Seasons.FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted, ct)
            ?? throw new NotFoundException(nameof(Season), id);

        season.Name = request.Name;
        season.StartDate = request.StartDate;
        season.EndDate = request.EndDate;
        season.IsActive = request.IsActive;
        season.UpdatedAtUtc = _clock.UtcNow;

        await _db.SaveChangesAsync(ct);
        return ToDto(season);
    }

    private static SeasonDto ToDto(Season s) => new(s.Id, s.Name, s.StartDate, s.EndDate, s.IsActive);
}
