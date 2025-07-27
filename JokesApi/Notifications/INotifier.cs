namespace JokesApi.Notifications;

public interface INotifier
{
    Task SendAsync(string recipient, string message, CancellationToken cancellationToken = default);
} 