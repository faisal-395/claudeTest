using GrainMarket.Application.Common.Interfaces;
using GrainMarket.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GrainMarket.Application.Common.Services;

public class InvoiceNumberGenerator : IInvoiceNumberGenerator
{
    private readonly IApplicationDbContext _db;

    public InvoiceNumberGenerator(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<string> NextAsync(string prefix, CancellationToken ct = default)
    {
        var sequence = await _db.NumberSequences.FirstOrDefaultAsync(s => s.Prefix == prefix, ct);
        if (sequence is null)
        {
            sequence = new NumberSequence { Prefix = prefix, LastNumber = 0 };
            _db.NumberSequences.Add(sequence);
        }

        sequence.LastNumber++;
        await _db.SaveChangesAsync(ct);

        return $"{prefix}-{sequence.LastNumber:D6}";
    }
}
