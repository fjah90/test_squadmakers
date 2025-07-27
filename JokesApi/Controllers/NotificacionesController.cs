using JokesApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JokesApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "admin")]
public class NotificacionesController : ControllerBase
{
    private readonly AlertService _alertService;

    public NotificacionesController(AlertService alertService)
    {
        _alertService = alertService;
    }

    public record SendNotificationRequest(string destinatario, string mensaje, string tipoNotificacion);

    [HttpPost("enviar")]
    public async Task<IActionResult> Send([FromBody] SendNotificationRequest req)
    {
        await _alertService.SendAsync(req.destinatario, req.mensaje, req.tipoNotificacion);
        return Ok(new { message = "Notification sent" });
    }
} 