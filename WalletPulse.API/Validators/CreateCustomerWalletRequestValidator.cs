using FluentValidation;
using WalletPulse.Contracts.Request;

namespace WalletPulse.Validators;

public class CreateCustomerWalletRequestValidator : AbstractValidator<CreateCustomerWalletRequest>
{
    private static readonly string[] AllowedTypes = ["momo", "card", "bank"];
    private static readonly string[] AllowedSchemes = ["MTN", "Telecel", "AirtelTigo", "VISA", "Mastercard"];

    public CreateCustomerWalletRequestValidator()
    {
        RuleFor(x => x.WalletName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Type).NotEmpty().Must(t => AllowedTypes.Contains(t))
            .WithMessage("Type must be one of: momo, card, bank.");
        RuleFor(x => x.AccountNumber).NotEmpty().MaximumLength(30);
        RuleFor(x => x.AccountScheme).NotEmpty().Must(s => AllowedSchemes.Contains(s))
            .WithMessage("AccountScheme must be one of: MTN, Telecel, AirtelTigo, VISA, Mastercard.");
        RuleFor(x => x.Owner).NotEmpty().MaximumLength(100);
        RuleFor(x => x.CustomerId).NotEmpty();
    }
}
