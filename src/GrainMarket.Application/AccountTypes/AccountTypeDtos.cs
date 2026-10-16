namespace GrainMarket.Application.AccountTypes;

public record AccountTypeDto(int Id, string Name, string? NameUrdu, bool IsSystemType);

public record CreateAccountTypeRequest(string Name, string? NameUrdu);
