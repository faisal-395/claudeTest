namespace GrainMarket.Infrastructure.Services;

public class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = "GrainMarket";
    public string Audience { get; set; } = "GrainMarket.Client";
    public int ExpiryHours { get; set; } = 8;
    public int RefreshTokenExpiryDays { get; set; } = 30;
}
