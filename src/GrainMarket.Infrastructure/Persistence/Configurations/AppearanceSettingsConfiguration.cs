using GrainMarket.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GrainMarket.Infrastructure.Persistence.Configurations;

public class AppearanceSettingsConfiguration : IEntityTypeConfiguration<AppearanceSettings>
{
    public void Configure(EntityTypeBuilder<AppearanceSettings> builder)
    {
        builder.Property(a => a.PrimaryColor).IsRequired().HasMaxLength(7);
        builder.Property(a => a.AccentColor).IsRequired().HasMaxLength(7);
        builder.Property(a => a.SurfaceColor).IsRequired().HasMaxLength(7);
        builder.Property(a => a.PanelColor).IsRequired().HasMaxLength(7);
        builder.Property(a => a.InputBackgroundColor).IsRequired().HasMaxLength(7);
        builder.Property(a => a.InputBorderColor).IsRequired().HasMaxLength(7);
        builder.Property(a => a.GridHeaderColor).IsRequired().HasMaxLength(7);

        builder.HasIndex(a => a.Scope).IsUnique();
    }
}
