using JokesApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace JokesApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "admin")]
public class NotificacionesController : ControllerBase
{
    private readonly AlertService _alertService;
    private readonly ILogger<NotificacionesController> _logger;

    public NotificacionesController(AlertService alertService, ILogger<NotificacionesController> logger)
    {
        _alertService = alertService;
        _logger = logger;
    }

    public record SendNotificationRequest(string destinatario, string mensaje, string tipoNotificacion);

    [HttpPost("enviar")]
    public async Task<IActionResult> Send([FromBody] SendNotificationRequest req)
    {
        _logger.LogInformation("Sending {Type} notification to {Dest}", req.tipoNotificacion, req.destinatario);
        await _alertService.SendAsync(req.destinatario, req.mensaje, req.tipoNotificacion);
        return Ok(new { message = "Notification sent" });
    }
} 