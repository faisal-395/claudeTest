namespace GrainMarket.Application.AccountTypes;

public interface IAccountTypeService
{
    Task<List<AccountTypeDto>> GetAllAsync(CancellationToken ct = default);
    Task<AccountTypeDto> CreateAsync(CreateAccountTypeRequest request, CancellationToken ct = default);
}
