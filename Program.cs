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

var whiskyPath = Path.Combine(Directory.GetCurrentDirectory(), "whiskey.csv");
builder.Services.AddTransient<IWhiskyRepository, CsvWhiskyRepository>(p => new CsvWhiskyRepository(@"whisky.csv"));

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

app.Run();
