using GrainMarket.Application.Common.Exceptions;
using GrainMarket.Application.Common.Interfaces;
using GrainMarket.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GrainMarket.Application.ChartOfAccounts;

public class ChartOfAccountService : IChartOfAccountService
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public ChartOfAccountService(IApplicationDbContext db, ICurrentUser currentUser, IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<List<ChartOfAccountDto>> GetVisibleAsync(bool includeInactive = false, CancellationToken ct = default)
    {
        var query = _db.ChartOfAccounts
            .Include(a => a.AllowedRoles)
            .Where(a => !a.IsDeleted);
        if (!includeInactive) query = query.Where(a => a.IsActive);

        var all = await query.OrderBy(a => a.Code).ToListAsync(ct);
        var visible = all.Where(IsVisibleToCurrentUser).ToList();

        var balances = await GetCurrentBalancesAsync(visible.Select(a => a.Id).ToList(), ct);
        return visible.Select(a => ToDto(a, balances.GetValueOrDefault(a.Id))).ToList();
    }

    public async Task<ChartOfAccountDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var account = await _db.ChartOfAccounts
            .Include(a => a.AllowedRoles)
            .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted, ct)
            ?? throw new NotFoundException(nameof(ChartOfAccount), id);

        if (!IsVisibleToCurrentUser(account))
        {
            // Never reveal that a protected account exists via a raw id lookup either.
            throw new NotFoundException(nameof(ChartOfAccount), id);
        }

        var balance = (await GetCurrentBalancesAsync(new[] { id }, ct)).GetValueOrDefault(id);
        return ToDto(account, balance);
    }

    public async Task<ChartOfAccountDto> CreateAsync(UpsertChartOfAccountRequest request, CancellationToken ct = default)
    {
        var account = new ChartOfAccount
        {
            Code = request.Code,
            Name = request.Name,
            NameUrdu = request.NameUrdu,
            AccountType = request.AccountType,
            ParentAccountId = request.ParentAccountId,
            IsProtected = request.IsProtected,
            IsActive = request.IsActive
        };
        _db.ChartOfAccounts.Add(account);
        await _db.SaveChangesAsync(ct);

        if (request.IsProtected)
        {
            foreach (var roleId in request.AllowedRoleIds.Distinct())
            {
                _db.ChartOfAccountRoles.Add(new ChartOfAccountRole { ChartOfAccountId = account.Id, RoleId = roleId });
            }
            await _db.SaveChangesAsync(ct);
        }

        return await GetByIdAsync(account.Id, ct);
    }

    public async Task<ChartOfAccountDto> UpdateAsync(int id, UpsertChartOfAccountRequest request, CancellationToken ct = default)
    {
        var account = await _db.ChartOfAccounts
            .Include(a => a.AllowedRoles)
            .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted, ct)
            ?? throw new NotFoundException(nameof(ChartOfAccount), id);

        account.Code = request.Code;
        account.Name = request.Name;
        account.NameUrdu = request.NameUrdu;
        account.AccountType = request.AccountType;
        account.ParentAccountId = request.ParentAccountId;
        account.IsProtected = request.IsProtected;
        account.IsActive = request.IsActive;
        account.UpdatedAtUtc = _clock.UtcNow;

        foreach (var existing in account.AllowedRoles.ToList())
        {
            _db.ChartOfAccountRoles.Remove(existing);
        }
        if (request.IsProtected)
        {
            foreach (var roleId in request.AllowedRoleIds.Distinct())
            {
                _db.ChartOfAccountRoles.Add(new ChartOfAccountRole { ChartOfAccountId = account.Id, RoleId = roleId });
            }
        }

        await _db.SaveChangesAsync(ct);
        return await GetByIdAsync(id, ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var account = await _db.ChartOfAccounts.FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted, ct)
            ?? throw new NotFoundException(nameof(ChartOfAccount), id);
        account.IsDeleted = true;
        account.IsActive = false;
        await _db.SaveChangesAsync(ct);
    }

    private bool IsVisibleToCurrentUser(ChartOfAccount account)
    {
        if (!account.IsProtected) return true;
        return _currentUser.HasAllowedAccountRole(account.AllowedRoles.Select(r => r.RoleId));
    }

    private async Task<Dictionary<int, decimal>> GetCurrentBalancesAsync(IReadOnlyCollection<int> accountIds, CancellationToken ct)
    {
        if (accountIds.Count == 0) return new Dictionary<int, decimal>();

        var accountIdList = accountIds.ToList();
        var latest = await _db.LedgerEntries
            .Where(e => e.ChartOfAccountId != null && accountIdList.Contains(e.ChartOfAccountId.Value))
            .GroupBy(e => e.ChartOfAccountId!.Value)
            .Select(g => new { AccountId = g.Key, Latest = g.OrderByDescending(e => e.Date).ThenByDescending(e => e.Id).First() })
            .ToListAsync(ct);

        return latest.ToDictionary(x => x.AccountId, x => x.Latest.RunningBalance);
    }

    private static ChartOfAccountDto ToDto(ChartOfAccount a, decimal currentBalance) => new(
        a.Id, a.Code, a.Name, a.NameUrdu, a.AccountType, a.ParentAccountId,
        a.IsProtected, a.IsActive, currentBalance, a.AllowedRoles.Select(r => r.RoleId).ToList());
}
