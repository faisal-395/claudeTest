using GrainMarket.Domain.Common;
using GrainMarket.Domain.Enums;

namespace GrainMarket.Domain.Entities;

public class ChartOfAccount : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? NameUrdu { get; set; }
    public AccountType AccountType { get; set; }
    public int? ParentAccountId { get; set; }
    public ChartOfAccount? ParentAccount { get; set; }

    /// <summary>When true, only roles listed in AllowedRoles may view/select this account (enforced server-side).</summary>
    public bool IsProtected { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<ChartOfAccountRole> AllowedRoles { get; set; } = new List<ChartOfAccountRole>();
}

/// <summary>Join entity: which roles may see a protected account.</summary>
public class ChartOfAccountRole
{
    public int ChartOfAccountId { get; set; }
    public ChartOfAccount ChartOfAccount { get; set; } = null!;
    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;
}
