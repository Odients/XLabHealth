using FluentValidation;
using XLabStatusService.Application.DTOs;
using XLabStatusService.Core.Enums;

namespace XLabStatusService.Application.Validators;

public class ServiceCreateDtoValidator : AbstractValidator<ServiceCreateDto>
{
    public ServiceCreateDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Service name is required")
            .MaximumLength(200).WithMessage("Service name must not exceed 200 characters");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters");

        RuleFor(x => x.Url)
            .NotEmpty().WithMessage("Service URL is required")
            .MaximumLength(500).WithMessage("URL must not exceed 500 characters");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Invalid service type");

        RuleFor(x => x.CheckInterval)
            .GreaterThan(0).WithMessage("Check interval must be greater than 0")
            .LessThanOrEqualTo(3600).WithMessage("Check interval must not exceed 3600 seconds");

        RuleFor(x => x.Timeout)
            .GreaterThan(0).WithMessage("Timeout must be greater than 0")
            .LessThanOrEqualTo(60000).WithMessage("Timeout must not exceed 60000 milliseconds");

        RuleFor(x => x.RetryCount)
            .GreaterThanOrEqualTo(0).WithMessage("Retry count must be 0 or greater")
            .LessThanOrEqualTo(10).WithMessage("Retry count must not exceed 10");
    }
}

