using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class NotificationQueueWorker : IHostedService
{
    private readonly QueuedNotificationService _queuedNotificationService;
    private readonly ILogger<NotificationQueueWorker> _logger;

    private CancellationTokenSource _workerCancellationTokenSource;
    private Task _worker;

    public NotificationQueueWorker(QueuedNotificationService queuedNotificationService, ILogger<NotificationQueueWorker> logger)
    {
        _queuedNotificationService = queuedNotificationService;
        _logger = logger;
    }
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Background notification queue processing starting");
        _workerCancellationTokenSource = new CancellationTokenSource();

        _worker = Task.Run(() => _queuedNotificationService.ProcessItem(_workerCancellationTokenSource.Token), cancellationToken);

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Background notification queue processing STOP requested");
        if (_worker != null)
        {
            try
            {
                await _queuedNotificationService.StopProcessing();
                _workerCancellationTokenSource.CancelAfter(5000);
                await _worker.WaitAsync(cancellationToken);
            }
            catch (TaskCanceledException ex)
            {
                // ignore
            }
        }
    }
}
