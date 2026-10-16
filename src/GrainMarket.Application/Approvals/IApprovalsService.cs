namespace GrainMarket.Application.Approvals;

public interface IApprovalsService
{
    /// <summary>Every Kachi/Pakki/Purchase/Sale Invoice currently Pending review or sent back
    /// Rejected, across all four types, newest-submitted first.</summary>
    Task<List<PendingApprovalDto>> GetPendingAsync(CancellationToken ct = default);
}
