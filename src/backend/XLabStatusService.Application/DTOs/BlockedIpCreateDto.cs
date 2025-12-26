namespace XLabStatusService.Application.DTOs;

/// <summary>
/// DTO для создания записи о заблокированном IP-адресе
/// </summary>
public class BlockedIpCreateDto
{
    public string IpAddress { get; set; } = string.Empty;
}

