using FluentValidation;
using GrainMarket.Application.Common.Exceptions;
using Microsoft.AspNetCore.Mvc.Filters;

namespace GrainMarket.Api.Middleware;

/// <summary>
/// Runs the FluentValidation validator (if any is registered) for every action argument before the
/// action executes. This is the guard behind "no total renders until rate, weight, and every
/// deduction input are valid numbers" — invalid requests never reach a service method.
/// </summary>
public class ValidationActionFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null) continue;

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
            if (context.HttpContext.RequestServices.GetService(validatorType) is not IValidator validator) continue;

            var validationContext = new ValidationContext<object>(argument);
            var result = await validator.ValidateAsync(validationContext);
            if (!result.IsValid)
            {
                throw new ValidationAppException(result.Errors);
            }
        }

        await next();
    }
}
