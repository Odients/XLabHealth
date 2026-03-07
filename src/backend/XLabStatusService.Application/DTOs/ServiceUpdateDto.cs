using XLabStatusService.Core.Enums;

namespace XLabStatusService.Application.DTOs;

/// <summary>
/// DTO для обновления сервиса
/// </summary>
public class ServiceUpdateDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Url { get; set; }
    public ServiceType? Type { get; set; }
    public int? CheckInterval { get; set; }
    public int? Timeout { get; set; }
    public int? RetryCount { get; set; }
    public bool? IsEnabled { get; set; }
    public bool? IsPublic { get; set; }
    public bool? IsCritical { get; set; }
    public ServiceConfigurationDto? Configuration { get; set; }
}

