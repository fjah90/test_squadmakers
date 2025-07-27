using JokesApi.Domain.Repositories;
using JokesApi.Entities;
using JokesApi.Data;

namespace JokesApi.Infrastructure.Repositories;

public class ThemeRepository : IThemeRepository
{
    private readonly AppDbContext _db;
    public ThemeRepository(AppDbContext db) => _db = db;
    public IQueryable<Theme> Query => _db.Themes.AsQueryable();
} 