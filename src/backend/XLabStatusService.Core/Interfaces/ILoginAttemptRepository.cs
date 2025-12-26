namespace XLabStatusService.Core.Interfaces;

/// <summary>
/// Репозиторий для отслеживания попыток входа
/// </summary>
public interface ILoginAttemptRepository
{
    /// <summary>
    /// Добавить запись о попытке входа
    /// </summary>
    /// <param name="attempt">Попытка входа</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Созданная запись</returns>
    Task<Entities.LoginAttempt> CreateAsync(Entities.LoginAttempt attempt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить количество неудачных попыток входа с IP-адреса за указанный период
    /// </summary>
    /// <param name="ipAddress">IP-адрес</param>
    /// <param name="since">Время начала периода</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Количество неудачных попыток</returns>
    Task<int> GetFailedAttemptsCountByIpAsync(string ipAddress, DateTime since, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить количество неудачных попыток входа для пользователя за указанный период
    /// </summary>
    /// <param name="username">Имя пользователя</param>
    /// <param name="since">Время начала периода</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Количество неудачных попыток</returns>
    Task<int> GetFailedAttemptsCountByUsernameAsync(string username, DateTime since, CancellationToken cancellationToken = default);

    /// <summary>
    /// Удалить старые записи о попытках входа (старше указанной даты)
    /// </summary>
    /// <param name="before">Дата, до которой удалять записи</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Количество удаленных записей</returns>
    Task<int> DeleteOldAttemptsAsync(DateTime before, CancellationToken cancellationToken = default);
}

