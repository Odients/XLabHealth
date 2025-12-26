namespace XLabStatusService.Core.Entities;

/// <summary>
/// Результат проверки здоровья сервиса
/// </summary>
public class HealthCheckResult
{
    public Guid Id { get; set; }
    public Guid ServiceId { get; set; }
    public Enums.HealthStatus Status { get; set; }
    public int ResponseTime { get; set; } // миллисекунды
    public string? Message { get; set; }
    public string? Exception { get; set; } // если есть
    public DateTime CheckedAt { get; set; }
    public string? Metadata { get; set; } // JSON с дополнительными данными

    // Navigation property
    public virtual Service Service { get; set; } = null!;
}

