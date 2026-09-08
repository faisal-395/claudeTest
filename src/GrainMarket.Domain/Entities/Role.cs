using GrainMarket.Domain.Common;

namespace GrainMarket.Domain.Entities;

public class Role : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? NameUrdu { get; set; }
    public bool IsSystemRole { get; set; }

    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<RolePermission> Permissions { get; set; } = new List<RolePermission>();
    public ICollection<ChartOfAccountRole> AllowedAccounts { get; set; } = new List<ChartOfAccountRole>();
}

/// <summary>Application menu modules that permissions can be granted for.</summary>
public enum ModuleName
{
    Dashboard = 1,
    Kachi = 2,
    Pakki = 3,
    DualInvoice = 4,
    SaleInvoice = 5,
    Purchase = 6,
    Journal = 7,
    Recovery = 8,
    Payment = 9,
    Receipt = 10,
    Expense = 11,
    Ledger = 12,
    Reports = 13,
    Trading = 14,
    SetupProducts = 15,
    SetupParty = 16,
    SetupChartOfAccounts = 17,
    SetupDeductionRules = 18,
    SetupUnitConversions = 19,
    SetupUsersRoles = 20,
    Backup = 21,
    SetupSeasons = 22
}

public class RolePermission : BaseEntity
{
    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;
    public ModuleName Module { get; set; }
    public bool CanView { get; set; }
    public bool CanCreate { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
}
