namespace JokesApi.Notifications;

public interface INotifier
{
    string Channel { get; }
    Task SendAsync(string recipient, string message, CancellationToken cancellationToken = default);
} 