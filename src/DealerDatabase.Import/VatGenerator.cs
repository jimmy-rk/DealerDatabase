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


        /// <summary>
        /// Apply VAT numbers to dealers in the database by matching postcodes found in the
        /// aggregated vat.json file. For any vat.json entry with a postcode, this method will
        /// set Dealer.VatNumber = postcode for dealers whose Postcode matches (after normalisation).
        /// </summary>
        public static async System.Threading.Tasks.Task ApplyVatNumbersAsync(DealerDbContext db)
        {
            var outPath = Path.Combine(SolutionPaths.DataDirectory, "vat.json");
            if (!File.Exists(outPath)) return;

            List<(string filename, string? postcode)> entries;
            try
            {
                var txt = File.ReadAllText(outPath);
                entries = JsonSerializer.Deserialize<List<Dictionary<string, object?>>>(txt)!
                    .Select(d => (
                        filename: d.ContainsKey("filename") && d["filename"] != null ? d["filename"].ToString() ?? string.Empty : string.Empty,
                        postcode: d.ContainsKey("postcode") && d["postcode"] != null ? d["postcode"].ToString() : null
                    ))
                    .ToList();
            }
            catch
            {
                return;
            }

            // Build map of normalized postcode -> filename (we'll store filename into VatNumber)
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var e in entries)
            {
                if (string.IsNullOrWhiteSpace(e.postcode)) continue;
                var norm = Normaliser.NormalisePostcode(e.postcode);
                if (string.IsNullOrWhiteSpace(norm)) continue;
                var fname = e.filename?.Trim();
                if (string.IsNullOrWhiteSpace(fname)) continue;
                if (!map.ContainsKey(norm)) map[norm] = fname!;
            }

            if (map.Count == 0) return;

            var dealers = await db.Dealers.ToListAsync();
            var changed = false;
            foreach (var dealer in dealers)
            {
                if (string.IsNullOrWhiteSpace(dealer.Postcode)) continue;
                var dnorm = Normaliser.NormalisePostcode(dealer.Postcode);
                if (string.IsNullOrWhiteSpace(dnorm)) continue;
                if (map.TryGetValue(dnorm, out var vatValue))
                {
                    if (string.IsNullOrWhiteSpace(dealer.VatNumber) || !string.Equals(dealer.VatNumber, vatValue, StringComparison.Ordinal))
                    {
                        dealer.VatNumber = vatValue;
                        changed = true;
                    }
                }
            }

            if (changed)
            {
                await db.SaveChangesAsync();
            }
        }


    }
}
