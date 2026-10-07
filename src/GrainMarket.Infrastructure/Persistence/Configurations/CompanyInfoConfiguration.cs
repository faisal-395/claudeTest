using GrainMarket.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GrainMarket.Infrastructure.Persistence.Configurations;

public class CompanyInfoConfiguration : IEntityTypeConfiguration<CompanyInfo>
{
    public void Configure(EntityTypeBuilder<CompanyInfo> builder)
    {
        builder.Property(c => c.NameEnglish).IsRequired().HasMaxLength(200);
        builder.Property(c => c.NameUrdu).HasMaxLength(200);
        builder.Property(c => c.MarketName).HasMaxLength(200);
        builder.Property(c => c.Phone).HasMaxLength(30);
        builder.Property(c => c.Mobile).HasMaxLength(30);
        builder.Property(c => c.Email).HasMaxLength(150);
        builder.Property(c => c.NtnNumber).HasMaxLength(50);
        builder.Property(c => c.ProprietorName).HasMaxLength(150);
    }
}
