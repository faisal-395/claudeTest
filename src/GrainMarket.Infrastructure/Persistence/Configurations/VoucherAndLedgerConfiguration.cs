using GrainMarket.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GrainMarket.Infrastructure.Persistence.Configurations;

public class VoucherConfiguration : IEntityTypeConfiguration<Voucher>
{
    public void Configure(EntityTypeBuilder<Voucher> builder)
    {
        builder.Property(v => v.VoucherNo).IsRequired().HasMaxLength(30);
        builder.HasIndex(v => v.VoucherNo).IsUnique();
        builder.Property(v => v.RefNo).HasMaxLength(50);
        builder.Property(v => v.Description).HasMaxLength(500);
        builder.Property(v => v.DebitDescription).HasMaxLength(500);
        builder.Property(v => v.CreditDescription).HasMaxLength(500);
        builder.Property(v => v.Amount).HasPrecision(18, 2);

        builder.HasOne(v => v.Season).WithMany().HasForeignKey(v => v.SeasonId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(v => v.FromParty).WithMany().HasForeignKey(v => v.FromPartyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(v => v.FromAccount).WithMany().HasForeignKey(v => v.FromAccountId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(v => v.ToParty).WithMany().HasForeignKey(v => v.ToPartyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(v => v.ToAccount).WithMany().HasForeignKey(v => v.ToAccountId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(v => v.DebitAccount).WithMany().HasForeignKey(v => v.DebitAccountId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(v => v.CreditAccount).WithMany().HasForeignKey(v => v.CreditAccountId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class LedgerEntryConfiguration : IEntityTypeConfiguration<LedgerEntry>
{
    public void Configure(EntityTypeBuilder<LedgerEntry> builder)
    {
        builder.Property(e => e.Debit).HasPrecision(18, 2);
        builder.Property(e => e.Credit).HasPrecision(18, 2);
        builder.Property(e => e.RunningBalance).HasPrecision(18, 2);
        builder.Property(e => e.Description).HasMaxLength(500);

        builder.HasOne(e => e.Party).WithMany().HasForeignKey(e => e.PartyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.ChartOfAccount).WithMany().HasForeignKey(e => e.ChartOfAccountId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.PartyId, e.Date });
        builder.HasIndex(e => new { e.ChartOfAccountId, e.Date });
        builder.HasIndex(e => new { e.SourceType, e.SourceId });
    }
}
