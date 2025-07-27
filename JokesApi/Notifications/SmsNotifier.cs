using Microsoft.Extensions.Logging;

namespace JokesApi.Notifications;

public class SmsNotifier : INotifier
{
    private readonly ILogger<SmsNotifier> _logger;

    public SmsNotifier(ILogger<SmsNotifier> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string recipient, string message, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Simulated SMS to {Recipient}: {Message}", recipient, message);
        return Task.CompletedTask;
    }
} 