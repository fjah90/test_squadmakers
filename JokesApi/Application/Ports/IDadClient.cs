namespace JokesApi.Application.Ports;

public interface IDadClient
{
    Task<string?> GetRandomJokeAsync(CancellationToken ct = default);
} 