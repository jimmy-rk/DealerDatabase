namespace DealerDatabase.Data.Entities;

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

/// <summary>
/// A consolidated dealership record. This type is used both as the import model and the EF Core entity.
/// It contains many optional fields collected from disparate sources.
/// </summary>
public class Dealer
{
    public int Id { get; set; }

    // Primary display name (kept for backwards compatibility with the starter schema)
    public string Name { get; set; } = string.Empty;

    // Consolidated source fields (optional)
    public string? Key { get; set; }
    public string? CompanyNumber { get; set; }
    public string? LegalName { get; set; }
    public string? TradingName { get; set; }
    public string? CompanyStatus { get; set; }
    public string? CompanyType { get; set; }
    public string? IncorporationDate { get; set; }

    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? Postcode { get; set; }
    // Town or locality (kept separately from AddressLine2)
    public string? Town { get; set; }
    // Source of the address (e.g. "FCA Register" or "Companies House")
    public string? AddressSource { get; set; }
    public string? Country { get; set; }

    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
    public string? FacebookUrl { get; set; }
    public string? InstagramUrl { get; set; }
    public string? WebsitePlatform { get; set; }

    public string? FcaFrn { get; set; }
    public string? FcaStatus { get; set; }
    public string? FcaStatusEffectiveDate { get; set; }
    public ICollection<DealerFcaPermission> FcaPermissions { get; set; } = new List<DealerFcaPermission>();
    public string? IcoRegistrationNumber { get; set; }
    public string? IcoExpiryDate { get; set; }
    public string? SafStatus { get; set; }
    public string? SafExpiryDate { get; set; }
    public string? VatNumber { get; set; }

    public string? SellerType { get; set; }
    public string? FranchiseMake { get; set; }
    public int InventoryCount { get; set; }
    public decimal AvgListedPrice { get; set; }
    public int AvgDaysInStock { get; set; }
    public int SoldLast30Days { get; set; }
    public string? StockFeedProvider { get; set; }

    public decimal GoogleRating { get; set; }
    public int GoogleReviewCount { get; set; }
    public decimal TrustpilotScore { get; set; }
    public string? WarrantyOffered { get; set; }

    // Collections gathered during consolidation. SicCodes are modelled as a related table.
    public ICollection<DealerSicCode> SicCodes { get; set; } = new List<DealerSicCode>();
    public ICollection<DealerOfficer> Officers { get; set; } = new List<DealerOfficer>();
    [NotMapped]
    public HashSet<string> MatchedSources { get; set; } = new();
}

