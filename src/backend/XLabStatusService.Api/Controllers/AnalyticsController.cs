using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XLabStatusService.Application.DTOs;
using XLabStatusService.Application.Services;

namespace XLabStatusService.Api.Controllers;

/// <summary>
/// Контроллер для аналитики и статистики
/// </summary>
[ApiController]
[Route("api/analytics")]
[Authorize(Roles = "Admin,Viewer")]
public class AnalyticsController : ControllerBase
{
    private readonly AnalyticsService _analyticsService;
    private readonly ILogger<AnalyticsController> _logger;

    public AnalyticsController(
        AnalyticsService analyticsService,
        ILogger<AnalyticsController> logger)
    {
        _analyticsService = analyticsService;
        _logger = logger;
    }

    /// <summary>
    /// Получить аналитику за период
    /// </summary>
    /// <param name="period">Период: 24h, 7d, 1y</param>
    /// <returns>Аналитика за период</returns>
    [HttpGet]
    public async Task<ActionResult<AnalyticsDto>> GetAnalytics(
        [FromQuery] string period = "7d",
        CancellationToken cancellationToken = default)
    {
        try
        {
            var analytics = await _analyticsService.GetAnalyticsAsync(period, cancellationToken);
            return Ok(analytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting analytics for period {Period}", period);
            return StatusCode(500, new { error = "Ошибка при получении аналитики", message = ex.Message });
        }
    }
}

