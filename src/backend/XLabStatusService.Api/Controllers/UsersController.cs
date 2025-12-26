using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XLabStatusService.Application.DTOs;
using XLabStatusService.Application.Services;

namespace XLabStatusService.Api.Controllers;

/// <summary>
/// Контроллер для управления пользователями
/// </summary>
[ApiController]
[Route("api/users")]
[Authorize(Roles = "Admin")]
public class UsersController : ControllerBase
{
    private readonly UserService _userService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(UserService userService, ILogger<UsersController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// Получить всех пользователей
    /// </summary>
    /// <returns>Список пользователей</returns>
    /// <response code="200">Возвращает список пользователей</response>
    /// <response code="401">Если пользователь не авторизован</response>
    /// <response code="403">Если пользователь не имеет роли Admin</response>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers(CancellationToken cancellationToken)
    {
        var users = await _userService.GetAllAsync(cancellationToken);
        return Ok(users);
    }

    /// <summary>
    /// Получить пользователя по ID
    /// </summary>
    /// <param name="id">ID пользователя</param>
    /// <returns>Информация о пользователе</returns>
    /// <response code="200">Возвращает информацию о пользователе</response>
    /// <response code="404">Если пользователь не найден</response>
    /// <response code="401">Если пользователь не авторизован</response>
    /// <response code="403">Если пользователь не имеет роли Admin</response>
    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> GetUser(Guid id, CancellationToken cancellationToken)
    {
        var user = await _userService.GetByIdAsync(id, cancellationToken);
        if (user == null)
        {
            return NotFound(new { error = "User not found" });
        }

        return Ok(user);
    }

    /// <summary>
    /// Создать нового пользователя
    /// </summary>
    /// <param name="dto">Данные для создания пользователя</param>
    /// <returns>Созданный пользователь</returns>
    /// <response code="201">Пользователь успешно создан</response>
    /// <response code="400">Если данные невалидны</response>
    /// <response code="401">Если пользователь не авторизован</response>
    /// <response code="403">Если пользователь не имеет роли Admin</response>
    [HttpPost]
    public async Task<ActionResult<UserDto>> CreateUser([FromBody] UserCreateDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _userService.CreateAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Обновить пользователя
    /// </summary>
    /// <param name="id">ID пользователя</param>
    /// <param name="dto">Данные для обновления пользователя</param>
    /// <returns>Обновленный пользователь</returns>
    /// <response code="200">Пользователь успешно обновлен</response>
    /// <response code="400">Если данные невалидны</response>
    /// <response code="404">Если пользователь не найден</response>
    /// <response code="401">Если пользователь не авторизован</response>
    /// <response code="403">Если пользователь не имеет роли Admin</response>
    [HttpPut("{id}")]
    public async Task<ActionResult<UserDto>> UpdateUser(Guid id, [FromBody] UserUpdateDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _userService.UpdateAsync(id, dto, cancellationToken);
            return Ok(user);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Удалить пользователя
    /// </summary>
    /// <param name="id">ID пользователя</param>
    /// <returns>Результат удаления</returns>
    /// <response code="204">Пользователь успешно удален</response>
    /// <response code="404">Если пользователь не найден</response>
    /// <response code="401">Если пользователь не авторизован</response>
    /// <response code="403">Если пользователь не имеет роли Admin</response>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _userService.DeleteAsync(id, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }
}

