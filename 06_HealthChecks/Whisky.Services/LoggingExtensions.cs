using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Queues.Models;
using Microsoft.Extensions.Logging;

public static class LoggingExtensions
{
    public static IDisposable BeginWhiskyScope(this ILogger logger, Guid whiskyId)
    {
        return logger.BeginScope(new []{ new KeyValuePair<string, object>("Whisky.Id", whiskyId)});
    }

    public static IDisposable BeginFileScope(this ILogger logger, string label, string fileName)
    {
        return logger.BeginScope(new[] { new KeyValuePair<string, object>(label, fileName) });
    }

    public static IDisposable BeginQueueMessageScope(this ILogger logger, QueueMessage queueMessage)
    {
        if (queueMessage == null) return null;

        return logger.BeginScope(new[]
        {
            new KeyValuePair<string, object>("Message.Id", queueMessage.MessageId),
            new KeyValuePair<string, object>("Message.Queued", queueMessage.InsertedOn),
        });
    }

    public static IDisposable BeginEmailScope(this ILogger logger, OutgoingEmailMessage emailMessage)
    {
        if (emailMessage == null) return null;

        return logger.BeginScope(new[]
        {
            new KeyValuePair<string, object>("Email.From", emailMessage.From),
            new KeyValuePair<string, object>("Email.To", emailMessage.To),
            new KeyValuePair<string, object>("Email.Subject", emailMessage.Subject),
        });
    }
}
