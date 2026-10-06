namespace DealerDatabase.Data.Entities;

public class DealerSicCode
{
    public int Id { get; set; }
    public int DealerId { get; set; }
    public Dealer? Dealer { get; set; }
    public string Code { get; set; } = string.Empty;
}
