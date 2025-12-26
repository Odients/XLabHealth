namespace XLabStatusService.Core.Entities;

/// <summary>
/// Webhook для отправки уведомлений при изменении состояния сервиса
/// </summary>
public class Webhook
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? Secret { get; set; } // для подписи запросов
    public bool IsEnabled { get; set; }
    public string? Events { get; set; } // JSON массив событий (ServiceStatusChanged, ServiceChecked и т.д.)
    public Guid? ServiceId { get; set; } // если null, то для всех сервисов
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation property
    public virtual Service? Service { get; set; }
}

