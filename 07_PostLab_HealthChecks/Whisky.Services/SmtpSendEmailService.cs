using System.Collections.Concurrent;
using System.Net;
using System.Net.Mail;
using System.Text.Json;
using Azure.Storage.Queues;
using Microsoft.Extensions.Logging;

public class SmtpSendEmailService : ISendEmailService
{
    private readonly string _cloudConnectionString;
    private readonly string _smtpHostName;
    private readonly int _smtpPort;
    private readonly string _smtpUserName;
    private readonly string _smtpPassword;
    private readonly ILogger<SmtpSendEmailService> _logger;
    private readonly QueueClient _queue;

    private const int MAX_QUEUE_SIZE = 1000;
    private const int MAX_ATTEMPTS = 5;
    private const string QUEUE_NAME = "whisky-outgoing-emails";

    public SmtpSendEmailService(string cloudConnectionString, string smtpHostName, int smtpPort, string smtpUserName, string smtpPassword, ILogger<SmtpSendEmailService> logger)
    {
        _cloudConnectionString = cloudConnectionString;
        _smtpHostName = smtpHostName;
        _smtpPort = smtpPort;
        _smtpUserName = smtpUserName;
        _smtpPassword = smtpPassword;
        _logger = logger;

        _queue = new QueueClient(cloudConnectionString, QUEUE_NAME);
        _queue.CreateIfNotExists();
    }

    public async Task QueueEmail(OutgoingEmailMessage outgoingEmailMessage)
    {
        await _queue.SendMessageAsync(JsonSerializer.Serialize(outgoingEmailMessage));
    }

    public async Task ProcessQueue(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var dequeued = await _queue.ReceiveMessageAsync(null, cancellationToken);
            if (dequeued == null || dequeued.Value == null) continue;

            var item = JsonSerializer.Deserialize<OutgoingEmailMessage>(dequeued.Value.MessageText);

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

                await _queue.DeleteMessageAsync(dequeued.Value.MessageId, dequeued.Value.PopReceipt, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending email - attempt {item.Attempts}");

                // auto re-queue
            }
        }
    }
    public Task StopProcessing()
    {
        return Task.CompletedTask;
    }
}