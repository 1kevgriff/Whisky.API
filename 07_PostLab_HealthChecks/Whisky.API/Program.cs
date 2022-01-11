using System.Reflection;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;

var cloudConnectionString = "UseDevelopmentStorage=true";
var notificationPath = Path.Combine(Directory.GetCurrentDirectory(), "Notifications", "notifications.json");
var whiskyPath = Path.Combine(Directory.GetCurrentDirectory(), "whisky.csv");

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

var healthChecksBuilder = builder.Services.AddHealthChecks();
healthChecksBuilder.AddAzureQueueStorage(cloudConnectionString, "whisky-notifications-new", "Notification queue: New");
healthChecksBuilder.AddAzureQueueStorage(cloudConnectionString, "whisky-notifications-rating", "Notification queue: Rating");
healthChecksBuilder.AddAzureQueueStorage(cloudConnectionString, "whisky-outgoing-emails", "Outgoing emails queue");
healthChecksBuilder.AddCheck("Does Whisky.csv exist", () => File.Exists(whiskyPath) ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy("File is missing"));
healthChecksBuilder.AddCheck("Does Notifications.json exist", () => File.Exists(notificationPath) ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy("File is missing"));

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "The Whisky API", Version = "v1" });

    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
});

builder.Services.AddTransient<IWhiskyRepository, CsvWhiskyRepository>(p => new CsvWhiskyRepository(whiskyPath));

builder 
    .Services
    .AddSingleton(p => new EmailNotificationService(notificationPath, p.GetService<ISendEmailService>(), p.GetService<ILogger<EmailNotificationService>>()));

builder.Services.AddSingleton<QueuedNotificationService>(p => new QueuedNotificationService(cloudConnectionString,
    p.GetService<EmailNotificationService>(), p.GetService<ILogger<EmailNotificationService>>()));

builder.Services.AddSingleton<INotificationService>(configure =>
    configure.GetService<QueuedNotificationService>());


builder.Services.AddSingleton<ISendEmailService, SmtpSendEmailService>(
    p => new SmtpSendEmailService(cloudConnectionString, "localhost", 1025, "", "", p.GetService<ILogger<SmtpSendEmailService>>()));

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();

app.MapControllers();
app.UseEndpoints(config =>
{
    config.MapHealthChecks("/health", new HealthCheckOptions
    {
        Predicate = _ => true,
        AllowCachingResponses = true,
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    });
});

// redirect / to /swagger/index.html
app.Use(async (context, next) =>
{
    if (context.Request.Path.Value == "/")
    {
        context.Response.Redirect("/swagger/index.html");
    }
    else
    {
        await next();
    }
});

// demo prep stuff if it's not already there
using (var scope = app.Services.CreateScope())
{
    var ratingFolder = Path.Combine(Path.GetDirectoryName(whiskyPath), "Ratings");
    DemoPrep.PrepareRatings(ratingFolder, scope.ServiceProvider.GetService<IWhiskyRepository>(), app.Logger);

    var notificationFolder = Path.Combine(Path.GetDirectoryName(whiskyPath), "Notifications");
    DemoPrep.PrepareNotifications(notificationFolder, scope.ServiceProvider.GetService<IWhiskyRepository>(), app.Logger);
}

app.Run();
