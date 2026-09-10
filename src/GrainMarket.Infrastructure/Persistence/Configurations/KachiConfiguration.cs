using GrainMarket.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GrainMarket.Infrastructure.Persistence.Configurations;

public class KachiConfiguration : IEntityTypeConfiguration<Kachi>
{
    public void Configure(EntityTypeBuilder<Kachi> builder)
    {
        builder.Property(k => k.InvoiceNo).IsRequired().HasMaxLength(30);
        builder.HasIndex(k => k.InvoiceNo).IsUnique();
        builder.Property(k => k.ReceiptNumber).HasMaxLength(30);

        foreach (var name in new[] { nameof(Kachi.ManQty), nameof(Kachi.KiloQty), nameof(Kachi.GramQty), nameof(Kachi.BoriQty) })
        {
            builder.Property(name).HasPrecision(18, 4);
        }
        builder.Property(k => k.NetWeightKg).HasPrecision(18, 4);
        builder.Property(k => k.RatePerUnit).HasPrecision(18, 4);
        builder.Property(k => k.GrossAmount).HasPrecision(18, 2);
        builder.Property(k => k.TotalDeductions).HasPrecision(18, 2);
        builder.Property(k => k.Total).HasPrecision(18, 2);

        builder.HasOne(k => k.Season).WithMany().HasForeignKey(k => k.SeasonId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(k => k.Farmer).WithMany(p => p.Kachis).HasForeignKey(k => k.FarmerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(k => k.Buyer).WithMany().HasForeignKey(k => k.BuyerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(k => k.Product).WithMany().HasForeignKey(k => k.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(k => k.ConvertedToPakki).WithOne().HasForeignKey<Kachi>(k => k.ConvertedToPakkiId).OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(k => k.DeductionLines).WithOne(l => l.Kachi).HasForeignKey(l => l.KachiId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class KachiDeductionLineConfiguration : IEntityTypeConfiguration<KachiDeductionLine>
{
    public void Configure(EntityTypeBuilder<KachiDeductionLine> builder)
    {
        builder.Property(l => l.Name).IsRequired().HasMaxLength(100);
        builder.Property(l => l.NameUrdu).IsRequired().HasMaxLength(100);
        builder.Property(l => l.Amount).HasPrecision(18, 2);
        builder.Property(l => l.VehicleNumber).HasMaxLength(30);
        builder.HasOne(l => l.DeductionRule).WithMany().HasForeignKey(l => l.DeductionRuleId).OnDelete(DeleteBehavior.Restrict);
    }
}
