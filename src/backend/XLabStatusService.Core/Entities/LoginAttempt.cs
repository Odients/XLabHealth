namespace XLabStatusService.Core.Entities;

/// <summary>
/// Запись о попытке входа в систему
/// </summary>
public class LoginAttempt
{
    /// <summary>
    /// Уникальный идентификатор записи
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// IP-адрес, с которого была произведена попытка входа
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Имя пользователя, для которого была произведена попытка входа
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Указывает, была ли попытка входа успешной
    /// </summary>
    public bool IsSuccessful { get; set; }

    /// <summary>
    /// Дата и время попытки входа
    /// </summary>
    public DateTime AttemptedAt { get; set; }

    /// <summary>
    /// Причина неудачи (если попытка была неудачной)
    /// </summary>
    public string? FailureReason { get; set; }
}

