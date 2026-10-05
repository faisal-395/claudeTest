namespace GrainMarket.Application.Common.Interfaces;

public interface IInvoiceNumberGenerator
{
    /// <summary>Generates the next sequential number for the given prefix, e.g. "K-000123".</summary>
    Task<string> NextAsync(string prefix, CancellationToken ct = default);

    /// <summary>Same as NextAsync but with a caller-chosen zero-padding width instead of the fixed
    /// 6 digits — e.g. padding 0 gives "PV-7" instead of "PV-000007".</summary>
    Task<string> NextAsync(string prefix, int padding, CancellationToken ct = default);

    /// <summary>Read-only preview of what NextAsync would hand out next, without consuming it — the
    /// sequence's LastNumber is left untouched. Lets a screen show "what the number will be" before
    /// Save, without a page load (or an abandoned entry) silently burning a number.</summary>
    Task<string> PeekNextAsync(string prefix, int padding, CancellationToken ct = default);
}
