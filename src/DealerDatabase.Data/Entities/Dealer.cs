namespace DealerDatabase.Data.Entities;

/// <summary>
/// A consolidated dealership record.
/// This is a minimal starting point - extend or replace it as you see fit.
/// </summary>
public class Dealer
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
}
