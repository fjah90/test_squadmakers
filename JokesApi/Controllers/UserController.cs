using JokesApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using JokesApi.Services;

namespace JokesApi.Controllers;

/// <summary>
/// Manage user registration and profile endpoints.
/// </summary>
[ApiController]
[Route("api/users")]
public class UserController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITokenService _tokenService;
    private readonly ILogger<UserController> _logger;

    public UserController(AppDbContext db, ITokenService tokenService, ILogger<UserController> logger)
    {
        _db = db;
        _tokenService = tokenService;
        _logger = logger;
    }

    public record RegisterRequest([Required] string Name, [Required, EmailAddress] string Email, [Required, MinLength(6)] string Password);

    /// <summary>
    /// Registers a new user. The server automatically sets the role to <c>"user"</c>.
    /// </summary>
    /// <param name="request">Registration data.</param>
    /// <remarks>
    /// Example request:
    /// ```json
    /// {
    ///   "name": "Jane Doe",
    ///   "email": "jane@example.com",
    ///   "password": "Password123!"
    /// }
    /// ```
    /// The response incluye el objeto <c>user</c> (donde se indica <c>role = "user"</c>),
    /// el JWT (<c>token</c>) y el <c>refreshToken</c>.
    /// </remarks>
    /// <response code="201">User created successfully.</response>
    /// <response code="409">Email already registered.</response>
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (await _db.Users.AnyAsync(u => u.Email == request.Email))
        {
            return Conflict(new { message = "Email already registered" });
        }

        var user = new JokesApi.Entities.User
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = "user"
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var pair = _tokenService.CreateTokenPair(user);

        var response = new
        {
            user = new { user.Id, user.Name, user.Email, user.Role },
            token = pair.Token,
            refreshToken = pair.RefreshToken
        };

        return CreatedAtAction(nameof(GetCurrentUser), new { }, response);
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