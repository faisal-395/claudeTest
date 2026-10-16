using GrainMarket.Application.Common.Interfaces;
using GrainMarket.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GrainMarket.Application.AccountTypes;

public class AccountTypeService : IAccountTypeService
{
    private readonly IApplicationDbContext _db;

    public AccountTypeService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<AccountTypeDto>> GetAllAsync(CancellationToken ct = default)
    {
        var rows = await _db.AccountTypeDefinitions.Where(t => !t.IsDeleted).OrderBy(t => t.Id).ToListAsync(ct);
        return rows.Select(ToDto).ToList();
    }

    public async Task<AccountTypeDto> CreateAsync(CreateAccountTypeRequest request, CancellationToken ct = default)
    {
        var type = new AccountTypeDefinition { Name = request.Name, NameUrdu = request.NameUrdu };
        _db.AccountTypeDefinitions.Add(type);
        await _db.SaveChangesAsync(ct);
        return ToDto(type);
    }

    private static AccountTypeDto ToDto(AccountTypeDefinition t) => new(t.Id, t.Name, t.NameUrdu, t.IsSystemType);
}
