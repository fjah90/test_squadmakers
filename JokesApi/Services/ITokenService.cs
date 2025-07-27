using JokesApi.Entities;

namespace JokesApi.Services;

public record TokenPair(string Token, string RefreshToken);

public interface ITokenService
{
    TokenPair CreateTokenPair(User user);
    TokenPair Refresh(string refreshToken);
    void Revoke(string refreshToken);
} 