using GrainMarket.Domain.Common;

namespace GrainMarket.Domain.Entities;

/// <summary>Crop/trading season used to group Kachi, Pakki and Voucher records (e.g. "2025-26").</summary>
public class Season : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; } = true;
}
