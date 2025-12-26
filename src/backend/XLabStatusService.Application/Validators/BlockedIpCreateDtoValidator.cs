using FluentValidation;
using XLabStatusService.Application.DTOs;

namespace XLabStatusService.Application.Validators;

/// <summary>
/// Валидатор для BlockedIpCreateDto
/// </summary>
public class BlockedIpCreateDtoValidator : AbstractValidator<BlockedIpCreateDto>
{
    public BlockedIpCreateDtoValidator()
    {
        RuleFor(x => x.IpAddress)
            .NotEmpty().WithMessage("IP address is required")
            .Must(BeValidIpAddress).WithMessage("Invalid IP address format");
    }

    private static bool BeValidIpAddress(string? ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            return false;
        }

        return System.Net.IPAddress.TryParse(ipAddress, out _);
    }
}

