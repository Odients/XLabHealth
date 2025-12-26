using XLabStatusService.Core.Enums;

namespace XLabStatusService.Application.DTOs;

/// <summary>
/// DTO для создания нового сервиса
/// </summary>
public class ServiceCreateDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public ServiceType Type { get; set; }
    public int CheckInterval { get; set; }
    public int Timeout { get; set; }
    public int RetryCount { get; set; }
    public bool IsEnabled { get; set; }
    public bool IsPublic { get; set; }
    public ServiceConfigurationDto? Configuration { get; set; }
}

/// <summary>
/// DTO для конфигурации сервиса
/// </summary>
public class ServiceConfigurationDto
{
    public string CheckType { get; set; } = string.Empty;
    public string? Parameters { get; set; }
    public string? Headers { get; set; }
    public int? ExpectedStatusCode { get; set; }
    public string? ExpectedResponse { get; set; }
}

