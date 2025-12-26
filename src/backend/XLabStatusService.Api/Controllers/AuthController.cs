using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using XLabStatusService.Api.Middleware;
using XLabStatusService.Application.DTOs;
using XLabStatusService.Application.Services;

namespace XLabStatusService.Api.Controllers;

/// <summary>
/// Контроллер для аутентификации
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(AuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Вход в систему
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.AuthPolicy)]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginDto dto, CancellationToken cancellationToken)
    {
        // Получаем IP-адрес клиента
        var ipAddress = RateLimitingExtensions.GetClientIpAddress(HttpContext);

        try
        {
            var result = await _authService.LoginAsync(dto, ipAddress, cancellationToken);
            if (result == null)
            {
                return Unauthorized(new { error = "Invalid username or password" });
            }

            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            // Ошибка из-за превышения лимита попыток или блокировки IP
            return StatusCode(429, new { error = "TooManyAttempts", message = ex.Message });
        }
    }

    /// <summary>
    /// Обновить токен
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.AuthPolicy)]
    public async Task<ActionResult<AuthResponseDto>> Refresh([FromBody] RefreshTokenDto dto, CancellationToken cancellationToken)
    {
        var result = await _authService.RefreshTokenAsync(dto.RefreshToken, cancellationToken);
        if (result == null)
        {
            return Unauthorized(new { error = "Invalid or expired refresh token" });
        }

        return Ok(result);
    }

    /// <summary>
    /// Выход из системы
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenDto dto, CancellationToken cancellationToken)
    {
        await _authService.LogoutAsync(dto.RefreshToken, cancellationToken);
        return NoContent();
    }
}

