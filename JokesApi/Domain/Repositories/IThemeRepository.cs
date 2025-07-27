namespace JokesApi.Domain.Repositories;

using JokesApi.Entities;

public interface IThemeRepository
{
    IQueryable<Theme> Query { get; }
} 