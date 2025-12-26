namespace XLabStatusService.Application.DTOs;

/// <summary>
/// DTO для обновления webhook
/// </summary>
public class WebhookUpdateDto
{
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? Secret { get; set; }
    public bool IsEnabled { get; set; }
    public string? Events { get; set; }
    public Guid? ServiceId { get; set; }
}

