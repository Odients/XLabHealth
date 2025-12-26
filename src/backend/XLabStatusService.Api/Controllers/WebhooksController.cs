using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XLabStatusService.Application.DTOs;

namespace XLabStatusService.Api.Controllers;

/// <summary>
/// Контроллер для управления webhooks
/// </summary>
[ApiController]
[Route("api/webhooks")]
[Authorize(Roles = "Admin")]
public class WebhooksController : ControllerBase
{
    private readonly ILogger<WebhooksController> _logger;

    public WebhooksController(ILogger<WebhooksController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Получить все webhooks
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<WebhookDto>>> GetWebhooks()
    {
        // TODO: Реализовать получение webhooks
        return Ok(new List<WebhookDto>());
    }

    /// <summary>
    /// Создать webhook
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<WebhookDto>> CreateWebhook([FromBody] WebhookCreateDto dto)
    {
        // TODO: Реализовать создание webhook
        return BadRequest("Not implemented yet");
    }

    /// <summary>
    /// Обновить webhook
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<WebhookDto>> UpdateWebhook(Guid id, [FromBody] WebhookUpdateDto dto)
    {
        // TODO: Реализовать обновление webhook
        return BadRequest("Not implemented yet");
    }

    /// <summary>
    /// Удалить webhook
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteWebhook(Guid id)
    {
        // TODO: Реализовать удаление webhook
        return BadRequest("Not implemented yet");
    }
}

