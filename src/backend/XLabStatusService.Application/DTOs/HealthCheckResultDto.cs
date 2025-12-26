using XLabStatusService.Core.Enums;

namespace XLabStatusService.Application.DTOs;

/// <summary>
/// DTO для результата проверки здоровья
/// </summary>
public class HealthCheckResultDto
{
    public Guid Id { get; set; }
    public Guid ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public HealthStatus Status { get; set; }
    public int ResponseTime { get; set; }
    public string? Message { get; set; }
    public string? Exception { get; set; }
    public DateTime CheckedAt { get; set; }
    public string? Metadata { get; set; }
}

