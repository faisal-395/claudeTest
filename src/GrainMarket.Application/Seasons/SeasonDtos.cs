namespace GrainMarket.Application.Seasons;

public record SeasonDto(int Id, string Name, DateTime StartDate, DateTime? EndDate, bool IsActive);

public record UpsertSeasonRequest(string Name, DateTime StartDate, DateTime? EndDate, bool IsActive);
