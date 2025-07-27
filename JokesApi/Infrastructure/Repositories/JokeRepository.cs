using JokesApi.Domain.Repositories;
using JokesApi.Entities;
using JokesApi.Data;
using Microsoft.EntityFrameworkCore;

namespace JokesApi.Infrastructure.Repositories;

public class JokeRepository : IJokeRepository
{
    private readonly AppDbContext _db;
    public JokeRepository(AppDbContext db) => _db = db;

    public IQueryable<Joke> Query => _db.Jokes.AsQueryable();

    public Task AddAsync(Joke joke, CancellationToken ct = default)
    {
        _db.Jokes.Add(joke);
        return Task.CompletedTask;
    }

    public void Remove(Joke joke) => _db.Jokes.Remove(joke);
} 