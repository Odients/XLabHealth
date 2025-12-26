namespace XLabStatusService.Core.Entities;

/// <summary>
/// Токен для обновления сессии пользователя
/// </summary>
public class RefreshToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsRevoked { get; set; }

    // Navigation property
    public virtual User User { get; set; } = null!;
}

