using JokesApi.Data;
using JokesApi.Domain.Repositories;
using JokesApi.Infrastructure.Repositories;

namespace JokesApi.Infrastructure;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _db;
    public IJokeRepository Jokes { get; }
    public IThemeRepository Themes { get; }
    public IUserRepository Users { get; }

    public UnitOfWork(AppDbContext db)
    {
        _db = db;
        Jokes = new JokeRepository(db);
        Themes = new ThemeRepository(db);
        Users = new UserRepository(db);
    }

    public Task<int> SaveAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
} 