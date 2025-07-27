namespace JokesApi.Domain.Repositories;

using JokesApi.Entities;

public interface IJokeRepository
{
    IQueryable<Joke> Query { get; }
    Task AddAsync(Joke joke, CancellationToken ct = default);
    void Remove(Joke joke);
} 