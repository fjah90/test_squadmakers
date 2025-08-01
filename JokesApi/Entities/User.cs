namespace JokesApi.Entities;

public class User
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public string Role { get; set; } = "user";

    // Navigation property
    public ICollection<Joke> Jokes { get; set; } = new List<Joke>();
} 