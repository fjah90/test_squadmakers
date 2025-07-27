using JokesApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JokesApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsuarioController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<UsuarioController> _logger;

    public UsuarioController(AppDbContext db, ILogger<UsuarioController> logger)
    {
        _db = db;
        _logger = logger;
    }

    [Authorize(Roles = "user,admin")]
    [HttpGet]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userIdStr = User.FindFirst("sub")?.Value;
        if (!Guid.TryParse(userIdStr, out var userId))
        {
            return Unauthorized();
        }

        var user = await _db.Users.AsNoTracking()
            .Select(u => new { u.Id, u.Name, u.Email, u.Role })
            .FirstOrDefaultAsync(u => u.Id == userId);

        return Ok(user);
    }

    [Authorize(Roles = "admin")]
    [HttpGet("/api/admin")]
    public IActionResult AdminEndpoint()
    {
        return Ok(new { message = "Welcome, admin!" });
    }
} 