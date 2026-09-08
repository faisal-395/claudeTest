using GrainMarket.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GrainMarket.Infrastructure.Persistence.Configurations;

public class ChartOfAccountConfiguration : IEntityTypeConfiguration<ChartOfAccount>
{
    public void Configure(EntityTypeBuilder<ChartOfAccount> builder)
    {
        builder.Property(a => a.Code).IsRequired().HasMaxLength(30);
        builder.Property(a => a.Name).IsRequired().HasMaxLength(200);
        builder.Property(a => a.NameUrdu).HasMaxLength(200);
        builder.HasIndex(a => a.Code).IsUnique();

        builder.HasOne(a => a.ParentAccount).WithMany().HasForeignKey(a => a.ParentAccountId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class ChartOfAccountRoleConfiguration : IEntityTypeConfiguration<ChartOfAccountRole>
{
    public void Configure(EntityTypeBuilder<ChartOfAccountRole> builder)
    {
        builder.HasKey(x => new { x.ChartOfAccountId, x.RoleId });
        builder.HasOne(x => x.ChartOfAccount).WithMany(a => a.AllowedRoles).HasForeignKey(x => x.ChartOfAccountId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Role).WithMany(r => r.AllowedAccounts).HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Restrict);

        // Matches ChartOfAccount's own soft-delete filter — required so EF doesn't warn about a
        // required relationship to a filtered entity with no filter of its own on this side.
        builder.HasQueryFilter(x => !x.ChartOfAccount.IsDeleted);
    }
}
