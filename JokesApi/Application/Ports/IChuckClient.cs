namespace JokesApi.Application.Ports;

public interface IChuckClient
{
    Task<string?> GetRandomJokeAsync(CancellationToken ct = default);
} 