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

    public Task<string> NextAsync(string prefix, CancellationToken ct = default) => NextAsync(prefix, 6, ct);

    public async Task<string> NextAsync(string prefix, int padding, CancellationToken ct = default)
    {
        var sequence = await _db.NumberSequences.FirstOrDefaultAsync(s => s.Prefix == prefix, ct);
        if (sequence is null)
        {
            sequence = new NumberSequence { Prefix = prefix, LastNumber = 0 };
            _db.NumberSequences.Add(sequence);
        }

        sequence.LastNumber++;
        await _db.SaveChangesAsync(ct);

        return Format(prefix, sequence.LastNumber, padding);
    }

    public async Task<string> PeekNextAsync(string prefix, int padding, CancellationToken ct = default)
    {
        var sequence = await _db.NumberSequences.FirstOrDefaultAsync(s => s.Prefix == prefix, ct);
        return Format(prefix, (sequence?.LastNumber ?? 0) + 1, padding);
    }

    private static string Format(string prefix, int number, int padding) =>
        padding > 0 ? $"{prefix}-{number.ToString($"D{padding}")}" : $"{prefix}-{number}";
}
