using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace WalletPulse.Filters;

public sealed class ValidationFilter : IAsyncActionFilter
{
    private readonly IServiceProvider _serviceProvider;

    public ValidationFilter(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var (_, value) in context.ActionArguments)
        {
            if (value is null)
            {
                continue;
            }
            var validateMethod = typeof(ValidatorHelper).GetMethod("Validate")!;
            var generic = validateMethod.MakeGenericMethod(value.GetType());
            var result = (ValidationResult)generic.Invoke(null, new object[] { _serviceProvider, value })!;
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

internal static class ValidatorHelper
{
    public static ValidationResult Validate<T>(IServiceProvider sp, T value)
    {
        var validator = sp.GetService(typeof(IValidator<T>));
        if (validator is null)
        {
            return new ValidationResult();
        }
        return ((IValidator<T>)validator).Validate(value);
    }
}
