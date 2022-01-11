using System;
using System.Collections.Concurrent;
using System.Dynamic;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Storage.Queues;
using Microsoft.Extensions.Logging;

public class QueuedNotificationService : INotificationService
{
    private readonly EmailNotificationService _emailNotificationService;
    private ILogger<EmailNotificationService> _logger;
    private readonly QueueClient _notificationQueue;
    private readonly QueueClient _notificationRatingQueue;

    private const string QUEUE_NAME_NOTIFICATIONS = "whisky-notifications-new";
    private const string QUEUE_NAME_RATING_NOTIFICATIONS = "whisky-notifications-rating";

    private JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions
    {
        IncludeFields = true,
    };

    public QueuedNotificationService(string cloudConnectionString, EmailNotificationService emailNotificationService, ILogger<EmailNotificationService> logger)
    {
        _emailNotificationService = emailNotificationService;
        _logger = logger;

        _notificationQueue = new QueueClient(cloudConnectionString, QUEUE_NAME_NOTIFICATIONS);
        _notificationQueue.CreateIfNotExists();

        _notificationRatingQueue =
            new QueueClient(cloudConnectionString, QUEUE_NAME_RATING_NOTIFICATIONS);
        _notificationRatingQueue.CreateIfNotExists();
    }

    public async Task WhiskeyAdded(Whisky whisky)
    {
        var messageText = JsonSerializer.Serialize(whisky);

        await _notificationQueue.SendMessageAsync(messageText);
    }

    public async Task RatingAdded(Whisky whisky, Rating rating)
    {
        var messageText = JsonSerializer.Serialize((whisky, rating), jsonSerializerOptions);

        await _notificationRatingQueue.SendMessageAsync(messageText);
    }

    public async Task ProcessQueue(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var notification = await _notificationQueue.ReceiveMessageAsync(null, cancellationToken);
            if (notification?.Value != null)
            {
                try
                {
                    var deserializedWhisky = JsonSerializer.Deserialize<Whisky>(notification.Value.MessageText);
                    await _emailNotificationService.WhiskeyAdded(deserializedWhisky);

                    await _notificationQueue.DeleteMessageAsync(notification.Value.MessageId, notification.Value.PopReceipt, cancellationToken);
                }
                catch
                {
                    // auto re-queued
                }
            }

            var rating = await _notificationRatingQueue.ReceiveMessageAsync(null, cancellationToken);
            if (rating?.Value != null)
            {
                try
                {
                    var deserialized =
                        JsonSerializer.Deserialize<(Whisky, Rating)>(rating.Value.MessageText, jsonSerializerOptions);
                    await _emailNotificationService.RatingAdded(deserialized.Item1, deserialized.Item2);

                    await _notificationRatingQueue.DeleteMessageAsync(rating.Value.MessageId, rating.Value.PopReceipt, cancellationToken);
                }
                catch
                {
                    // auto re-queued
                }
            }
        }
    }

    public Task StopProcessing()
    {
        return Task.CompletedTask;
    }
}