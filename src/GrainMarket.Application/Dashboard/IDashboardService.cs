namespace GrainMarket.Application.Dashboard;

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync(DateTime date, CancellationToken ct = default);
}
