using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XLabStatusService.Application.DTOs;
using XLabStatusService.Core.Interfaces;

namespace XLabStatusService.Api.Controllers;

/// <summary>
/// Контроллер для управления заблокированными IP-адресами
/// </summary>
[ApiController]
[Route("api/blocked-ips")]
[Authorize(Roles = "Admin")]
public class BlockedIpsController : ControllerBase
{
    private readonly IBlockedIpRepository _blockedIpRepository;
    private readonly ILogger<BlockedIpsController> _logger;

    public BlockedIpsController(
        IBlockedIpRepository blockedIpRepository,
        ILogger<BlockedIpsController> logger)
    {
        _blockedIpRepository = blockedIpRepository;
        _logger = logger;
    }

    /// <summary>
    /// Получить все заблокированные IP-адреса
    /// </summary>
    /// <returns>Список заблокированных IP-адресов</returns>
    /// <response code="200">Возвращает список заблокированных IP-адресов</response>
    /// <response code="401">Если пользователь не авторизован</response>
    /// <response code="403">Если пользователь не имеет роли Admin</response>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<BlockedIpDto>>> GetBlockedIps(CancellationToken cancellationToken)
    {
        var blockedIps = await _blockedIpRepository.GetAllAsync(cancellationToken);
        
        var result = blockedIps.Select(b => new BlockedIpDto
        {
            Id = b.Id,
            IpAddress = b.IpAddress,
            Date = b.Date
        });

        return Ok(result);
    }

    /// <summary>
    /// Получить заблокированный IP по ID
    /// </summary>
    /// <param name="id">ID записи</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Заблокированный IP</returns>
    /// <response code="200">Возвращает заблокированный IP</response>
    /// <response code="404">Если запись не найдена</response>
    /// <response code="401">Если пользователь не авторизован</response>
    /// <response code="403">Если пользователь не имеет роли Admin</response>
    [HttpGet("{id}")]
    public async Task<ActionResult<BlockedIpDto>> GetBlockedIp(Guid id, CancellationToken cancellationToken)
    {
        var blockedIp = await _blockedIpRepository.GetByIdAsync(id, cancellationToken);

        if (blockedIp == null)
        {
            return NotFound(new { error = "Blocked IP not found" });
        }

        var result = new BlockedIpDto
        {
            Id = blockedIp.Id,
            IpAddress = blockedIp.IpAddress,
            Date = blockedIp.Date
        };

        return Ok(result);
    }

    /// <summary>
    /// Добавить IP-адрес в блок-лист
    /// </summary>
    /// <param name="dto">DTO с IP-адресом для блокировки</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Созданная запись о заблокированном IP</returns>
    /// <response code="201">IP-адрес успешно добавлен в блок-лист</response>
    /// <response code="400">Если IP-адрес некорректный</response>
    /// <response code="401">Если пользователь не авторизован</response>
    /// <response code="403">Если пользователь не имеет роли Admin</response>
    [HttpPost]
    public async Task<ActionResult<BlockedIpDto>> CreateBlockedIp([FromBody] BlockedIpCreateDto dto, CancellationToken cancellationToken)
    {
        var blockedIp = await _blockedIpRepository.AddAsync(dto.IpAddress, cancellationToken);

        _logger.LogInformation("IP address {IpAddress} was added to blocklist by user {UserId}", 
            dto.IpAddress, User.Identity?.Name);

        var result = new BlockedIpDto
        {
            Id = blockedIp.Id,
            IpAddress = blockedIp.IpAddress,
            Date = blockedIp.Date
        };

        return CreatedAtAction(nameof(GetBlockedIp), new { id = result.Id }, result);
    }

    /// <summary>
    /// Удалить IP-адрес из блок-листа
    /// </summary>
    /// <param name="id">ID записи</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Результат операции</returns>
    /// <response code="204">IP-адрес успешно удален из блок-листа</response>
    /// <response code="404">Если запись не найдена</response>
    /// <response code="401">Если пользователь не авторизован</response>
    /// <response code="403">Если пользователь не имеет роли Admin</response>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteBlockedIp(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _blockedIpRepository.DeleteAsync(id, cancellationToken);

        if (!deleted)
        {
            return NotFound(new { error = "Blocked IP not found" });
        }

        _logger.LogInformation("Blocked IP {Id} was removed from blocklist by user {UserId}", 
            id, User.Identity?.Name);

        return NoContent();
    }
}

