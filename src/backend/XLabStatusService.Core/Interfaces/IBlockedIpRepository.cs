namespace XLabStatusService.Core.Interfaces;

/// <summary>
/// Репозиторий для проверки заблокированных IP-адресов
/// </summary>
public interface IBlockedIpRepository
{
    /// <summary>
    /// Проверить, заблокирован ли IP-адрес
    /// </summary>
    /// <param name="ipAddress">IP-адрес для проверки</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>True, если IP заблокирован, иначе False</returns>
    Task<bool> IsBlockedAsync(string ipAddress, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить все заблокированные IP-адреса
    /// </summary>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Список заблокированных IP-адресов</returns>
    Task<IEnumerable<Entities.BlockedIp>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить заблокированный IP по адресу
    /// </summary>
    /// <param name="ipAddress">IP-адрес</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Запись о заблокированном IP или null</returns>
    Task<Entities.BlockedIp?> GetByIpAddressAsync(string ipAddress, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить заблокированный IP по ID
    /// </summary>
    /// <param name="id">ID записи</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Заблокированный IP или null</returns>
    Task<Entities.BlockedIp?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Добавить IP-адрес в блок-лист
    /// </summary>
    /// <param name="ipAddress">IP-адрес для блокировки</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Созданная запись о заблокированном IP</returns>
    Task<Entities.BlockedIp> AddAsync(string ipAddress, CancellationToken cancellationToken = default);

    /// <summary>
    /// Удалить IP-адрес из блок-листа
    /// </summary>
    /// <param name="id">ID записи</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>True, если запись удалена, иначе False</returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

