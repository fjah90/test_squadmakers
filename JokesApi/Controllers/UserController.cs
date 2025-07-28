using JokesApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using JokesApi.Services;
using Swashbuckle.AspNetCore.Annotations;

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
    [SwaggerOperation(
        Summary = "Registra un nuevo usuario",
        Description = "Crea una nueva cuenta de usuario con los datos proporcionados"
    )]
    [SwaggerResponse(201, "Usuario creado correctamente", typeof(object))]
    [SwaggerResponse(409, "El email ya está registrado")]
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
    [SwaggerOperation(
        Summary = "Promueve a un usuario a administrador",
        Description = "Cambia el rol de un usuario existente a administrador"
    )]
    [SwaggerResponse(204, "Usuario promovido correctamente")]
    [SwaggerResponse(404, "Usuario no encontrado")]
    public async Task<IActionResult> Promote(Guid id)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user is null) return NotFound();

        user.Role = "admin";
        await _db.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Gets all users (admin only).
    /// </summary>
    /// <response code="200">List of users.</response>
    /// <response code="401">Unauthorized.</response>
    /// <response code="403">Forbidden (not admin).</response>
    [Authorize(Roles = "admin")]
    [HttpGet]
    [SwaggerOperation(
        Summary = "Obtiene todos los usuarios",
        Description = "Devuelve una lista con todos los usuarios registrados (solo administradores)"
    )]
    [SwaggerResponse(200, "Lista de usuarios obtenida correctamente", typeof(IEnumerable<object>))]
    [SwaggerResponse(401, "No autenticado")]
    [SwaggerResponse(403, "No autorizado (no es administrador)")]
    public async Task<IActionResult> GetAllUsers()
    {
        var users = await _db.Users
            .AsNoTracking()
            .Select(u => new { u.Id, u.Name, u.Email, u.Role })
            .ToListAsync();

        return Ok(users);
    }

    /// <summary>
    /// Gets a user by ID (admin only).
    /// </summary>
    /// <param name="id">User ID.</param>
    /// <response code="200">User found.</response>
    /// <response code="404">User not found.</response>
    [Authorize(Roles = "admin")]
    [HttpGet("{id:guid}")]
    [SwaggerOperation(
        Summary = "Obtiene un usuario por su ID",
        Description = "Devuelve los datos de un usuario específico (solo administradores)"
    )]
    [SwaggerResponse(200, "Usuario encontrado", typeof(object))]
    [SwaggerResponse(404, "Usuario no encontrado")]
    public async Task<IActionResult> GetUserById(Guid id)
    {
        var user = await _db.Users
            .AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new { u.Id, u.Name, u.Email, u.Role })
            .FirstOrDefaultAsync();

        return user is null ? NotFound() : Ok(user);
    }

    /// <summary>
    /// Deletes a user (admin only).
    /// </summary>
    /// <param name="id">User ID.</param>
    /// <response code="204">User deleted.</response>
    /// <response code="404">User not found.</response>
    [Authorize(Roles = "admin")]
    [HttpDelete("{id:guid}")]
    [SwaggerOperation(
        Summary = "Elimina un usuario",
        Description = "Elimina permanentemente un usuario del sistema (solo administradores)"
    )]
    [SwaggerResponse(204, "Usuario eliminado correctamente")]
    [SwaggerResponse(404, "Usuario no encontrado")]
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
    /// Updates a user (admin only).
    /// </summary>
    /// <param name="id">User ID.</param>
    /// <param name="req">Update data.</param>
    /// <response code="200">User updated.</response>
    /// <response code="404">User not found.</response>
    [Authorize(Roles = "admin")]
    [HttpPut("{id:guid}")]
    [SwaggerOperation(
        Summary = "Actualiza un usuario",
        Description = "Modifica los datos de un usuario existente (solo administradores)"
    )]
    [SwaggerResponse(200, "Usuario actualizado correctamente", typeof(object))]
    [SwaggerResponse(404, "Usuario no encontrado")]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserRequest req)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user is null) return NotFound();

        user.Name = req.Name;
        if (!string.IsNullOrWhiteSpace(req.Role))
        {
            user.Role = req.Role.ToLower() == "admin" ? "admin" : "user";
        }

        await _db.SaveChangesAsync();

        return Ok(new { user.Id, user.Name, user.Email, user.Role });
    }

    /// <summary>
    /// Gets the current user's information.
    /// </summary>
    /// <response code="200">Current user information.</response>
    /// <response code="400">Invalid user identifier in token.</response>
    /// <response code="404">User not found.</response>
    [Authorize]
    [HttpGet("/api/usuario")]
    [SwaggerOperation(
        Summary = "Obtiene información del usuario actual",
        Description = "Devuelve los datos del usuario autenticado"
    )]
    [SwaggerResponse(200, "Información del usuario obtenida correctamente", typeof(object))]
    [SwaggerResponse(400, "Identificador de usuario inválido en el token")]
    [SwaggerResponse(404, "Usuario no encontrado")]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId = User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var id))
        {
            return BadRequest(new { message = "Invalid user identifier in token" });
        }

        var user = await _db.Users.AsNoTracking()
            .Select(u => new { u.Id, u.Name, u.Email, u.Role })
            .FirstOrDefaultAsync(u => u.Id == id);

        return user is null ? NotFound() : Ok(user);
    }

    /// <summary>
    /// Admin-only endpoint for testing authorization.
    /// </summary>
    /// <response code="200">User is admin.</response>
    /// <response code="403">User is not admin.</response>
    [Authorize(Roles = "admin")]
    [HttpGet("/api/admin")]
    [SwaggerOperation(
        Summary = "Endpoint exclusivo para administradores",
        Description = "Endpoint de prueba para verificar autorización de administrador"
    )]
    [SwaggerResponse(200, "Usuario es administrador")]
    [SwaggerResponse(403, "Usuario no es administrador")]
    public IActionResult AdminEndpoint()
    {
        return Ok(new { message = "You are an admin!" });
    }
} 