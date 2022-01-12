// See https://aka.ms/new-console-template for more information

using Loupe.Agent.Core.Services;
using Loupe.Extensions.Logging;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

Console.WriteLine("Starting Whisky Worker");

var notificationFile = "../../../../Whisky.API/Notifications/notifications.json";
var notificationPath = new DirectoryInfo(notificationFile);

var cloudConnectionString = "UseDevelopmentStorage=true";

var services = new ServiceCollection();
    services.AddLogging(p =>
    {
        p.AddSimpleConsole();
        p.AddLoupe();
    })
    .AddLoupe(config =>
    {
        config.Publisher.ProductName = "Whisky";
        config.Publisher.ApplicationName = "API";
    })
	.AddSingleton<QueuedNotificationService>(p=> new QueuedNotificationService(cloudConnectionString, p.GetService<EmailNotificationService>(), p.GetService<ILogger<EmailNotificationService>>()))
    .AddSingleton(p => new EmailNotificationService(notificationPath.FullName, p.GetService<ISendEmailService>(), p.GetService<ILogger<EmailNotificationService>>()))
    .AddSingleton<ISendEmailService, SmtpSendEmailService>(
        p => new SmtpSendEmailService(cloudConnectionString, "localhost", 1025, "", "", p.GetService<ILogger<SmtpSendEmailService>>()))
    .BuildServiceProvider();

var logger = serviceProvider.GetService<ILogger<Program>>();

// start workers
var workerCancellationTokenSource = new CancellationTokenSource();
var queuedNotificationService = serviceProvider.GetService<QueuedNotificationService>();
var sendEmailService = serviceProvider.GetService<ISendEmailService>();

logger.LogInformation("Starting worker tasks");

var notificationWorker = Task.Run(() => queuedNotificationService!.ProcessQueue(workerCancellationTokenSource.Token));
var sendEmailWorkers = Task.Run(() => sendEmailService!.ProcessQueue(workerCancellationTokenSource.Token));

var resetEvent = new ManualResetEvent(false);
do
{
    if (Console.KeyAvailable || resetEvent.WaitOne(1000))
    {
        if (Console.ReadKey().Key == ConsoleKey.Escape)
        {
            await queuedNotificationService!.StopProcessing();
            await sendEmailService!.StopProcessing();

            workerCancellationTokenSource.Cancel();

            Task.WaitAll(notificationWorker, sendEmailWorkers);
            break;
        }
    }
} while (true);