using GrainMarket.Domain.Enums;

namespace GrainMarket.Application.Recovery;

public record OutstandingPartyDto(int PartyId, string PartyName, PartyType PartyType, string? Phone, decimal Balance);

public record RecoveryNoteDto(int Id, int PartyId, DateTime ContactDate, string Note, DateTime? PromisedDate, decimal? PromisedAmount);

public record CreateRecoveryNoteRequest(int PartyId, DateTime ContactDate, string Note, DateTime? PromisedDate, decimal? PromisedAmount);
