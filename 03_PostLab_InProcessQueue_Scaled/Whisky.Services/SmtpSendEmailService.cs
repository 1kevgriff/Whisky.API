using System.Collections.Concurrent;
using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;

public class SmtpSendEmailService : ISendEmailService
{
    private readonly string _smtpHostName;
    private readonly int _smtpPort;
    private readonly string _smtpUserName;
    private readonly string _smtpPassword;
    private readonly ILogger<SmtpSendEmailService> _logger;
    private readonly BlockingCollection<OutgoingEmailMessage> _queue;
    private readonly BlockingCollection<OutgoingEmailMessage> _errorQueue;

    private const int MAX_QUEUE_SIZE = 1000;
    private const int MAX_ATTEMPTS = 5;


    public SmtpSendEmailService(string smtpHostName, int smtpPort, string smtpUserName, string smtpPassword, ILogger<SmtpSendEmailService> logger)
    {
        _smtpHostName = smtpHostName;
        _smtpPort = smtpPort;
        _smtpUserName = smtpUserName;
        _smtpPassword = smtpPassword;
        _logger = logger;
        _queue = new BlockingCollection<OutgoingEmailMessage>(MAX_QUEUE_SIZE);
        _errorQueue = new BlockingCollection<OutgoingEmailMessage>();
    }

    public void QueueEmail(OutgoingEmailMessage outgoingEmailMessage)
    {
        _queue.Add(outgoingEmailMessage);
    }

    public async Task ProcessQueue(CancellationToken cancellationToken)
    {
        foreach (var item in _queue.GetConsumingEnumerable(cancellationToken))
        {
            if (item.ScheduledTimeStamp > DateTimeOffset.UtcNow)
                await Task.Delay((item.ScheduledTimeStamp - DateTimeOffset.UtcNow).Milliseconds, cancellationToken);

            try
            {
                MailMessage mailMessage =
                    new MailMessage("notifications@whiskyapi.com", item.To, item.Subject, item.Body);
                using SmtpClient client = new SmtpClient(_smtpHostName, _smtpPort);

                if (!string.IsNullOrWhiteSpace(_smtpUserName) && !string.IsNullOrWhiteSpace(_smtpPassword))
                {
                    client.Credentials = new NetworkCredential(_smtpUserName, _smtpPassword);
                }

                _logger.LogInformation($"Sending email to {item.To} with subject {item.Subject}");
                await client.SendMailAsync(mailMessage, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending email - attempt {item.Attempts}");

                _errorQueue.Add(item, cancellationToken);
            }
        }
    }

    public Task ProcessErrorQueue(CancellationToken cancellationToken)
    {
        foreach (var item in _errorQueue.GetConsumingEnumerable(cancellationToken))
        {
            // TODO: complex error logic here
            if (item.Attempts > MAX_ATTEMPTS)
            {
                _logger.LogError($"Error sending email - no more attempts");
                continue;
            }

            item.Attempts++;
            item.ScheduledTimeStamp = DateTimeOffset.UtcNow.AddSeconds(5);

            _queue.Add(item, cancellationToken); // add back to real queue!!
        }

        return Task.CompletedTask;
    }

    public Task StopProcessing()
    {
        _queue.CompleteAdding();
        return Task.CompletedTask;
    }
}