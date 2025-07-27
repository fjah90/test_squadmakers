using Microsoft.Extensions.Logging;

namespace JokesApi.Notifications;

public class EmailNotifier : INotifier
{
    private readonly ILogger<EmailNotifier> _logger;

    public EmailNotifier(ILogger<EmailNotifier> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string recipient, string message, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Simulated EMAIL to {Recipient}: {Message}", recipient, message);
        return Task.CompletedTask;
    }
} 