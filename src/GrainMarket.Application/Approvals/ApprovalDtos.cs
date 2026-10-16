using GrainMarket.Domain.Enums;

namespace GrainMarket.Application.Approvals;

/// <summary>One row across all four transaction types that either needs review (Pending) or was
/// sent back for a fix (Rejected) — the Setup &gt; Approvals screen's single worklist. EntityType is
/// one of "Kachi", "Pakki", "Purchase", "SaleInvoice", matching the segment ApprovalsController's
/// approve/reject routes take.</summary>
public record PendingApprovalDto(
    string EntityType, int Id, string InvoiceNo, DateTime Date, string PartyName, decimal Amount,
    ApprovalStatus Status, DateTime SubmittedAtUtc, string? SubmittedByUsername,
    string? RejectionReason, string? ReviewedByUsername, DateTime? ReviewedAtUtc);
