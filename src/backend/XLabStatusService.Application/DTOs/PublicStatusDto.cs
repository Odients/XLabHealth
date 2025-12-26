using XLabStatusService.Core.Enums;

namespace XLabStatusService.Application.DTOs;

/// <summary>
/// DTO для публичного статуса системы
/// </summary>
public class PublicStatusDto
{
    public HealthStatus Status { get; set; }
    public int TotalServices { get; set; }
    public int HealthyServices { get; set; }
    public int DegradedServices { get; set; }
    public int UnhealthyServices { get; set; }
    public DateTime? LastUpdated { get; set; }
    public double AvailabilityPercentage { get; set; }
}

