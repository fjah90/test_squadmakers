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

namespace JokesApi.Controllers;

[ApiController]
[Route("api/[controller]")]
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
    public record LoginResponse(string Token);

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        _logger.LogInformation("Login attempt for {Email}", request.Email);
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Invalid credentials for {Email}", request.Email);
            return Unauthorized(new { message = "Invalid credentials" });
        }

        var token = _tokenService.CreateToken(user);
        return Ok(new LoginResponse(token));
    }

    [HttpGet("external/google-login")]
    public IActionResult GoogleLogin(string? returnUrl = "/")
    {
        var properties = new AuthenticationProperties { RedirectUri = Url.Action(nameof(ExternalCallback), new { returnUrl }) };
        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    [HttpGet("external/github-login")]
    public IActionResult GitHubLogin(string? returnUrl = "/")
    {
        var properties = new AuthenticationProperties { RedirectUri = Url.Action(nameof(ExternalCallback), new { returnUrl }) };
        return Challenge(properties, GitHubAuthenticationDefaults.AuthenticationScheme);
    }

    [HttpGet("external/callback")]
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

        var token = _tokenService.CreateToken(user);
        return Ok(new { token, returnUrl });
    }
} 