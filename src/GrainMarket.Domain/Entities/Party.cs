using GrainMarket.Domain.Common;
using GrainMarket.Domain.Enums;

namespace GrainMarket.Domain.Entities;

public class Party : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? NameUrdu { get; set; }
    public PartyType PartyType { get; set; }
    public string? Phone { get; set; }
    public string? Cnic { get; set; }
    public string? Address { get; set; }
    public decimal OpeningBalance { get; set; }
    public BalanceSide OpeningBalanceType { get; set; } = BalanceSide.Debit;
    public bool IsActive { get; set; } = true;

    public ICollection<Kachi> Kachis { get; set; } = new List<Kachi>();
    public ICollection<Pakki> PakkisAsBuyer { get; set; } = new List<Pakki>();
}
