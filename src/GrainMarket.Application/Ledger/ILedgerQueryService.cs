namespace GrainMarket.Application.Ledger;

public interface ILedgerQueryService
{
    Task<PartyLedgerDto> GetPartyLedgerAsync(int partyId, DateTime? from, DateTime? to, CancellationToken ct = default);

    /// <summary>Throws ForbiddenAccessException if the account is protected and the caller's role is not allowed.</summary>
    Task<AccountLedgerDto> GetAccountLedgerAsync(int accountId, DateTime? from, DateTime? to, CancellationToken ct = default);

    /// <summary>Every ledger entry posted on the given date, across every party and account —
    /// the Day Book.</summary>
    Task<List<DayBookRowDto>> GetDayBookAsync(DateTime date, CancellationToken ct = default);
}
