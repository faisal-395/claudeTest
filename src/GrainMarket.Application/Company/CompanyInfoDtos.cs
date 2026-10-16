namespace GrainMarket.Application.Company;

public record CompanyInfoDto(
    int Id, string NameEnglish, string? NameUrdu, string? MarketName,
    string? Phone, string? Mobile, string? Email, string? NtnNumber, string? ProprietorName);

public record UpdateCompanyInfoRequest(
    string NameEnglish, string? NameUrdu, string? MarketName,
    string? Phone, string? Mobile, string? Email, string? NtnNumber, string? ProprietorName);
