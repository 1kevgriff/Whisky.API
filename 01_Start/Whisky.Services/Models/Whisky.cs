public class Whisky
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string RegionStyle { get; set; } = string.Empty;
    public List<Rating> Ratings { get; set; } = new List<Rating>();
}
