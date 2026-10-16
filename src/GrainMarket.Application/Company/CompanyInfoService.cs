using GrainMarket.Application.Common.Interfaces;
using GrainMarket.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GrainMarket.Application.Company;

public class CompanyInfoService : ICompanyInfoService
{
    private readonly IApplicationDbContext _db;
    private readonly IDateTimeProvider _clock;

    public CompanyInfoService(IApplicationDbContext db, IDateTimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<CompanyInfoDto> GetAsync(CancellationToken ct = default)
    {
        var info = await GetOrCreateAsync(ct);
        return ToDto(info);
    }

    public async Task<CompanyInfoDto> UpdateAsync(UpdateCompanyInfoRequest request, CancellationToken ct = default)
    {
        var info = await GetOrCreateAsync(ct);

        info.NameEnglish = request.NameEnglish;
        info.NameUrdu = request.NameUrdu;
        info.MarketName = request.MarketName;
        info.Phone = request.Phone;
        info.Mobile = request.Mobile;
        info.Email = request.Email;
        info.NtnNumber = request.NtnNumber;
        info.ProprietorName = request.ProprietorName;
        info.UpdatedAtUtc = _clock.UtcNow;

        await _db.SaveChangesAsync(ct);
        return ToDto(info);
    }

    // A migration always seeds one row, but self-heal instead of throwing if it's ever missing —
    // the print pages depend on this never failing.
    private async Task<CompanyInfo> GetOrCreateAsync(CancellationToken ct)
    {
        var info = await _db.CompanyInfos.FirstOrDefaultAsync(ct);
        if (info is not null) return info;

        info = new CompanyInfo { NameEnglish = string.Empty };
        _db.CompanyInfos.Add(info);
        await _db.SaveChangesAsync(ct);
        return info;
    }

    private static CompanyInfoDto ToDto(CompanyInfo c) => new(
        c.Id, c.NameEnglish, c.NameUrdu, c.MarketName, c.Phone, c.Mobile, c.Email, c.NtnNumber, c.ProprietorName);
}
