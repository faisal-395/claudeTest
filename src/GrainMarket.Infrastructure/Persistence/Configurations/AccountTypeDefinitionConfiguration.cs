using GrainMarket.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GrainMarket.Infrastructure.Persistence.Configurations;

public class AccountTypeDefinitionConfiguration : IEntityTypeConfiguration<AccountTypeDefinition>
{
    public void Configure(EntityTypeBuilder<AccountTypeDefinition> builder)
    {
        builder.Property(t => t.Name).IsRequired().HasMaxLength(100);
        builder.Property(t => t.NameUrdu).HasMaxLength(100);
        builder.HasIndex(t => t.Name).IsUnique();
    }
}
