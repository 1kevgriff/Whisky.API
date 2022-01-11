using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class SendEmailQueueWorker : IHostedService
{
    private readonly ISendEmailService _sendEmailService;
    private readonly ILogger<SendEmailQueueWorker> _logger;
    private CancellationTokenSource _workerCancellationTokenSource;
    private Task[] _workers;
    private Task _errorCheckingQueue;

    public SendEmailQueueWorker(ISendEmailService sendEmailService, ILogger<SendEmailQueueWorker> logger)
    {
        _sendEmailService = sendEmailService;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Background email queue processing starting");
        _workerCancellationTokenSource = new CancellationTokenSource();

        var workerCount = Environment.ProcessorCount;

        _workers = new Task[workerCount];

        for (int i = 0; i < workerCount; i++)
        {
            _workers[i] = Task.Run(() => _sendEmailService.ProcessQueue(_workerCancellationTokenSource.Token),
                cancellationToken);
        }

        _errorCheckingQueue = Task.Run(() => _sendEmailService.ProcessErrorQueue(_workerCancellationTokenSource.Token), cancellationToken);

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Background email queue processing STOP requested");
        if (_workers != null)
        {
            try
            {
                await _sendEmailService.StopProcessing();
                _workerCancellationTokenSource.CancelAfter(5000);
                
                Task.WaitAll(_workers, cancellationToken);
            }
            catch (TaskCanceledException ex)
            {
                // ignore
            }
        }
    }
}