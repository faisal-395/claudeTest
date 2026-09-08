using GrainMarket.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GrainMarket.Infrastructure.Persistence.Configurations;

public class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
{
    public void Configure(EntityTypeBuilder<Expense> builder)
    {
        builder.Property(e => e.ExpenseNo).IsRequired().HasMaxLength(30);
        builder.HasIndex(e => e.ExpenseNo).IsUnique();
        builder.Property(e => e.Amount).HasPrecision(18, 2);
        builder.Property(e => e.Description).HasMaxLength(500);

        builder.HasOne(e => e.ExpenseAccount).WithMany().HasForeignKey(e => e.ExpenseAccountId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.PaidFromAccount).WithMany().HasForeignKey(e => e.PaidFromAccountId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class RecoveryNoteConfiguration : IEntityTypeConfiguration<RecoveryNote>
{
    public void Configure(EntityTypeBuilder<RecoveryNote> builder)
    {
        builder.Property(n => n.Note).IsRequired().HasMaxLength(1000);
        builder.Property(n => n.PromisedAmount).HasPrecision(18, 2);
        builder.HasOne(n => n.Party).WithMany().HasForeignKey(n => n.PartyId).OnDelete(DeleteBehavior.Restrict);
    }
}
