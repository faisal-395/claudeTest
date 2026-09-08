using GrainMarket.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GrainMarket.Infrastructure.Persistence.Configurations;

public class SaleInvoiceConfiguration : IEntityTypeConfiguration<SaleInvoice>
{
    public void Configure(EntityTypeBuilder<SaleInvoice> builder)
    {
        builder.Property(s => s.InvoiceNo).IsRequired().HasMaxLength(30);
        builder.HasIndex(s => s.InvoiceNo).IsUnique();
        builder.Property(s => s.BillNo).HasMaxLength(30);
        builder.Property(s => s.TotalBill).HasPrecision(18, 2);
        builder.Property(s => s.TotalDiscount).HasPrecision(18, 2);
        builder.Property(s => s.NetBill).HasPrecision(18, 2);
        builder.Property(s => s.ReceivedCash).HasPrecision(18, 2);
        builder.Property(s => s.PayCash).HasPrecision(18, 2);

        builder.HasOne(s => s.Customer).WithMany().HasForeignKey(s => s.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(s => s.Lines).WithOne(l => l.SaleInvoice).HasForeignKey(l => l.SaleInvoiceId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class SaleInvoiceLineConfiguration : IEntityTypeConfiguration<SaleInvoiceLine>
{
    public void Configure(EntityTypeBuilder<SaleInvoiceLine> builder)
    {
        builder.Property(l => l.Quantity).HasPrecision(18, 4);
        builder.Property(l => l.Price).HasPrecision(18, 2);
        builder.Property(l => l.DiscountPercent).HasPrecision(5, 2);
        builder.Property(l => l.NetPrice).HasPrecision(18, 2);
        builder.HasOne(l => l.Product).WithMany().HasForeignKey(l => l.ProductId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class PurchaseConfiguration : IEntityTypeConfiguration<Purchase>
{
    public void Configure(EntityTypeBuilder<Purchase> builder)
    {
        builder.Property(p => p.InvoiceNo).IsRequired().HasMaxLength(30);
        builder.HasIndex(p => p.InvoiceNo).IsUnique();
        builder.Property(p => p.BillNo).HasMaxLength(30);
        builder.Property(p => p.TotalBill).HasPrecision(18, 2);
        builder.Property(p => p.TotalDiscount).HasPrecision(18, 2);
        builder.Property(p => p.NetBill).HasPrecision(18, 2);
        builder.Property(p => p.PaidCash).HasPrecision(18, 2);

        builder.HasOne(p => p.Supplier).WithMany().HasForeignKey(p => p.SupplierId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(p => p.Lines).WithOne(l => l.Purchase).HasForeignKey(l => l.PurchaseId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class PurchaseLineConfiguration : IEntityTypeConfiguration<PurchaseLine>
{
    public void Configure(EntityTypeBuilder<PurchaseLine> builder)
    {
        builder.Property(l => l.Quantity).HasPrecision(18, 4);
        builder.Property(l => l.Price).HasPrecision(18, 2);
        builder.Property(l => l.DiscountPercent).HasPrecision(5, 2);
        builder.Property(l => l.NetPrice).HasPrecision(18, 2);
        builder.HasOne(l => l.Product).WithMany().HasForeignKey(l => l.ProductId).OnDelete(DeleteBehavior.Restrict);
    }
}
