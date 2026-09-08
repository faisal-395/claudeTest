namespace GrainMarket.Application.Common.Interfaces;

public interface IInvoiceNumberGenerator
{
    /// <summary>Generates the next sequential number for the given prefix, e.g. "K-000123".</summary>
    Task<string> NextAsync(string prefix, CancellationToken ct = default);
}
