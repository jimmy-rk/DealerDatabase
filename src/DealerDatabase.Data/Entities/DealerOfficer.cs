namespace DealerDatabase.Data.Entities;

public class DealerOfficer
{
    public int Id { get; set; }
    public int DealerId { get; set; }
    public Dealer? Dealer { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Role { get; set; }
    public string? AppointedOn { get; set; }
    public string? Occupation { get; set; }
    public string? Nationality { get; set; }
}
