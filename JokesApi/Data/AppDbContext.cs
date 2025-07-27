using JokesApi.Entities;
using Microsoft.EntityFrameworkCore;

namespace JokesApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Joke> Jokes => Set<Joke>();
    public DbSet<Theme> Themes => Set<Theme>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure many-to-many between Joke and Theme
        modelBuilder.Entity<Joke>()
            .HasMany(j => j.Themes)
            .WithMany(t => t.Jokes);

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        base.OnModelCreating(modelBuilder);
    }
} 