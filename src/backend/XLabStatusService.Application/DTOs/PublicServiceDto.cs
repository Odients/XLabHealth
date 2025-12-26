using XLabStatusService.Core.Enums;

namespace XLabStatusService.Application.DTOs;

/// <summary>
/// DTO для публичного отображения сервиса (минимальная информация)
/// </summary>
public class PublicServiceDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public HealthStatus Status { get; set; }
    public DateTime? LastCheckedAt { get; set; }
}

