using JokesApi.Notifications;
using JokesApi.Settings;
using Microsoft.Extensions.Options;

namespace JokesApi.Services;

public class AlertService
{
    private readonly EmailNotifier _emailNotifier;
    private readonly SmsNotifier _smsNotifier;
    private readonly NotificationSettings _settings;

    public AlertService(EmailNotifier email, SmsNotifier sms, IOptions<NotificationSettings> options)
    {
        _emailNotifier = email;
        _smsNotifier = sms;
        _settings = options.Value;
    }

    public Task SendAsync(string recipient, string message, string? channel = null)
    {
        channel = (channel ?? _settings.DefaultChannel).ToLower();
        return channel switch
        {
            "sms" => _smsNotifier.SendAsync(recipient, message),
            _ => _emailNotifier.SendAsync(recipient, message),
        };
    }
} 