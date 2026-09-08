using GrainMarket.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GrainMarket.Infrastructure.Persistence.Configurations;

public class DeductionRuleConfiguration : IEntityTypeConfiguration<DeductionRule>
{
    public void Configure(EntityTypeBuilder<DeductionRule> builder)
    {
        builder.Property(r => r.Name).IsRequired().HasMaxLength(100);
        builder.Property(r => r.NameUrdu).IsRequired().HasMaxLength(100);
        builder.Property(r => r.Value).HasPrecision(18, 4);

        builder.HasOne(r => r.Product).WithMany().HasForeignKey(r => r.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(r => r.Party).WithMany().HasForeignKey(r => r.PartyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(r => r.IncomeAccount).WithMany().HasForeignKey(r => r.IncomeAccountId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class SeasonConfiguration : IEntityTypeConfiguration<Season>
{
    public void Configure(EntityTypeBuilder<Season> builder)
    {
        builder.Property(s => s.Name).IsRequired().HasMaxLength(50);
        builder.HasIndex(s => s.Name).IsUnique();
    }
}

public class NumberSequenceConfiguration : IEntityTypeConfiguration<NumberSequence>
{
    public void Configure(EntityTypeBuilder<NumberSequence> builder)
    {
        builder.Property(s => s.Prefix).IsRequired().HasMaxLength(10);
        builder.HasIndex(s => s.Prefix).IsUnique();
    }
}
