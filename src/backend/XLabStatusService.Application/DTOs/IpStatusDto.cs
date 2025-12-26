namespace XLabStatusService.Application.DTOs;

/// <summary>
/// DTO для статуса IP-адреса клиента
/// </summary>
public class IpStatusDto
{
    public string? IpAddress { get; set; }
    public bool IsBlocked { get; set; }
    public DateTimeOffset? BlockedDate { get; set; }
}

