using XLabStatusService.Core.Enums;

namespace XLabStatusService.Application.DTOs;

/// <summary>
/// DTO для сервиса (полная информация для приватных endpoints)
/// </summary>
public class ServiceDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public ServiceType Type { get; set; }
    public int CheckInterval { get; set; }
    public int Timeout { get; set; }
    public int RetryCount { get; set; }
    public bool IsEnabled { get; set; }
    public bool IsPublic { get; set; }
    public HealthStatus? LastStatus { get; set; }
    public DateTime? LastCheckedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ServiceConfigurationDto? Configuration { get; set; }
}

