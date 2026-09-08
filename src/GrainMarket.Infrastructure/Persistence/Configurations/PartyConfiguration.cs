using GrainMarket.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GrainMarket.Infrastructure.Persistence.Configurations;

public class PartyConfiguration : IEntityTypeConfiguration<Party>
{
    public void Configure(EntityTypeBuilder<Party> builder)
    {
        builder.Property(p => p.Name).IsRequired().HasMaxLength(200);
        builder.Property(p => p.NameUrdu).HasMaxLength(200);
        builder.Property(p => p.Phone).HasMaxLength(30);
        builder.Property(p => p.Cnic).HasMaxLength(20);
        builder.Property(p => p.Address).HasMaxLength(500);
        builder.Property(p => p.OpeningBalance).HasPrecision(18, 2);

        builder.HasIndex(p => p.PartyType);
        builder.HasIndex(p => p.Name);
    }
}
