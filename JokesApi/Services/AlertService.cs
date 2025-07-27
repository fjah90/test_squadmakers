using JokesApi.Notifications;
using JokesApi.Settings;
using Microsoft.Extensions.Options;

namespace JokesApi.Services;

public class AlertService
{
    private readonly IDictionary<string, INotifier> _notifiers;
    private readonly NotificationSettings _settings;

    public AlertService(IEnumerable<INotifier> notifiers, IOptions<NotificationSettings> options)
    {
        _notifiers = notifiers.ToDictionary(n => n.Channel.ToLower(), n => n);
        _settings = options.Value;
    }

    public Task SendAsync(string recipient, string message, string? channel = null)
    {
        var key = (channel ?? _settings.DefaultChannel).ToLower();
        if (!_notifiers.TryGetValue(key, out var notifier))
        {
            notifier = _notifiers.Values.First(); // fallback
        }
        return notifier.SendAsync(recipient, message);
    }
} 