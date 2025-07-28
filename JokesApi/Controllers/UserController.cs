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

    public record RegisterRequest(
        [Required] string Name,
        [Required, EmailAddress] string Email,
        [Required, MinLength(6)] string Password,
        string? Role);

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
    ///   "password": "Password123!",
    ///   "role": "admin"   // opcional: "user" (por defecto) o "admin"
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

        var userRole = (request.Role?.ToLower() == "admin") ? "admin" : "user";

        var user = new JokesApi.Entities.User
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = userRole
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

        return CreatedAtAction(nameof(GetUserById), new { id = user.Id }, response);
    }

    /// <summary>
    /// Promotes an existing user to administrator.
    /// </summary>
    /// <param name="id">User ID.</param>
    /// <response code="204">User promoted.</response>
    /// <response code="404">User not found.</response>
    [Authorize(Roles = "admin")]
    [HttpPost("{id:guid}/promote")]
    public async Task<IActionResult> Promote(Guid id)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user is null) return NotFound();

        user.Role = "admin";
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// Returns the list of all registered users. Accessible only to administrators.
    /// </summary>
    /// <response code="200">List of users returned.</response>
    [Authorize(Roles = "admin")]
    [HttpGet]
    public async Task<IActionResult> GetAllUsers()
    {
        var users = await _db.Users.AsNoTracking()
            .Select(u => new { u.Id, u.Name, u.Email, u.Role })
            .ToListAsync();
        return Ok(users);
    }

    /// <summary>
    /// Returns a single user by ID. Accessible only to administrators.
    /// </summary>
    /// <param name="id">User ID.</param>
    /// <response code="200">User data returned.</response>
    /// <response code="404">User not found.</response>
    [Authorize(Roles = "admin")]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetUserById(Guid id)
    {
        var user = await _db.Users.AsNoTracking()
            .Select(u => new { u.Id, u.Name, u.Email, u.Role })
            .FirstOrDefaultAsync(u => u.Id == id);
        return user is null ? NotFound() : Ok(user);
    }

    /// <summary>
    /// Deletes an existing user. Only administrators can perform this action.
    /// </summary>
    /// <param name="id">User ID.</param>
    /// <response code="204">User deleted.</response>
    /// <response code="404">User not found.</response>
    [Authorize(Roles = "admin")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user is null) return NotFound();

        _db.Users.Remove(user);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    public record UpdateUserRequest([Required] string Name, string? Role);

    /// <summary>
    /// Updates name and/or role of an existing user. Only administrators can perform this action.
    /// </summary>
    /// <param name="id">User ID.</param>
    /// <param name="req">Fields to update.</param>
    /// <response code="204">User updated.</response>
    /// <response code="404">User not found.</response>
    [Authorize(Roles = "admin")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserRequest req)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user is null) return NotFound();

        user.Name = req.Name;
        if (!string.IsNullOrWhiteSpace(req.Role))
            user.Role = req.Role!.ToLower() == "admin" ? "admin" : "user";

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [Authorize(Roles = "admin")]
    [HttpGet("/api/admin")]
    public IActionResult AdminEndpoint()
    {
        return Ok(new { message = "Welcome, admin!" });
    }
} 