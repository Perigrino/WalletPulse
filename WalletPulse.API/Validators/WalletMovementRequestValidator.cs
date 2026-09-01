using FluentValidation;
using WalletPulse.Contracts.Request;

namespace WalletPulse.Validators;

public class WalletMovementRequestValidator : AbstractValidator<WalletMovementRequest>
{
    public WalletMovementRequestValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0)
            .Must(a => decimal.Round(a, 2) == a).WithMessage("Amount must have at most 2 decimal places.");
        RuleFor(x => x.Reference).MaximumLength(200);
    }
}
