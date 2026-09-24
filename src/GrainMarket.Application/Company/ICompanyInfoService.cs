namespace GrainMarket.Application.Company;

public interface ICompanyInfoService
{
    Task<CompanyInfoDto> GetAsync(CancellationToken ct = default);
    Task<CompanyInfoDto> UpdateAsync(UpdateCompanyInfoRequest request, CancellationToken ct = default);
}
