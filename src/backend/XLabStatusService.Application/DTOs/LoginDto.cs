namespace XLabStatusService.Application.DTOs;

/// <summary>
/// DTO для входа в систему
/// </summary>
public class LoginDto
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

