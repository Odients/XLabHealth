using FluentValidation;
using XLabStatusService.Application.DTOs;

namespace XLabStatusService.Application.Validators;

public class ServiceUpdateDtoValidator : AbstractValidator<ServiceUpdateDto>
{
    public ServiceUpdateDtoValidator()
    {
        // Валидация только если поле передано (не null)
        RuleFor(x => x.Name)
            .NotEmpty().When(x => x.Name != null).WithMessage("Service name cannot be empty")
            .MaximumLength(200).When(x => x.Name != null).WithMessage("Service name must not exceed 200 characters");

        RuleFor(x => x.Description)
            .MaximumLength(1000).When(x => x.Description != null).WithMessage("Description must not exceed 1000 characters");

        RuleFor(x => x.Url)
            .NotEmpty().When(x => x.Url != null).WithMessage("Service URL cannot be empty")
            .MaximumLength(500).When(x => x.Url != null).WithMessage("URL must not exceed 500 characters");

        RuleFor(x => x.CheckInterval)
            .GreaterThan(0).When(x => x.CheckInterval.HasValue).WithMessage("Check interval must be greater than 0")
            .LessThanOrEqualTo(3600).When(x => x.CheckInterval.HasValue).WithMessage("Check interval must not exceed 3600 seconds");

        RuleFor(x => x.Timeout)
            .GreaterThan(0).When(x => x.Timeout.HasValue).WithMessage("Timeout must be greater than 0")
            .LessThanOrEqualTo(60000).When(x => x.Timeout.HasValue).WithMessage("Timeout must not exceed 60000 milliseconds");

        RuleFor(x => x.RetryCount)
            .GreaterThanOrEqualTo(0).When(x => x.RetryCount.HasValue).WithMessage("Retry count must be 0 or greater")
            .LessThanOrEqualTo(10).When(x => x.RetryCount.HasValue).WithMessage("Retry count must not exceed 10");
    }
}

