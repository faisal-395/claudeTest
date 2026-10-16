using GrainMarket.Domain.Common;

namespace GrainMarket.Domain.Entities;

/// <summary>A Chart of Accounts category (Asset/Liability/Income/Expense/Equity are seeded by
/// default) — kept as a database row instead of a fixed enum so Setup &gt; Account Types can add
/// new ones later without a code change.</summary>
public class AccountTypeDefinition : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? NameUrdu { get; set; }

    /// <summary>True for the five types seeded on first run — purely informational, nothing
    /// currently blocks editing or deleting one.</summary>
    public bool IsSystemType { get; set; }
}
