namespace GrainMarket.Domain.Entities;

/// <summary>Backs sequential invoice/voucher numbering (Kachi "K-000123", Payment "PV-000045", etc.).</summary>
public class NumberSequence
{
    public int Id { get; set; }
    public string Prefix { get; set; } = string.Empty;
    public int LastNumber { get; set; }
}
