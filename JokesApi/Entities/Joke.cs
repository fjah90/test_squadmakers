namespace JokesApi.Entities;

public class Joke
{
    public Guid Id { get; set; }
    public required string Text { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Guid AuthorId { get; set; }
    public User Author { get; set; } = null!;

    public string Source { get; set; } = "Local";

    // Navigation property for many-to-many relationship
    public ICollection<Theme> Themes { get; set; } = new List<Theme>();
} 