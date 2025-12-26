namespace XLabStatusService.Core.Entities;

/// <summary>
/// Представляет сервис, который мониторится
/// </summary>
public class Service
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty; // endpoint для проверки
    public Enums.ServiceType Type { get; set; }
    public int CheckInterval { get; set; } // секунды
    public int Timeout { get; set; } // миллисекунды
    public int RetryCount { get; set; }
    public bool IsEnabled { get; set; }
    public bool IsPublic { get; set; } // отображается ли в публичном API
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public virtual ICollection<HealthCheckResult> HealthCheckResults { get; set; } = new List<HealthCheckResult>();
    public virtual ServiceConfiguration? Configuration { get; set; }
}

