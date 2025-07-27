namespace JokesApi.Domain.Repositories;

using JokesApi.Entities;

public interface IUserRepository
{
    IQueryable<User> Query { get; }
} 