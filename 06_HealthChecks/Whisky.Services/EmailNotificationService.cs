using System.Diagnostics;
using System.Net;
using System.Net.Mail;
using System.Text.Json;
using Microsoft.Extensions.Logging;

public class EmailNotificationService : INotificationService
{
    private readonly string _filePath;
    private readonly List<NotificationRequest> _notifications;
    private readonly string _smtpHostName;
    private readonly int _smtpPort;
    private readonly string _smtpUsername;
    private readonly string _smtpPassword;
    private readonly ISendEmailService _sendEmailService;
    private readonly ILogger<EmailNotificationService> _logger;

    public EmailNotificationService(ISendEmailService sendEmailService, ILogger<EmailNotificationService> logger)
    {
        var folder = "Notifications";
        var fileName = "notifications.json";
        _filePath = Path.Combine(Directory.GetCurrentDirectory(), folder, fileName);

        _notifications = new List<NotificationRequest>();
        if (File.Exists(_filePath))
        {
            var json = File.ReadAllText(_filePath);
            var deserialized = JsonSerializer.Deserialize<List<NotificationRequest>>(json);

            if (deserialized != null)
                _notifications = deserialized;
        }
        _sendEmailService = sendEmailService;
        _logger = logger;
    }

    public async Task RatingAdded(Whisky whisky, Rating rating)
    {
        _logger.LogInformation("Generating notifications for Rating Added");

        var subject = "[Whisky API] New Rating Added";
        var body = @$"Hey there!  We thought you'd like to know a new rating has been added for {whisky.Name}!  

                      It was given a {rating.Stars} star rating with the following message: {rating.Message}";

        var notificationsGenerated = 0;
        var timer = Stopwatch.StartNew();
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
            notificationsGenerated++;
        }

        timer.Stop();
        _logger.LogInformation("Completed generating {Notifications.Count:N0}  in {Duration}ms.", 
            notificationsGenerated, timer.ElapsedMilliseconds);
    }

    public async Task WhiskeyAdded(Whisky whisky)
    {
        _logger.LogInformation("Generating notifications for Whisky Added");

        var subject = "[Whisky API] New Whisky Added";
        var body = @$"Hey there!  We thought you'd like to know a new whisky has been added!  
                      It is named {whisky.Name} and is from the {whisky.RegionStyle} region.";
        var notificationsGenerated = 0;
        var timer = Stopwatch.StartNew();
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
            notificationsGenerated++;
        }

        timer.Stop();
        _logger.LogInformation("Completed generating {Notifications.Count:N0} notifications in {Duration}ms.",
            notificationsGenerated, timer.ElapsedMilliseconds);
    }
}