using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;

public interface IWhiskyRepository
{
    IEnumerable<Whisky> GetAll();
    void Add(Whisky whisky);
    void Update(Whisky whisky);
    void Delete(string id);
}

public class CsvWhiskyRepository : IWhiskyRepository
{
    private List<Whisky> _whisky = new List<Whisky>();
    private readonly string _csvPath;

    public CsvWhiskyRepository(string csvPath)
    {
        _whisky = ReadWhiskyFromCsv(csvPath).ToList();
        _csvPath = csvPath;
    }

    private IEnumerable<Whisky> ReadWhiskyFromCsv(string csvPath)
    {
        using var streamReader = new StreamReader(csvPath);

        using CsvReader csvReader = new CsvReader(streamReader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            TrimOptions = TrimOptions.Trim,
        });

        csvReader.Context.RegisterClassMap<WhiskyMap>();

        return csvReader.GetRecords<Whisky>().DistinctBy(p => p.Name).ToList();
    }

    private void SaveWhiskyListToCsv(string csvPath)
    {
        var csvLines = new List<string>();
        foreach (var whisky in _whisky)
        {
            csvLines.Add($"{whisky.Name},{whisky.RegionStyle}");
        }
        System.IO.File.WriteAllLines(csvPath, csvLines);
    }

    public void Add(Whisky whisky)
    {
        _whisky.Add(whisky);
        SaveWhiskyListToCsv(_csvPath);
    }

    public void Delete(string name)
    {
        _whisky.RemoveAll(p => p.Name == name);
        SaveWhiskyListToCsv(_csvPath);
    }

    public IEnumerable<Whisky> GetAll()
    {
        return _whisky;
    }

    public void Update(Whisky whisky)
    {
        var whiskyToUpdate = _whisky.Find(p => p.Name == whisky.Name);
        if (whiskyToUpdate == null) throw new KeyNotFoundException($"Whisky not found: {whisky.Name}");
        whiskyToUpdate.RegionStyle = whisky.RegionStyle;
        SaveWhiskyListToCsv(_csvPath);
    }
}