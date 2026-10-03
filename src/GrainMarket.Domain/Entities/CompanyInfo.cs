using GrainMarket.Domain.Common;

namespace GrainMarket.Domain.Entities;

/// <summary>Business letterhead shown on printed receipts/reports — a single row, edited from
/// Setup &gt; Company Info, never created or deleted through the UI.</summary>
public class CompanyInfo : BaseEntity
{
    public string NameEnglish { get; set; } = string.Empty;
    public string? NameUrdu { get; set; }
    public string? MarketName { get; set; }
    public string? Phone { get; set; }
    public string? Mobile { get; set; }
    public string? Email { get; set; }
    public string? NtnNumber { get; set; }
    public string? ProprietorName { get; set; }
}
