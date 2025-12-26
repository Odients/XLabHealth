using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace XLabStatusService.Api.Filters;

/// <summary>
/// Action filter for automatic FluentValidation model validation
/// </summary>
public class FluentValidationFilter : IAsyncActionFilter
{
    private readonly IServiceProvider _serviceProvider;

    public FluentValidationFilter(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // Validate all parameters that have validators
        foreach (var parameter in context.ActionDescriptor.Parameters)
        {
            if (context.ActionArguments.TryGetValue(parameter.Name, out var argument) && argument != null)
            {
                var argumentType = argument.GetType();
                var validatorType = typeof(IValidator<>).MakeGenericType(argumentType);
                
                if (_serviceProvider.GetService(validatorType) is IValidator validator)
                {
                    var validationContextType = typeof(ValidationContext<>).MakeGenericType(argumentType);
                    var validationContext = (IValidationContext)Activator.CreateInstance(validationContextType, argument)!;
                    var validationResult = await validator.ValidateAsync(validationContext, context.HttpContext.RequestAborted);
                    
                    if (!validationResult.IsValid)
                    {
                        foreach (var error in validationResult.Errors)
                        {
                            context.ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
                        }
                    }
                }
            }
        }

        // If validation failed, return BadRequest
        if (!context.ModelState.IsValid)
        {
            context.Result = new BadRequestObjectResult(context.ModelState);
            return;
        }

        await next();
    }
}

