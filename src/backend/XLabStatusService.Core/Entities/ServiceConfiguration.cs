namespace XLabStatusService.Core.Entities;

/// <summary>
/// Конфигурация проверки сервиса
/// </summary>
public class ServiceConfiguration
{
    public Guid Id { get; set; }
    public Guid ServiceId { get; set; }
    public string CheckType { get; set; } = string.Empty;
    public string? Parameters { get; set; } // JSON
    public string? Headers { get; set; } // JSON для HTTP
    public int? ExpectedStatusCode { get; set; } // для HTTP
    public string? ExpectedResponse { get; set; } // опционально

    // Navigation property
    public virtual Service Service { get; set; } = null!;
}

