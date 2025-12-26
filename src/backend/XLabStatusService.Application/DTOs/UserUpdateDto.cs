namespace XLabStatusService.Application.DTOs;

/// <summary>
/// DTO для обновления пользователя
/// </summary>
public class UserUpdateDto
{
    public string? Username { get; set; }
    public string? Email { get; set; }
    public string? Password { get; set; }
    public string? Role { get; set; } // Admin, Viewer
    public bool? IsActive { get; set; }
}

