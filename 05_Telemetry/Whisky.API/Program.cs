using System.Reflection;
using Loupe.Agent.AspNetCore;
using Loupe.Agent.Core.Services;
using Loupe.Extensions.Logging;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddLoupe();

// Add services to the container.
builder.Services.AddLoupe(config =>
{
    config.Publisher.ProductName = "Whisky";
    config.Publisher.ApplicationName = "API";
}).AddAspNetCoreDiagnostics();

builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "The Whisky API", Version = "v1" });

    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
});

var whiskyPath = Path.Combine(Directory.GetCurrentDirectory(), "whisky.csv");
builder.Services.AddTransient<IWhiskyRepository, CsvWhiskyRepository>(p => new CsvWhiskyRepository(whiskyPath, p.GetService<ILogger<IWhiskyRepository>>()));

builder 
    .Services
    .AddSingleton(p => new EmailNotificationService(p.GetService<ISendEmailService>(), p.GetService<ILogger<EmailNotificationService>>()));

var cloudConnectionString = "UseDevelopmentStorage=true";

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

app.UseAuthorization();

app.MapControllers();

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
