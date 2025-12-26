namespace XLabStatusService.Core.Entities;

/// <summary>
/// Заблокированный IP-адрес
/// </summary>
public class BlockedIp
{
    public Guid Id { get; set; }
    public string? IpAddress { get; set; }
    public DateTimeOffset? Date { get; set; }
}

