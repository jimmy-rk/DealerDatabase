namespace DealerDatabase.Data.Entities;

public class DealerFcaPermission
{
    public int Id { get; set; }
    public int DealerId { get; set; }
    public Dealer? Dealer { get; set; }

    public string Permission { get; set; } = string.Empty;
}
