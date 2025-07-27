namespace JokesApi.Entities;

public class Theme
{
    public Guid Id { get; set; }
    public required string Name { get; set; }

    // Navigation property for many-to-many relationship
    public ICollection<Joke> Jokes { get; set; } = new List<Joke>();
} 