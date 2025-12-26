namespace XLabStatusService.Application.DTOs;

/// <summary>
/// DTO для создания нового пользователя
/// </summary>
public class UserCreateDto
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty; // Admin, Viewer
    public bool IsActive { get; set; } = true;
}

