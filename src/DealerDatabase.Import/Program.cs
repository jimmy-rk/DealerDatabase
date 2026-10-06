using DealerDatabase;
using DealerDatabase.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDealerDatabase();

using var host = builder.Build();

var logger = host.Services.GetRequiredService<ILogger<Program>>();

await using (var scope = host.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DealerDbContext>();
    await db.Database.MigrateAsync();
}

logger.LogInformation("Database: {DatabaseFile}", SolutionPaths.DatabaseFile);
logger.LogInformation("Data folder: {DataDirectory}", SolutionPaths.DataDirectory);

foreach (var entry in Directory.EnumerateFileSystemEntries(SolutionPaths.DataDirectory).Order())
{
    logger.LogInformation("  Found source: {Name}", Path.GetFileName(entry));
}

logger.LogInformation("Starting data ingestion and consolidation pipeline...");


//Load Raw Sources
var chRoot = DataSourceProcessor.LoadJsonSource("companies_house.json");
var fcaRoot = DataSourceProcessor.LoadJsonSource("fca_register.json");
var icoRows = DataSourceProcessor.LoadCsvSource("ico_register.csv");
var mcRows = DataSourceProcessor.LoadCsvSource("marketcheck_dealers.csv");
var crawledRows = DataSourceProcessor.LoadCsvSource("crawled_dealers.csv");
var safDoc = DataSourceProcessor.LoadXmlSource("saf_members.xml");

// Generate VAT lookup aggregate (reads data/vat_lookups/*.json and writes data/vat.json)
try
{
    VatGenerator.GenerateAggregate("vat.json");
    logger.LogInformation("VAT json file generated: {Path}", Path.Combine(SolutionPaths.DataDirectory, "vat.json"));
}
catch (Exception ex)
{
    logger.LogWarning(ex, "Failed to generate VAT json file");
}


// Execute data Consolidation 
var masterMap = DealerConsolidator.RunConsolidation(chRoot, fcaRoot, icoRows, mcRows, crawledRows, safDoc);

logger.LogInformation("Consolidated {Count} distinct dealership records successfully.", masterMap.Count);

// Normalize and merge masterMap entries that refer to the same company name
var mergedMasters = NormaliseMergeConsolidatedData.GetNormaliseMergeConsolidatedData(masterMap);

logger.LogInformation("Merged masterMap into {Count} deduplicated records for persistence.", mergedMasters.Count);

//Persistence to Database 
await using (var scope = host.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DealerDbContext>();

    // Helper to produce a compact normalized key for names (letters/digits only, uppercased)
    static string NormalizeName(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return string.Empty;
        var chars = s.Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray();
        return new string(chars);
    }

    static bool IsMissing(string? s)
    {
        return string.IsNullOrWhiteSpace(s) || string.Equals(s, "nan", StringComparison.OrdinalIgnoreCase);
    }

    // Local helper to mark an entity as updated only when it's already persisted.
    void MarkUpdated(DealerDatabase.Data.Entities.Dealer ent)
    {
        var e = db.Entry(ent);
        if (e.State == Microsoft.EntityFrameworkCore.EntityState.Added)
        {
            // newly added in this context; no need to call Update
            return;
        }
        db.Update(ent);
    }

    // Load existing dealers into a map by normalized name (include SIC codes, officers and FCA permissions)
    var existingDealers = await db.Dealers
        .Include(d => d.SicCodes)
        .Include(d => d.Officers)
        .Include(d => d.FcaPermissions)
        .ToListAsync();
    var existingMap = existingDealers
        .Where(d => !string.IsNullOrWhiteSpace(d.Name))
        .ToDictionary(d => NormalizeName(d.Name), StringComparer.OrdinalIgnoreCase);

    foreach (var md in mergedMasters)
    {
        var rawName = md.TradingName ?? md.LegalName ?? md.Key;
        if (string.IsNullOrWhiteSpace(rawName)) continue;

        var norm = NormalizeName(rawName);
        if (existingMap.TryGetValue(norm, out var dealer))
        {
            // Update stored name if the incoming name appears more complete (longer) than the stored one
            if (!string.Equals(dealer.Name, rawName, StringComparison.Ordinal) && (dealer.Name?.Length ?? 0) < rawName.Length)
            {
                dealer.Name = rawName;
                MarkUpdated(dealer);
            }
            // If incoming source provided a deterministic MarketCheck key (cols[0]) prefer that over an earlier generated key.
            if (!string.IsNullOrWhiteSpace(md.Key) && md.Key.StartsWith("MC", StringComparison.OrdinalIgnoreCase) && !string.Equals(dealer.Key, md.Key, StringComparison.Ordinal))
            {
                dealer.Key = md.Key;
                MarkUpdated(dealer);
            }
            // Merge simple scalar fields when empty in DB
            dealer.Key ??= md.Key;
            dealer.CompanyNumber ??= md.CompanyNumber;
            dealer.LegalName ??= md.LegalName;
            dealer.TradingName ??= md.TradingName;
            dealer.CompanyStatus ??= md.CompanyStatus;
            dealer.CompanyType ??= md.CompanyType;
            dealer.IncorporationDate ??= md.IncorporationDate;
            // Prefer FCA-provided address when available (Town and Postcode included)
            if (!string.IsNullOrWhiteSpace(md.AddressSource) && md.AddressSource == "FCA Register")
            {
                // FCA is authoritative: overwrite when FCA provides values
                if (!IsMissing(md.AddressLine1)) dealer.AddressLine1 = md.AddressLine1;
                if (!IsMissing(md.AddressLine2)) dealer.AddressLine2 = md.AddressLine2;
                if (!IsMissing(md.Town)) dealer.Town = md.Town;
                if (!IsMissing(md.Postcode)) dealer.Postcode = md.Postcode;
                if (!IsMissing(md.Country)) dealer.Country = md.Country;
                dealer.AddressSource ??= md.AddressSource;
            }
            else
            {
                // Fill from other sources only when DB value is missing/invalid
                if (!IsMissing(md.AddressLine1) && IsMissing(dealer.AddressLine1)) dealer.AddressLine1 = md.AddressLine1;
                if (!IsMissing(md.AddressLine2) && IsMissing(dealer.AddressLine2)) dealer.AddressLine2 = md.AddressLine2;
                if (!IsMissing(md.Town) && IsMissing(dealer.Town)) dealer.Town = md.Town;
                if (!IsMissing(md.Postcode) && IsMissing(dealer.Postcode)) dealer.Postcode = md.Postcode;
                if (!IsMissing(md.Country) && IsMissing(dealer.Country)) dealer.Country = md.Country;
                dealer.AddressSource ??= md.AddressSource;
            }
            dealer.Phone ??= md.Phone;
            dealer.Email ??= md.Email;
            dealer.Website ??= md.Website;
            dealer.FacebookUrl ??= md.FacebookUrl;
            dealer.InstagramUrl ??= md.InstagramUrl;
            dealer.WebsitePlatform ??= md.WebsitePlatform;
            dealer.FcaFrn ??= md.FcaFrn;
            dealer.FcaStatus ??= md.FcaStatus;
            dealer.FcaStatusEffectiveDate ??= md.FcaStatusEffectiveDate;
            dealer.IcoRegistrationNumber ??= md.IcoRegistrationNumber;
            dealer.IcoExpiryDate ??= md.IcoExpiryDate;
            dealer.SafStatus ??= md.SafStatus;
            dealer.SafExpiryDate ??= md.SafExpiryDate;
            dealer.VatNumber ??= md.VatNumber;
            dealer.SellerType ??= md.SellerType;
            dealer.FranchiseMake ??= md.FranchiseMake;
            if (dealer.InventoryCount == 0 && md.InventoryCount != 0) dealer.InventoryCount = md.InventoryCount;
            if (dealer.AvgListedPrice == 0 && md.AvgListedPrice != 0) dealer.AvgListedPrice = md.AvgListedPrice;
            if (dealer.AvgDaysInStock == 0 && md.AvgDaysInStock != 0) dealer.AvgDaysInStock = md.AvgDaysInStock;
            if (dealer.SoldLast30Days == 0 && md.SoldLast30Days != 0) dealer.SoldLast30Days = md.SoldLast30Days;
            dealer.StockFeedProvider ??= md.StockFeedProvider;
            if (dealer.GoogleRating == 0 && md.GoogleRating != 0) dealer.GoogleRating = md.GoogleRating;
            if (dealer.GoogleReviewCount == 0 && md.GoogleReviewCount != 0) dealer.GoogleReviewCount = md.GoogleReviewCount;
            if (dealer.TrustpilotScore == 0 && md.TrustpilotScore != 0) dealer.TrustpilotScore = md.TrustpilotScore;
            dealer.WarrantyOffered ??= md.WarrantyOffered;
            // Merge officers from the consolidated model into the persisted dealer
            if (md.Officers != null && md.Officers.Any())
            {
                foreach (var incoming in md.Officers)
                {
                    var inName = incoming.Name?.Trim();
                    if (string.IsNullOrWhiteSpace(inName)) continue;
                    if (!dealer.Officers.Any(o => string.Equals(o.Name, inName, StringComparison.OrdinalIgnoreCase)))
                    {
                        dealer.Officers.Add(new DealerDatabase.Data.Entities.DealerOfficer
                        {
                            Name = inName,
                            Role = incoming.Role,
                            AppointedOn = incoming.AppointedOn,
                            Occupation = incoming.Occupation,
                            Nationality = incoming.Nationality
                        });
                    }
                }

                // Debug: inspect any dealers matching 'Carver' (MarketCheck row MC508842)
                await using (var debugScope = host.Services.CreateAsyncScope())
                {
                    var debugDb = debugScope.ServiceProvider.GetRequiredService<DealerDbContext>();
                    var allDealers = await debugDb.Dealers
                        .AsNoTracking()
                        .Include("SicCodes")
                        .Include("Officers")
                        .Include("FcaPermissions")
                        .ToListAsync();

                    var carvers = allDealers.Where(d => (d.Name != null && d.Name.Contains("Carver", StringComparison.OrdinalIgnoreCase))
                                                        || (d.TradingName != null && d.TradingName.Contains("Carver", StringComparison.OrdinalIgnoreCase))
                                                        || (d.Website != null && d.Website.IndexOf("carver", StringComparison.OrdinalIgnoreCase) >= 0))
                                        .ToList();

                    if (!carvers.Any())
                    {
                        logger.LogWarning("No dealers found matching 'Carver' after import.");
                    }
                    else
                    {
                        foreach (var c in carvers)
                        {
                            logger.LogInformation("Carver match: Key={Key}, Name={Name}, AddressLine1='{A1}', AddressLine2='{A2}', Town='{Town}', Postcode='{Postcode}', Website={Website}, StockFeedProvider={Sfp}",
                                c.Key, c.Name, c.AddressLine1 ?? "(null)", c.AddressLine2 ?? "(null)", c.Town ?? "(null)", c.Postcode ?? "(null)", c.Website ?? "(null)", c.StockFeedProvider ?? "(null)");
                        }
                    }
                }
                MarkUpdated(dealer);
            }
            // Merge SIC codes from the consolidated model into the persisted dealer
            if (md.SicCodes != null && md.SicCodes.Any())
            {
                foreach (var sc in md.SicCodes.Select(s => s.Code).Where(c => !string.IsNullOrWhiteSpace(c)))
                {
                    if (!dealer.SicCodes.Any(existing => string.Equals(existing.Code, sc, StringComparison.OrdinalIgnoreCase)))
                    {
                        dealer.SicCodes.Add(new DealerDatabase.Data.Entities.DealerSicCode { Code = sc.Trim() });
                    }
                }
                MarkUpdated(dealer);
            }
            // Merge FCA permissions
            if (md.FcaPermissions != null && md.FcaPermissions.Any())
            {
                foreach (var fp in md.FcaPermissions.Select(p => p.Permission).Where(p => !string.IsNullOrWhiteSpace(p)))
                {
                    if (!dealer.FcaPermissions.Any(existing => string.Equals(existing.Permission, fp, StringComparison.OrdinalIgnoreCase)))
                    {
                        dealer.FcaPermissions.Add(new DealerDatabase.Data.Entities.DealerFcaPermission { Permission = fp.Trim() });
                    }
                }
                MarkUpdated(dealer);
            }
        }
        else
        {
            var newDealer = new DealerDatabase.Data.Entities.Dealer
            {
                Name = rawName,
                Key = md.Key,
                CompanyNumber = md.CompanyNumber,
                LegalName = md.LegalName,
                TradingName = md.TradingName,
                CompanyStatus = md.CompanyStatus,
                CompanyType = md.CompanyType,
                IncorporationDate = md.IncorporationDate,
                AddressLine1 = md.AddressLine1,
                AddressLine2 = md.AddressLine2,
                Town = md.Town,
                Postcode = md.Postcode,
                Country = md.Country,
                Phone = md.Phone,
                Email = md.Email,
                Website = md.Website,
                FacebookUrl = md.FacebookUrl,
                InstagramUrl = md.InstagramUrl,
                WebsitePlatform = md.WebsitePlatform,
                FcaFrn = md.FcaFrn,
                FcaStatus = md.FcaStatus,
                FcaStatusEffectiveDate = md.FcaStatusEffectiveDate,
                IcoRegistrationNumber = md.IcoRegistrationNumber,
                IcoExpiryDate = md.IcoExpiryDate,
                SafStatus = md.SafStatus,
                SafExpiryDate = md.SafExpiryDate,
                VatNumber = md.VatNumber,
                SellerType = md.SellerType,
                FranchiseMake = md.FranchiseMake,
                InventoryCount = md.InventoryCount,
                AvgListedPrice = md.AvgListedPrice,
                AvgDaysInStock = md.AvgDaysInStock,
                SoldLast30Days = md.SoldLast30Days,
                StockFeedProvider = md.StockFeedProvider,
                GoogleRating = md.GoogleRating,
                GoogleReviewCount = md.GoogleReviewCount,
                TrustpilotScore = md.TrustpilotScore,
                WarrantyOffered = md.WarrantyOffered,
                SicCodes = md.SicCodes?.Select(s => new DealerDatabase.Data.Entities.DealerSicCode { Code = s.Code.Trim() }).ToList() ?? new List<DealerDatabase.Data.Entities.DealerSicCode>(),
                Officers = md.Officers?.Select(o => new DealerDatabase.Data.Entities.DealerOfficer
                {
                    Name = o.Name?.Trim() ?? string.Empty,
                    Role = o.Role,
                    AppointedOn = o.AppointedOn,
                    Occupation = o.Occupation,
                    Nationality = o.Nationality
                }).ToList() ?? new List<DealerDatabase.Data.Entities.DealerOfficer>(),
                FcaPermissions = md.FcaPermissions?.Select(p => new DealerDatabase.Data.Entities.DealerFcaPermission { Permission = p.Permission.Trim() }).ToList() ?? new List<DealerDatabase.Data.Entities.DealerFcaPermission>()
            };
            db.Dealers.Add(newDealer);
            existingMap[norm] = newDealer;
        }
    }

    await db.SaveChangesAsync();
}

logger.LogInformation("Pipeline execution completed successfully.");