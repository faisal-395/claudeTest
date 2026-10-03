using GrainMarket.Application.Common.Interfaces;
using GrainMarket.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GrainMarket.Application.Approvals;

/// <summary>Builds the single cross-type worklist behind Setup &gt; Approvals from the four
/// transaction tables' own ApprovalStatus column — nothing is stored here, this just reads and
/// projects. Approving/rejecting a specific row still goes through that type's own service (see
/// ApprovalsController), which is what actually knows how to post or reverse its ledger entries.</summary>
public class ApprovalsService : IApprovalsService
{
    private readonly IApplicationDbContext _db;

    public ApprovalsService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<PendingApprovalDto>> GetPendingAsync(CancellationToken ct = default)
    {
        var kachis = await _db.Kachis
            .Include(k => k.Farmer)
            .Where(k => !k.IsDeleted && k.ApprovalStatus != ApprovalStatus.Approved)
            .Select(k => new { k.Id, k.InvoiceNo, k.Date, PartyName = k.Farmer.Name, Amount = k.Total, k.ApprovalStatus, k.CreatedAtUtc, k.SubmittedByUserId, k.RejectionReason, k.ReviewedByUserId, k.ReviewedAtUtc })
            .ToListAsync(ct);

        var pakkis = await _db.Pakkis
            .Include(p => p.Farmer)
            .Where(p => !p.IsDeleted && p.ApprovalStatus != ApprovalStatus.Approved)
            .Select(p => new { p.Id, p.InvoiceNo, p.Date, PartyName = p.Farmer.Name, Amount = p.NetPayableToFarmer, p.ApprovalStatus, p.CreatedAtUtc, p.SubmittedByUserId, p.RejectionReason, p.ReviewedByUserId, p.ReviewedAtUtc })
            .ToListAsync(ct);

        var purchases = await _db.Purchases
            .Include(p => p.Supplier)
            .Where(p => !p.IsDeleted && p.ApprovalStatus != ApprovalStatus.Approved)
            .Select(p => new { p.Id, p.InvoiceNo, p.Date, PartyName = p.Supplier.Name, Amount = p.NetBill, p.ApprovalStatus, p.CreatedAtUtc, p.SubmittedByUserId, p.RejectionReason, p.ReviewedByUserId, p.ReviewedAtUtc })
            .ToListAsync(ct);

        var saleInvoices = await _db.SaleInvoices
            .Include(s => s.Customer)
            .Where(s => !s.IsDeleted && s.ApprovalStatus != ApprovalStatus.Approved)
            .Select(s => new { s.Id, s.InvoiceNo, s.Date, PartyName = s.Customer.Name, Amount = s.NetBill, s.ApprovalStatus, s.CreatedAtUtc, s.SubmittedByUserId, s.RejectionReason, s.ReviewedByUserId, s.ReviewedAtUtc })
            .ToListAsync(ct);

        var userIds = kachis.Select(x => x.SubmittedByUserId)
            .Concat(kachis.Select(x => x.ReviewedByUserId))
            .Concat(pakkis.Select(x => x.SubmittedByUserId))
            .Concat(pakkis.Select(x => x.ReviewedByUserId))
            .Concat(purchases.Select(x => x.SubmittedByUserId))
            .Concat(purchases.Select(x => x.ReviewedByUserId))
            .Concat(saleInvoices.Select(x => x.SubmittedByUserId))
            .Concat(saleInvoices.Select(x => x.ReviewedByUserId))
            .Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();
        var usernames = await _db.Users.Where(u => userIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.Username, ct);
        string? NameFor(int? userId) => userId.HasValue && usernames.TryGetValue(userId.Value, out var name) ? name : null;

        var result = new List<PendingApprovalDto>();
        result.AddRange(kachis.Select(k => new PendingApprovalDto("Kachi", k.Id, k.InvoiceNo, k.Date, k.PartyName, k.Amount, k.ApprovalStatus, k.CreatedAtUtc, NameFor(k.SubmittedByUserId), k.RejectionReason, NameFor(k.ReviewedByUserId), k.ReviewedAtUtc)));
        result.AddRange(pakkis.Select(p => new PendingApprovalDto("Pakki", p.Id, p.InvoiceNo, p.Date, p.PartyName, p.Amount, p.ApprovalStatus, p.CreatedAtUtc, NameFor(p.SubmittedByUserId), p.RejectionReason, NameFor(p.ReviewedByUserId), p.ReviewedAtUtc)));
        result.AddRange(purchases.Select(p => new PendingApprovalDto("Purchase", p.Id, p.InvoiceNo, p.Date, p.PartyName, p.Amount, p.ApprovalStatus, p.CreatedAtUtc, NameFor(p.SubmittedByUserId), p.RejectionReason, NameFor(p.ReviewedByUserId), p.ReviewedAtUtc)));
        result.AddRange(saleInvoices.Select(s => new PendingApprovalDto("SaleInvoice", s.Id, s.InvoiceNo, s.Date, s.PartyName, s.Amount, s.ApprovalStatus, s.CreatedAtUtc, NameFor(s.SubmittedByUserId), s.RejectionReason, NameFor(s.ReviewedByUserId), s.ReviewedAtUtc)));

        return result.OrderBy(r => r.Status).ThenBy(r => r.SubmittedAtUtc).ToList();
    }
}
