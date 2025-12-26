namespace XLabStatusService.Application.DTOs;

/// <summary>
/// DTO для обновления токена
/// </summary>
public class RefreshTokenDto
{
    public string RefreshToken { get; set; } = string.Empty;
}

