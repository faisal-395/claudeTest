using GrainMarket.Domain.Enums;

namespace GrainMarket.Application.Parties;

public record PartyDto(
    int Id,
    string Name,
    string? NameUrdu,
    PartyType PartyType,
    string? Phone,
    string? Cnic,
    string? Address,
    decimal OpeningBalance,
    BalanceSide OpeningBalanceType,
    decimal CurrentBalance,
    bool IsActive);

public record UpsertPartyRequest(
    string Name,
    string? NameUrdu,
    PartyType PartyType,
    string? Phone,
    string? Cnic,
    string? Address,
    decimal OpeningBalance,
    BalanceSide OpeningBalanceType,
    bool IsActive);
