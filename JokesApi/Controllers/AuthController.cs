using BCrypt.Net;
using JokesApi.Data;
using JokesApi.Entities;
using JokesApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using AspNet.Security.OAuth.GitHub;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.RateLimiting;
using Swashbuckle.AspNetCore.Annotations;

namespace JokesApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[SwaggerTag("Authentication endpoints for login, refresh tokens and OAuth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(AppDbContext db, ITokenService tokenService, ILogger<AuthController> logger)
    {
        _db = db;
        _tokenService = tokenService;
        _logger = logger;
    }

    public record LoginRequest([Required, EmailAddress] string Email, [Required] string Password);
    public record LoginResponse(string Token, string RefreshToken);

    [HttpPost("login")]
    [EnableRateLimiting("login")]
    [SwaggerOperation(
        Summary = "Authenticate a user with email and password",
        Description = "Returns JWT token and refresh token upon successful authentication. Rate limited to prevent brute force attacks."
    )]
    [SwaggerResponse(StatusCodes.Status200OK, "Authentication successful", typeof(LoginResponse))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Invalid credentials")]
    [SwaggerResponse(StatusCodes.Status429TooManyRequests, "Too many login attempts")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (request is null)
            return BadRequest("Request cannot be null");

        _logger.LogInformation("Login attempt for {Email}", request.Email);
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Invalid credentials for {Email}", request.Email);
            return Unauthorized(new { message = "Invalid credentials" });
        }

        var pair = _tokenService.CreateTokenPair(user);
        return Ok(new LoginResponse(pair.Token, pair.RefreshToken));
    }

    [HttpGet("external/google-login")]
    [SwaggerOperation(
        Summary = "Initiate Google OAuth login flow",
        Description = "Redirects to Google for authentication"
    )]
    [SwaggerResponse(StatusCodes.Status302Found, "Redirect to Google authentication")]
    public IActionResult GoogleLogin(string? returnUrl = "/")
    {
        var properties = new AuthenticationProperties { RedirectUri = Url.Action(nameof(ExternalCallback), new { returnUrl }) };
        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    [HttpGet("external/github-login")]
    [SwaggerOperation(
        Summary = "Initiate GitHub OAuth login flow",
        Description = "Redirects to GitHub for authentication"
    )]
    [SwaggerResponse(StatusCodes.Status302Found, "Redirect to GitHub authentication")]
    public IActionResult GitHubLogin(string? returnUrl = "/")
    {
        var properties = new AuthenticationProperties { RedirectUri = Url.Action(nameof(ExternalCallback), new { returnUrl }) };
        return Challenge(properties, GitHubAuthenticationDefaults.AuthenticationScheme);
    }

    [HttpGet("external/callback")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<IActionResult> ExternalCallback(string? returnUrl = "/")
    {
        var authenticateResult = await HttpContext.AuthenticateAsync();
        if (!authenticateResult.Succeeded || authenticateResult.Principal is null)
            return Unauthorized();

        var email = authenticateResult.Principal.FindFirst(c => c.Type.Contains("email"))?.Value;
        var name = authenticateResult.Principal.Identity?.Name ?? email ?? "User";
        if (email is null)
            return BadRequest("Email not provided by external provider");

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user is null)
        {
            user = new User
            {
                Id = Guid.NewGuid(),
                Name = name,
                Email = email,
                PasswordHash = string.Empty,
                Role = "user"
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
        }

        var pair = _tokenService.CreateTokenPair(user);
        return Ok(new { token = pair.Token, refreshToken = pair.RefreshToken, returnUrl });
    }

    public record RefreshRequest(string RefreshToken);

    [HttpPost("refresh")]
    [SwaggerOperation(
        Summary = "Refresh an expired JWT token",
        Description = "Uses a refresh token to generate a new JWT token and refresh token pair"
    )]
    [SwaggerResponse(StatusCodes.Status200OK, "Token refresh successful", typeof(LoginResponse))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Invalid or expired refresh token")]
    public IActionResult Refresh([FromBody] RefreshRequest request)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.RefreshToken))
            return Unauthorized(new { message = "Invalid refresh token" });

        try
        {
            var pair = _tokenService.Refresh(request.RefreshToken);
            return Ok(new LoginResponse(pair.Token, pair.RefreshToken));
        }
        catch (SecurityTokenException)
        {
            return Unauthorized(new { message = "Invalid refresh token" });
        }
    }

    public record RevokeRequest(string RefreshToken);

    [HttpPost("revoke")]
    [SwaggerOperation(
        Summary = "Revoke a refresh token",
        Description = "Invalidates a refresh token so it can no longer be used"
    )]
    [SwaggerResponse(StatusCodes.Status204NoContent, "Token successfully revoked")]
    public IActionResult Revoke([FromBody] RevokeRequest req)
    {
        if (req is null)
            return NoContent();

        _tokenService.Revoke(req.RefreshToken);
        return NoContent();
    }
} 