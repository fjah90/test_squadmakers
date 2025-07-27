using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using JokesApi.Entities;
using JokesApi.Settings;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using JokesApi.Data;
using Microsoft.EntityFrameworkCore;

namespace JokesApi.Services;

public class TokenService : ITokenService
{
    private readonly JwtSettings _settings;
    private readonly AppDbContext _db;

    public TokenService(AppDbContext db, IOptions<JwtSettings> options)
    {
        _db = db;
        _settings = options.Value;
    }

    private string CreateJwt(User user)
    {
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("name", user.Name)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_settings.ExpirationMinutes),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateRefreshToken() => Convert.ToBase64String(Guid.NewGuid().ToByteArray()) + Convert.ToBase64String(Guid.NewGuid().ToByteArray());

    public TokenPair CreateTokenPair(User user)
    {
        // create refresh token
        var refresh = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = GenerateRefreshToken(),
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            UserId = user.Id
        };
        _db.RefreshTokens.Add(refresh);
        _db.SaveChanges();

        var jwt = CreateJwt(user);
        return new TokenPair(jwt, refresh.Token);
    }

    public TokenPair Refresh(string refreshToken)
    {
        var stored = _db.RefreshTokens.Include(r=>r.User).FirstOrDefault(r => r.Token == refreshToken);
        if (stored is null || !stored.IsActive)
            throw new SecurityTokenException("Invalid refresh token");

        // revoke old
        stored.RevokedAt = DateTime.UtcNow;

        var pair = CreateTokenPair(stored.User);

        _db.SaveChanges();
        return pair;
    }

    public void Revoke(string refreshToken)
    {
        var stored = _db.RefreshTokens.FirstOrDefault(r => r.Token == refreshToken);
        if (stored is null) return;
        stored.RevokedAt = DateTime.UtcNow;
        _db.SaveChanges();
    }
} 