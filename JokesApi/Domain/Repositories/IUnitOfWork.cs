namespace JokesApi.Domain.Repositories;

public interface IUnitOfWork
{
    IJokeRepository Jokes { get; }
    IThemeRepository Themes { get; }
    IUserRepository Users { get; }
    Task<int> SaveAsync(CancellationToken ct = default);
} 