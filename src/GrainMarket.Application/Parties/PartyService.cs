using GrainMarket.Application.Common.Exceptions;
using GrainMarket.Application.Common.Interfaces;
using GrainMarket.Domain.Entities;
using GrainMarket.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GrainMarket.Application.Parties;

public class PartyService : IPartyService
{
    private readonly IApplicationDbContext _db;
    private readonly ILedgerPostingService _ledger;
    private readonly IDateTimeProvider _clock;

    public PartyService(IApplicationDbContext db, ILedgerPostingService ledger, IDateTimeProvider clock)
    {
        _db = db;
        _ledger = ledger;
        _clock = clock;
    }

    public async Task<List<PartyDto>> GetAllAsync(PartyType? type = null, bool includeInactive = false, CancellationToken ct = default)
    {
        var query = _db.Parties.Where(p => !p.IsDeleted);
        if (type is not null) query = query.Where(p => (p.PartyType & type.Value) == type.Value);
        if (!includeInactive) query = query.Where(p => p.IsActive);

        var parties = await query.OrderBy(p => p.Name).ToListAsync(ct);
        var balances = await GetCurrentBalancesAsync(parties.Select(p => p.Id).ToList(), ct);

        return parties.Select(p => ToDto(p, balances.GetValueOrDefault(p.Id))).ToList();
    }

    public async Task<PartyDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var party = await _db.Parties.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, ct)
            ?? throw new NotFoundException(nameof(Party), id);

        var balance = (await GetCurrentBalancesAsync(new[] { id }, ct)).GetValueOrDefault(id);
        return ToDto(party, balance);
    }

    public async Task<PartyDto> CreateAsync(UpsertPartyRequest request, CancellationToken ct = default)
    {
        var party = new Party
        {
            Name = request.Name,
            NameUrdu = request.NameUrdu,
            PartyType = request.PartyType,
            Phone = request.Phone,
            Cnic = request.Cnic,
            Address = request.Address,
            OpeningBalance = request.OpeningBalance,
            OpeningBalanceType = request.OpeningBalanceType,
            IsActive = request.IsActive
        };

        _db.Parties.Add(party);
        await _db.SaveChangesAsync(ct);

        if (request.OpeningBalance > 0)
        {
            var debit = request.OpeningBalanceType == BalanceSide.Debit ? request.OpeningBalance : 0m;
            var credit = request.OpeningBalanceType == BalanceSide.Credit ? request.OpeningBalance : 0m;
            await _ledger.PostPartyEntryAsync(party.Id, _clock.UtcNow, debit, credit,
                Domain.Enums.LedgerSourceType.OpeningBalance, party.Id, "Opening balance", ct);
            await _db.SaveChangesAsync(ct);
        }

        return await GetByIdAsync(party.Id, ct);
    }

    public async Task<PartyDto> UpdateAsync(int id, UpsertPartyRequest request, CancellationToken ct = default)
    {
        var party = await _db.Parties.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, ct)
            ?? throw new NotFoundException(nameof(Party), id);

        party.Name = request.Name;
        party.NameUrdu = request.NameUrdu;
        party.PartyType = request.PartyType;
        party.Phone = request.Phone;
        party.Cnic = request.Cnic;
        party.Address = request.Address;
        party.IsActive = request.IsActive;
        party.UpdatedAtUtc = _clock.UtcNow;

        await _db.SaveChangesAsync(ct);
        return await GetByIdAsync(id, ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var party = await _db.Parties.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, ct)
            ?? throw new NotFoundException(nameof(Party), id);

        party.IsDeleted = true;
        party.IsActive = false;
        await _db.SaveChangesAsync(ct);
    }

    private async Task<Dictionary<int, decimal>> GetCurrentBalancesAsync(IReadOnlyCollection<int> partyIds, CancellationToken ct)
    {
        if (partyIds.Count == 0) return new Dictionary<int, decimal>();

        var partyIdList = partyIds.ToList();
        var latestPerParty = await _db.LedgerEntries
            .Where(e => e.PartyId != null && partyIdList.Contains(e.PartyId.Value))
            .GroupBy(e => e.PartyId!.Value)
            .Select(g => new { PartyId = g.Key, Latest = g.OrderByDescending(e => e.Date).ThenByDescending(e => e.Id).First() })
            .ToListAsync(ct);

        return latestPerParty.ToDictionary(x => x.PartyId, x => x.Latest.RunningBalance);
    }

    private static PartyDto ToDto(Party p, decimal currentBalance) => new(
        p.Id, p.Name, p.NameUrdu, p.PartyType, p.Phone, p.Cnic, p.Address,
        p.OpeningBalance, p.OpeningBalanceType, currentBalance, p.IsActive);
}
