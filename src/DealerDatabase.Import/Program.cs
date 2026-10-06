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


