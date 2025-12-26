namespace XLabStatusService.Application.DTOs;

/// <summary>
/// DTO для webhook
/// </summary>
public class WebhookDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public string? Events { get; set; }
    public Guid? ServiceId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

