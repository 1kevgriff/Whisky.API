using System.Reflection;
using System.Text.Json;
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

var ratingFolder = Path.Combine(Path.GetDirectoryName(whiskyPath), "Ratings");
if (!Directory.Exists(ratingFolder))
{
    Directory.CreateDirectory(ratingFolder);
    // let's generate some ratings

    app.Logger.LogInformation("Generating test ratings");

    var whiskyRepo = app.Services.GetService<IWhiskyRepository>();
    var random = new Random();

    var ratingMessages = new List<string>() {
        "This whisky is great!",
        "This whisky is ok.",
        "This whisky is not good.",
        "This whisky is awful.",
        "This whisky is terrible.",
        "Tastes like motor oil.",
        "You can clean a bumper with this stuff.",
        "A+++ will drink again",
        "It killed my dog.",
        "It was a great experience.",
        "It was a bad experience.",
        "It was a good experience.",
        "It was a terrible experience.",
        "Aight.",
        "I'm not a fan of this whisky.",
        "I'm a fan of this whisky.",
        "Two thumbs up",
        "Two thumbs down",
        "One thumbs up",
        "One thumbs down",
    };

    foreach (var w in whiskyRepo.GetAll(-1, -1)){
        app.Logger.LogInformation($"Generating ratings for {w.Name}");

        var randomRatings = random.Next(3, 10);

        for(int x = 0; x< randomRatings; x++){
            var rating = new Rating
            {
                Stars = (short)random.Next(1, 5),
                Message = ratingMessages[random.Next(0, ratingMessages.Count - 1)],
            };

            w.Ratings.Add(rating);
        }
        
        var whiskyRatingJsonPath = Path.Combine(ratingFolder, $"{w.Id}.json");
        var whiskyRatingJson = JsonSerializer.Serialize(w.Ratings);
        File.WriteAllText(whiskyRatingJsonPath, whiskyRatingJson);
    }
}

app.Run();
