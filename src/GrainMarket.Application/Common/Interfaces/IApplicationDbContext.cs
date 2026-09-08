using GrainMarket.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GrainMarket.Application.Common.Interfaces;

/// <summary>
/// Abstraction over the EF Core DbContext so the Application layer never references
/// Infrastructure/EF Core directly. Implemented by GrainMarket.Infrastructure.Persistence.AppDbContext.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<Party> Parties { get; }
    DbSet<Product> Products { get; }
    DbSet<UnitConversion> UnitConversions { get; }
    DbSet<ChartOfAccount> ChartOfAccounts { get; }
    DbSet<ChartOfAccountRole> ChartOfAccountRoles { get; }
    DbSet<Role> Roles { get; }
    DbSet<RolePermission> RolePermissions { get; }
    DbSet<User> Users { get; }
    DbSet<DeductionRule> DeductionRules { get; }
    DbSet<Season> Seasons { get; }
    DbSet<Kachi> Kachis { get; }
    DbSet<KachiDeductionLine> KachiDeductionLines { get; }
    DbSet<Pakki> Pakkis { get; }
    DbSet<PakkiDeductionLine> PakkiDeductionLines { get; }
    DbSet<SaleInvoice> SaleInvoices { get; }
    DbSet<SaleInvoiceLine> SaleInvoiceLines { get; }
    DbSet<Purchase> Purchases { get; }
    DbSet<PurchaseLine> PurchaseLines { get; }
    DbSet<Voucher> Vouchers { get; }
    DbSet<LedgerEntry> LedgerEntries { get; }
    DbSet<Expense> Expenses { get; }
    DbSet<RecoveryNote> RecoveryNotes { get; }
    DbSet<NumberSequence> NumberSequences { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
