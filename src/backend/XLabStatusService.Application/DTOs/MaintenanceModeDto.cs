namespace XLabStatusService.Application.DTOs;

/// <summary>
/// DTO для режима обслуживания
/// </summary>
public class MaintenanceModeDto
{
    public Guid Id { get; set; }
    public bool IsEnabled { get; set; }
    public string? Message { get; set; }
    public DateTime? ScheduledStartTime { get; set; }
    public DateTime? ScheduledEndTime { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public Guid? StartedByUserId { get; set; }
    public Guid? EndedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// DTO для включения режима обслуживания
/// </summary>
public class MaintenanceModeEnableDto
{
    public string? Message { get; set; }
    public DateTime? ScheduledStartTime { get; set; }
    public DateTime? ScheduledEndTime { get; set; }
}

