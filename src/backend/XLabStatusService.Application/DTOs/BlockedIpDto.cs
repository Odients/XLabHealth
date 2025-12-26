namespace XLabStatusService.Application.DTOs;

/// <summary>
/// DTO для заблокированного IP-адреса
/// </summary>
public class BlockedIpDto
{
    public Guid Id { get; set; }
    public string? IpAddress { get; set; }
    public DateTimeOffset? Date { get; set; }
}

