using GrainMarket.Application.Common.Exceptions;
using GrainMarket.Application.Common.Interfaces;
using GrainMarket.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GrainMarket.Application.Recovery;

public class RecoveryService : IRecoveryService
{
    private readonly IApplicationDbContext _db;

    public RecoveryService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<OutstandingPartyDto>> GetOutstandingAsync(CancellationToken ct = default)
    {
        var latestPerParty = await _db.LedgerEntries
            .Where(e => e.PartyId != null && !e.IsDeleted)
            .GroupBy(e => e.PartyId!.Value)
            .Select(g => new { PartyId = g.Key, Latest = g.OrderByDescending(e => e.Date).ThenByDescending(e => e.Id).First() })
            .Where(x => x.Latest.RunningBalance > 0)
            .ToListAsync(ct);

        var partyIds = latestPerParty.Select(x => x.PartyId).ToList();
        var parties = await _db.Parties.Where(p => partyIds.Contains(p.Id) && !p.IsDeleted).ToListAsync(ct);

        return latestPerParty
            .Join(parties, x => x.PartyId, p => p.Id, (x, p) => new OutstandingPartyDto(p.Id, p.Name, p.PartyType, p.Phone, x.Latest.RunningBalance))
            .OrderByDescending(d => d.Balance)
            .ToList();
    }

    public async Task<List<RecoveryNoteDto>> GetNotesAsync(int partyId, CancellationToken ct = default)
    {
        var rows = await _db.RecoveryNotes.Where(n => n.PartyId == partyId && !n.IsDeleted)
            .OrderByDescending(n => n.ContactDate).ToListAsync(ct);
        return rows.Select(ToDto).ToList();
    }

    public async Task<RecoveryNoteDto> AddNoteAsync(CreateRecoveryNoteRequest request, CancellationToken ct = default)
    {
        if (!await _db.Parties.AnyAsync(p => p.Id == request.PartyId && !p.IsDeleted, ct))
            throw new NotFoundException(nameof(Party), request.PartyId);

        var note = new RecoveryNote
        {
            PartyId = request.PartyId,
            ContactDate = request.ContactDate,
            Note = request.Note,
            PromisedDate = request.PromisedDate,
            PromisedAmount = request.PromisedAmount
        };
        _db.RecoveryNotes.Add(note);
        await _db.SaveChangesAsync(ct);
        return ToDto(note);
    }

    private static RecoveryNoteDto ToDto(RecoveryNote n) => new(n.Id, n.PartyId, n.ContactDate, n.Note, n.PromisedDate, n.PromisedAmount);
}
