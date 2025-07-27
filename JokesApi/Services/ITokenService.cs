using JokesApi.Entities;

namespace JokesApi.Services;

public interface ITokenService
{
    string CreateToken(User user);
} 