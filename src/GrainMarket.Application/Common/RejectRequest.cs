namespace GrainMarket.Application.Common;

/// <summary>Body for every Approvals reject endpoint (Kachi/Pakki/Purchase/SaleInvoice) — just the
/// reviewer's note on why, shown to whoever then has to fix it.</summary>
public record RejectRequest(string? Reason);
