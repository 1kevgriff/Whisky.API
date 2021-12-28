using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

public class WhiskyMap : ClassMap<Whisky>
{
    public WhiskyMap()
    {
        Map(p => p.Id).Name("Id").Index(0).TypeConverter<GuidConverter>();
        Map(p => p.Name).Name("Whisky Name").Index(1).TypeConverter<TitleCaseConverter>();
        Map(p => p.RegionStyle).Name("Whisky Region or Style").Index(2).TypeConverter<TitleCaseConverter>();
    }
}

public class TitleCaseConverter : DefaultTypeConverter
{
    public override string ConvertToString(object value, IWriterRow row, MemberMapData memberMapData)
    {
        TextInfo myTI = new CultureInfo("en-US", false).TextInfo;
        return myTI.ToTitleCase((string)value);
    }

    public override object ConvertFromString(string value, IReaderRow row, MemberMapData memberMapData)
    {
        TextInfo myTI = new CultureInfo("en-US", false).TextInfo;
        return myTI.ToTitleCase(value);
    }
}