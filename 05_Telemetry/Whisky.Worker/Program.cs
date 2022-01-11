// See https://aka.ms/new-console-template for more information

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

Console.WriteLine("Starting Whisky Worker");

var cloudConnectionString = "UseDevelopmentStorage=true";

var serviceProvider = new ServiceCollection()
    .AddLogging(p =>
    {
        p.AddConsole();
    })
    .AddSingleton<QueuedNotificationService>(p=> new QueuedNotificationService(cloudConnectionString, p.GetService<EmailNotificationService>(), p.GetService<ILogger<EmailNotificationService>>()))
    .AddSingleton(p => new EmailNotificationService(p.GetService<ISendEmailService>(), p.GetService<ILogger<EmailNotificationService>>()))
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