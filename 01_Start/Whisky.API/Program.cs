using System.Reflection;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "The Whisky API", Version = "v1" });

    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
});

var whiskyPath = Path.Combine(Directory.GetCurrentDirectory(), "whisky.csv");
builder.Services.AddTransient<IWhiskyRepository, CsvWhiskyRepository>(p => new CsvWhiskyRepository(whiskyPath));

builder.Services
    .AddTransient<INotificationService, EmailNotificationService>(p =>
                            new EmailNotificationService("localhost", 1025, string.Empty, string.Empty, p.GetService<ILogger<EmailNotificationService>>()));

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
