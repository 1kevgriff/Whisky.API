using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;

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
        using var streamWriter = new StreamWriter(csvPath);

        using CsvWriter csvWriter = new CsvWriter(streamWriter, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            TrimOptions = TrimOptions.Trim,
        });

        csvWriter.Context.RegisterClassMap<WhiskyMap>();
        csvWriter.WriteRecords(_whisky);
        csvWriter.Flush();
    }

    public void Add(Whisky whisky)
    {
        whisky.Id = Guid.NewGuid();

        _whisky.Add(whisky);
        SaveWhiskyListToCsv(_csvPath);
    }

    public void Delete(Guid id)
    {
        _whisky.RemoveAll(p => p.Id.Equals(id));
        SaveWhiskyListToCsv(_csvPath);
    }

    public IEnumerable<Whisky> GetAll(int pageNumber = 0, int pageSize = 100)
    {
        return _whisky.Skip(pageNumber * pageSize).Take(pageSize);
    }

    public Whisky? GetById(Guid id) => _whisky.FirstOrDefault(p => p.Id.Equals(id));

    public void Update(Whisky whisky)
    {
        var whiskyToUpdate = _whisky.Find(p => p.Name == whisky.Name);
        if (whiskyToUpdate == null) throw new KeyNotFoundException($"Whisky not found: {whisky.Name}");
        whiskyToUpdate.RegionStyle = whisky.RegionStyle;
        SaveWhiskyListToCsv(_csvPath);
    }
}