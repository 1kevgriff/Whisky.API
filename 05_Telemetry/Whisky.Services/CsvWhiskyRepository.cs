using System.Globalization;
using System.Text.Json;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Logging;

public class CsvWhiskyRepository : IWhiskyRepository
{
    private List<Whisky> _whisky = new List<Whisky>();
    private readonly string _csvPath;
    private readonly ILogger<IWhiskyRepository> _logger;

    public CsvWhiskyRepository(string csvPath, ILogger<IWhiskyRepository> logger)
    {
        _csvPath = csvPath;
        _logger = logger;
        _whisky = ReadWhiskyFromCsv(csvPath).ToList();
    }

    private IEnumerable<Whisky> ReadWhiskyFromCsv(string csvPath)
    {
        using var scope = _logger.BeginFileScope("Whisky", csvPath);

        using var streamReader = new StreamReader(csvPath);

        using CsvReader csvReader = new CsvReader(streamReader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            TrimOptions = TrimOptions.Trim,
        });

        csvReader.Context.RegisterClassMap<WhiskyMap>();

        var results = csvReader.GetRecords<Whisky>().DistinctBy(p => p.Name).ToList();

        // read all the ratings from disk
        LoadRatings(results);

        return results;
    }

    private void LoadRatings(List<Whisky> whiskyList)
    {
        var ratingFolder = Path.Combine(Path.GetDirectoryName(_csvPath), "ratings");
        if (!Directory.Exists(ratingFolder)) Directory.CreateDirectory(ratingFolder);

        foreach (var w in whiskyList)
        {
            var whiskyRatingJsonPath = Path.Combine(ratingFolder, $"{w.Id}.json");

            using var scope = _logger.BeginFileScope("Whisky-Ratings", whiskyRatingJsonPath);

            if (File.Exists(whiskyRatingJsonPath))
            {
                var whiskyRatingJson = File.ReadAllText(whiskyRatingJsonPath);
                w.Ratings = JsonSerializer.Deserialize<List<Rating>>(whiskyRatingJson);
            }
        }
    }

    private void SaveWhiskyListToCsv(string csvPath)
    {
        using var scope = _logger.BeginFileScope("Whisky", csvPath);

        using var streamWriter = new StreamWriter(csvPath);

        using CsvWriter csvWriter = new CsvWriter(streamWriter, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            TrimOptions = TrimOptions.Trim,
        });

        csvWriter.Context.RegisterClassMap<WhiskyMap>();
        csvWriter.WriteRecords(_whisky);
        csvWriter.Flush();

        foreach (var w in _whisky)
        {
            var whiskyRatingJsonPath = Path.Combine(Path.GetDirectoryName(csvPath), "ratings", $"{w.Id}.json");
            var whiskyRatingJson = JsonSerializer.Serialize(w.Ratings);
            File.WriteAllText(whiskyRatingJsonPath, whiskyRatingJson);
        }
    }

    public Whisky Add(Whisky whisky)
    {
        whisky.Id = Guid.NewGuid();
        using (_logger.BeginWhiskyScope(whisky.Id))
        {
            _logger.LogInformation("Adding Whisky {WhiskyName}", whisky.Name);

            _whisky.Add(whisky);
            SaveWhiskyListToCsv(_csvPath);

            return whisky;
        }
    }

    public void Delete(Guid id)
    {
        _whisky.RemoveAll(p => p.Id.Equals(id));
        // TODO: delete rating file

        SaveWhiskyListToCsv(_csvPath);
    }

    public IEnumerable<Whisky> GetAll(int skip = 0, int take = 100)
    {
        if (skip == -1 && take == -1)
        {
            return _whisky;
        }

        return _whisky.Skip(skip).Take(take);
    }

    public Whisky? GetById(Guid id) => _whisky.FirstOrDefault(p => p.Id.Equals(id));

    public void Update(Whisky whisky)
    {
        _logger.LogInformation("Updating Whisky {Whisky.Name}.\r\n" +
                               "Region / Style: {Whisky.Region}", whisky.Name, whisky.RegionStyle);

        var whiskyToUpdate = _whisky.Find(p => p.Id == whisky.Id);
        if (whiskyToUpdate == null) throw new KeyNotFoundException($"Whisky not found: {whisky.Name}");
        whiskyToUpdate.RegionStyle = whisky.RegionStyle;
        
        SaveWhiskyListToCsv(_csvPath);
    }

    public void AddRating(Guid id, short stars, string message)
    {
        _logger.LogInformation("Adding {Whisky-Rating.Stars} to Whisky.\r\n" +
                               "Message: {Whisky-Rating.Message}", stars, message);

        var whisky = _whisky.Find(p => p.Id.Equals(id));
        if (whisky == null) throw new KeyNotFoundException($"Whisky not found: {id}");

        var rating = new Rating
        {
            Stars = stars,
            Message = message,
        };

        whisky.Ratings.Add(rating);
        SaveWhiskyListToCsv(_csvPath);
    }
}