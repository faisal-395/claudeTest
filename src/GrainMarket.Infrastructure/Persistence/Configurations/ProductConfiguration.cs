using GrainMarket.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GrainMarket.Infrastructure.Persistence.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.Property(p => p.Name).IsRequired().HasMaxLength(200);
        builder.Property(p => p.NameUrdu).HasMaxLength(200);
        builder.Property(p => p.Category).HasMaxLength(100);
        builder.Property(p => p.BaseUnit).IsRequired().HasMaxLength(10);
        builder.Property(p => p.DefaultRate).HasPrecision(18, 2);
        builder.HasIndex(p => p.Name);
    }
}

public class UnitConversionConfiguration : IEntityTypeConfiguration<UnitConversion>
{
    public void Configure(EntityTypeBuilder<UnitConversion> builder)
    {
        builder.Property(u => u.FactorToKg).HasPrecision(18, 6);
        builder.HasOne(u => u.Product).WithMany().HasForeignKey(u => u.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(u => new { u.Unit, u.ProductId }).IsUnique();
    }
}
