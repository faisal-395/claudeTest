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
        builder.Property(p => p.SalePrice).HasPrecision(18, 2);
        builder.Property(p => p.SaleMarkupPercent).HasPrecision(5, 2);
        builder.HasIndex(p => p.Name);
        builder.HasOne(p => p.ProductType).WithMany().HasForeignKey(p => p.ProductTypeId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class ProductTypeConfiguration : IEntityTypeConfiguration<ProductType>
{
    public void Configure(EntityTypeBuilder<ProductType> builder)
    {
        builder.Property(t => t.Name).IsRequired().HasMaxLength(100);
        builder.Property(t => t.NameUrdu).HasMaxLength(100);
        builder.HasIndex(t => t.Name).IsUnique();
    }
}

public class ProductUnitConfiguration : IEntityTypeConfiguration<ProductUnit>
{
    public void Configure(EntityTypeBuilder<ProductUnit> builder)
    {
        builder.Property(u => u.Name).IsRequired().HasMaxLength(10);
        builder.Property(u => u.NameUrdu).HasMaxLength(20);
        builder.HasIndex(u => u.Name).IsUnique();
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
