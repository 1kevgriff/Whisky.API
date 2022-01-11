using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

public class QueuedNotificationService : INotificationService
{
    private readonly EmailNotificationService _emailNotificationService;
    private ILogger<EmailNotificationService> _logger;
    private readonly BlockingCollection<(Whisky, Rating)> _queue;

    public QueuedNotificationService(EmailNotificationService emailNotificationService, ILogger<EmailNotificationService> logger)
    {
        _emailNotificationService = emailNotificationService;
        _logger = logger;
        _queue = new BlockingCollection<(Whisky, Rating)>();
    }

    public async Task WhiskeyAdded(Whisky whisky)
    {
        _queue.Add((whisky, null));
    }

    public async Task RatingAdded(Whisky whisky, Rating rating)
    {
        _queue.Add((whisky, rating));
    }

    public async Task ProcessQueue(CancellationToken cancellationToken)
    {
        foreach (var item in _queue.GetConsumingEnumerable(cancellationToken))
        {
            if (item.Item2 == null)
            {
                await _emailNotificationService.WhiskeyAdded(item.Item1);
            }
            else
            {
                await _emailNotificationService.RatingAdded(item.Item1, item.Item2);
            }
        }
    }

    public Task StopProcessing()
    {
        _queue.CompleteAdding();
        return Task.CompletedTask;
    }
}


public interface ISendEmailService
{
    void QueueEmail(OutgoingEmailMessage outgoingEmailMessage);
    Task ProcessQueue(CancellationToken cancellationToken);
    Task ProcessErrorQueue(CancellationToken cancellationToken);
    Task StopProcessing();
}