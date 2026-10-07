using FluentValidation;

namespace GrainMarket.Application.Company;

public class UpdateCompanyInfoRequestValidator : AbstractValidator<UpdateCompanyInfoRequest>
{
    public UpdateCompanyInfoRequestValidator()
    {
        RuleFor(x => x.NameEnglish).NotEmpty().MaximumLength(200);
        RuleFor(x => x.NameUrdu).MaximumLength(200);
        RuleFor(x => x.MarketName).MaximumLength(200);
        RuleFor(x => x.Phone).MaximumLength(30);
        RuleFor(x => x.Mobile).MaximumLength(30);
        RuleFor(x => x.Email).MaximumLength(150).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.NtnNumber).MaximumLength(50);
        RuleFor(x => x.ProprietorName).MaximumLength(150);
    }
}
