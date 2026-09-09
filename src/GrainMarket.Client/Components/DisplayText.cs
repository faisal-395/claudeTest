using GrainMarket.Application.ChartOfAccounts;
using GrainMarket.Application.Parties;
using GrainMarket.Application.Products;

namespace GrainMarket.Client.Components;

/// <summary>Shared "English — Urdu" / "code - name" label formatting for SearchSelect pickers,
/// so every picker of the same entity type reads the same way.</summary>
public static class DisplayText
{
    public static string Party(PartyDto p) => string.IsNullOrEmpty(p.NameUrdu) ? p.Name : $"{p.Name} — {p.NameUrdu}";
    public static string Product(ProductDto p) => string.IsNullOrEmpty(p.NameUrdu) ? p.Name : $"{p.Name} — {p.NameUrdu}";
    public static string Account(ChartOfAccountDto a) => $"{a.Code} - {a.Name}";
}
