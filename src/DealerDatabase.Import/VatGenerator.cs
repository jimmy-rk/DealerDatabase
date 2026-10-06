using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using DealerDatabase.Data;

namespace DealerDatabase
{
    /// <summary>
    /// Scans JSON files inside the data/vat_lookups folder and produces an
    /// aggregated JSON file in the data folder containing { filename, postcode } entries.
    /// </summary>
    public static class VatGenerator
    {
        /// <summary>
        /// Generate aggregated VAT lookup file. Writes to {SolutionPaths.DataDirectory}/{outputFileName}.
        /// </summary>
        public static void GenerateAggregate(string outputFileName = "vat.json")
        {
            var vatDir = Path.Combine(SolutionPaths.DataDirectory, "vat_lookups");
            var outPath = Path.Combine(SolutionPaths.DataDirectory, outputFileName);

            var results = new List<object>();

            if (!Directory.Exists(vatDir))
            {
                // Ensure output directory exists
                Directory.CreateDirectory(SolutionPaths.DataDirectory);
                File.WriteAllText(outPath, JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true }));
                return;
            }

            foreach (var file in Directory.GetFiles(vatDir, "*.json"))
            {
                string filename = Path.GetFileNameWithoutExtension(file);
                string? postcode = null;

                try
                {
                    var text = File.ReadAllText(file);
                    using var doc = JsonDocument.Parse(text);
                    var root = doc.RootElement;

                    // Try to find a postcode anywhere in the JSON by:
                    // 1) looking for properties whose name contains 'post', and
                    // 2) falling back to scanning string values for a postcode-like pattern.
                    var postcodePattern = new System.Text.RegularExpressions.Regex(@"\b[A-Z]{1,2}\d{1,2}[A-Z]?\s*\d[A-Z]{2}\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                    string? FindPostcode(JsonElement element)
                    {
                        switch (element.ValueKind)
                        {
                            case JsonValueKind.Object:
                                foreach (var prop in element.EnumerateObject())
                                {
                                    // check property name first
                                    if (prop.Name.IndexOf("post", StringComparison.OrdinalIgnoreCase) >= 0)
                                    {
                                        if (prop.Value.ValueKind == JsonValueKind.String)
                                        {
                                            var v = prop.Value.GetString();
                                            if (!string.IsNullOrWhiteSpace(v)) return v.Trim();
                                        }
                                    }

                                    // recurse into property value
                                    var found = FindPostcode(prop.Value);
                                    if (found != null) return found;
                                }
                                break;
                            case JsonValueKind.Array:
                                foreach (var item in element.EnumerateArray())
                                {
                                    var found = FindPostcode(item);
                                    if (found != null) return found;
                                }
                                break;
                            case JsonValueKind.String:
                                var s = element.GetString();
                                if (!string.IsNullOrWhiteSpace(s))
                                {
                                    var m = postcodePattern.Match(s);
                                    if (m.Success) return m.Value.Trim();
                                }
                                break;
                        }
                        return null;
                    }

                    postcode = FindPostcode(root);
                }
                catch
                {
                    // ignore parse errors, record filename with null postcode
                    postcode = null;
                }

                results.Add(new { filename, postcode });
            }

            // Ensure data directory exists
            Directory.CreateDirectory(SolutionPaths.DataDirectory);
            File.WriteAllText(outPath, JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true }));
        }

        
    }
}
