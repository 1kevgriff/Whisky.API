using System.ComponentModel.DataAnnotations;

public class Whisky
{
    [Key]
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string RegionStyle { get; set; } = string.Empty;
}
