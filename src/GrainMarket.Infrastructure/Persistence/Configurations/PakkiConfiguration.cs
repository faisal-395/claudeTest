using GrainMarket.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GrainMarket.Infrastructure.Persistence.Configurations;

public class PakkiConfiguration : IEntityTypeConfiguration<Pakki>
{
    public void Configure(EntityTypeBuilder<Pakki> builder)
    {
        builder.Property(p => p.InvoiceNo).IsRequired().HasMaxLength(30);
        builder.HasIndex(p => p.InvoiceNo).IsUnique();
        builder.Property(p => p.VehicleNumber).HasMaxLength(30);

        foreach (var name in new[] { nameof(Pakki.ManQty), nameof(Pakki.KiloQty), nameof(Pakki.GramQty), nameof(Pakki.BoriQty) })
        {
            builder.Property(name).HasPrecision(18, 4);
        }
        builder.Property(p => p.NetWeightKg).HasPrecision(18, 4);
        builder.Property(p => p.RatePerUnit).HasPrecision(18, 4);
        builder.Property(p => p.GrossAmount).HasPrecision(18, 2);
        builder.Property(p => p.TotalDeductions).HasPrecision(18, 2);
        builder.Property(p => p.NetPayableToFarmer).HasPrecision(18, 2);

        builder.HasOne(p => p.Season).WithMany().HasForeignKey(p => p.SeasonId).OnDelete(DeleteBehavior.Restrict);
        // Deliberately unpaired with Kachi.ConvertedToPakki (that is a separate one-to-one, configured
        // in KachiConfiguration) — Pakki.KachiId just points back at its originating Kachi, if any.
        builder.HasOne(p => p.Kachi).WithOne().HasForeignKey<Pakki>(p => p.KachiId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(p => p.Buyer).WithMany(pt => pt.PakkisAsBuyer).HasForeignKey(p => p.BuyerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(p => p.Farmer).WithMany().HasForeignKey(p => p.FarmerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(p => p.Product).WithMany().HasForeignKey(p => p.ProductId).OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.DeductionLines).WithOne(l => l.Pakki).HasForeignKey(l => l.PakkiId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class PakkiDeductionLineConfiguration : IEntityTypeConfiguration<PakkiDeductionLine>
{
    public void Configure(EntityTypeBuilder<PakkiDeductionLine> builder)
    {
        builder.Property(l => l.Name).IsRequired().HasMaxLength(100);
        builder.Property(l => l.NameUrdu).IsRequired().HasMaxLength(100);
        builder.Property(l => l.Amount).HasPrecision(18, 2);
        builder.Property(l => l.VehicleNumber).HasMaxLength(30);
        builder.HasOne(l => l.DeductionRule).WithMany().HasForeignKey(l => l.DeductionRuleId).OnDelete(DeleteBehavior.Restrict);
    }
}
