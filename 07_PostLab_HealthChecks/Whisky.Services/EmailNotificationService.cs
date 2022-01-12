using System.Net;
using System.Net.Mail;
using System.Text.Json;
using Microsoft.Extensions.Logging;

public class EmailNotificationService : INotificationService
{
    private readonly string _filePath;
    private readonly List<NotificationRequest> _notifications;
    private readonly ISendEmailService _sendEmailService;
    private readonly ILogger<EmailNotificationService> _logger;

    public EmailNotificationService(string notificationFilePath, ISendEmailService sendEmailService, ILogger<EmailNotificationService> logger)
    {
        _notifications = new List<NotificationRequest>();
        if (File.Exists(notificationFilePath))
        {
            var json = File.ReadAllText(notificationFilePath);
            var deserialized = JsonSerializer.Deserialize<List<NotificationRequest>>(json);

            if (deserialized != null)
                _notifications = deserialized;
        }

        _sendEmailService = sendEmailService;
        _logger = logger;
    }

    public async Task RatingAdded(Whisky whisky, Rating rating)
    {
        var subject = "[Whisky API] New Rating Added";
        var body = @$"Hey there!  We thought you'd like to know a new rating has been added for {whisky.Name}!  

                      It was given a {rating.Stars} star rating with the following message: {rating.Message}";

        foreach (var notification in _notifications.Where(p => p.NotificationType == NotificationType.NEW_RATING))
        {
            var newOutgoingMessage = new OutgoingEmailMessage()
            {
                Body = body,
                Subject = subject,
                To = notification.EmailAddress
            };

            // notify!
            await _sendEmailService.QueueEmail(newOutgoingMessage);
        }
    }

    public async Task WhiskeyAdded(Whisky whisky)
    {
        var subject = "[Whisky API] New Whisky Added";
        var body = @$"Hey there!  We thought you'd like to know a new whisky has been added!  
                      It is named {whisky.Name} and is from the {whisky.RegionStyle} region.";
        foreach (var notification in
                        _notifications.Where(p => p.NotificationType == NotificationType.NEW_WHISKY ||
                            (p.NotificationType == NotificationType.NEW_WHISKY_IN_REGION &&
                                                    p.Region.Equals(whisky.RegionStyle, StringComparison.OrdinalIgnoreCase))))
        {
            var newOutgoingMessage = new OutgoingEmailMessage()
            {
                Body = body,
                Subject = subject,
                To = notification.EmailAddress
            };

            // notify!
            await _sendEmailService.QueueEmail(newOutgoingMessage);
        }
    }
}