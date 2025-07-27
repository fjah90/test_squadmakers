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
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure many-to-many between Joke and Theme
        modelBuilder.Entity<Joke>()
            .HasMany(j => j.Themes)
            .WithMany(t => t.Jokes);

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<RefreshToken>()
            .HasIndex(r => r.Token)
            .IsUnique();
        modelBuilder.Entity<RefreshToken>()
            .HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId);

        base.OnModelCreating(modelBuilder);
    }
} 