using FluentValidation;

namespace GrainMarket.Application.SaleInvoices;

public class SaleInvoiceLineRequestValidator : AbstractValidator<SaleInvoiceLineRequest>
{
    public SaleInvoiceLineRequestValidator()
    {
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
        RuleFor(x => x.DiscountPercent).InclusiveBetween(0, 100);
    }
}

public class CreateSaleInvoiceRequestValidator : AbstractValidator<CreateSaleInvoiceRequest>
{
    public CreateSaleInvoiceRequestValidator()
    {
        RuleFor(x => x.CustomerId).GreaterThan(0);
        RuleFor(x => x.Lines).NotEmpty().WithMessage("At least one line item is required.");
        RuleForEach(x => x.Lines).SetValidator(new SaleInvoiceLineRequestValidator());
        RuleFor(x => x.ReceivedCash).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PrintFormat).IsInEnum();
        RuleFor(x => x.PrintLanguage).IsInEnum();
    }
}
