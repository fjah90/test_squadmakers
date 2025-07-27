using JokesApi.Domain.Repositories;
using JokesApi.Entities;
using JokesApi.Data;

namespace JokesApi.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _db;
    public UserRepository(AppDbContext db) => _db = db;
    public IQueryable<User> Query => _db.Users.AsQueryable();
} 