using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace WalletPulse.Filters;

public sealed class ValidationFilter : IAsyncActionFilter
{
    private readonly IValidatorFactory _validatorFactory;

    public ValidationFilter(IValidatorFactory validatorFactory)
    {
        _validatorFactory = validatorFactory;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var (_, value) in context.ActionArguments)
        {
            if (value is null)
            {
                continue;
            }
            var validator = _validatorFactory.GetValidator(value.GetType());
            if (validator is null)
            {
                continue;
            }
            var result = await validator.ValidateAsync(new ValidationContext<object>(value), context.HttpContext.RequestAborted);
            if (!result.IsValid)
            {
                context.Result = new BadRequestObjectResult(new ValidationProblemDetails(
                    result.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())));
                return;
            }
        }
        await next();
    }
}
