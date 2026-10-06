using DealerDatabase.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DealerDatabase
{
    public static class NormaliseMergeConsolidatedData
    {
        public static List<Dealer> GetNormaliseMergeConsolidatedData(Dictionary<string, Dealer> masterMap)
        {
            // Normalize and merge masterMap entries that refer to the same company name
            static string NormalizeDisplayNameForMerge(string? s)
            {
                if (string.IsNullOrWhiteSpace(s)) return string.Empty;
                // Normalize common abbreviations like "LTD" -> "LIMITED" for better grouping, keep original casing/spacing trimmed.
                var replaced = Regex.Replace(s, @"\bLTD\.?\b", "LIMITED", RegexOptions.IgnoreCase);
                return replaced.Trim();
            }

            static string NormalizeKeyFromName(string? s)
            {
                if (string.IsNullOrWhiteSpace(s)) return string.Empty;
                var chars = s.Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray();
                return new string(chars);
            }

            var grouped = masterMap.Values
                .GroupBy(m => NormalizeKeyFromName(NormalizeDisplayNameForMerge(m.TradingName ?? m.LegalName ?? m.Key)))
                .Where(g => !string.IsNullOrWhiteSpace(g.Key))
                .ToList();

            var mergedMasters = new List<Dealer>();
            foreach (var grp in grouped)
            {
                var members = grp.ToList();
                if (members.Count == 1)
                {
                    var single = members[0];
                    single.Name = NormalizeDisplayNameForMerge(single.Name ?? single.TradingName ?? single.LegalName ?? single.Key);
                    mergedMasters.Add(single);
                    continue;
                }

                var priority = members
                    .OrderByDescending(m => string.Equals(m.AddressSource, "FCA Register", StringComparison.OrdinalIgnoreCase))
                    .ThenByDescending(m => string.Equals(m.AddressSource, "Companies House", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                var merged = priority.First();
                merged.Name = NormalizeDisplayNameForMerge(members.Select(m => m.TradingName ?? m.LegalName ?? m.Key).FirstOrDefault(n => !string.IsNullOrWhiteSpace(n)));

                string? PickFirstString(params string?[] vals)
                {
                    foreach (var v in vals)
                        if (!string.IsNullOrWhiteSpace(v) && !string.Equals(v, "nan", StringComparison.OrdinalIgnoreCase)) return v;
                    return null;
                }

                merged.Key = members.Select(m => m.Key).FirstOrDefault(k => !string.IsNullOrWhiteSpace(k));
                merged.CompanyNumber = members.Select(m => m.CompanyNumber).FirstOrDefault(c => !string.IsNullOrWhiteSpace(c));
                merged.LegalName = PickFirstString(priority.Select(p => p.LegalName).ToArray()) ?? members.Select(m => m.LegalName).FirstOrDefault();
                merged.TradingName = PickFirstString(priority.Select(p => p.TradingName).ToArray()) ?? members.Select(m => m.TradingName).FirstOrDefault();
                merged.CompanyStatus = PickFirstString(members.Select(m => m.CompanyStatus).ToArray());
                merged.CompanyType = PickFirstString(members.Select(m => m.CompanyType).ToArray());
                merged.IncorporationDate = PickFirstString(members.Select(m => m.IncorporationDate).ToArray());

                merged.AddressLine1 = PickFirstString(priority.Select(p => p.AddressLine1).ToArray()) ?? members.Select(m => m.AddressLine1).FirstOrDefault();
                merged.AddressLine2 = PickFirstString(priority.Select(p => p.AddressLine2).ToArray()) ?? members.Select(m => m.AddressLine2).FirstOrDefault();
                merged.Town = PickFirstString(priority.Select(p => p.Town).ToArray()) ?? members.Select(m => m.Town).FirstOrDefault();
                merged.Postcode = PickFirstString(priority.Select(p => p.Postcode).ToArray()) ?? members.Select(m => m.Postcode).FirstOrDefault();
                merged.Country = PickFirstString(priority.Select(p => p.Country).ToArray()) ?? members.Select(m => m.Country).FirstOrDefault();
                merged.AddressSource = PickFirstString(priority.Select(p => p.AddressSource).ToArray()) ?? members.Select(m => m.AddressSource).FirstOrDefault();

                merged.Phone = PickFirstString(members.Select(m => m.Phone).ToArray());
                merged.Email = PickFirstString(members.Select(m => m.Email).ToArray());
                merged.Website = PickFirstString(members.Select(m => m.Website).ToArray());
                merged.StockFeedProvider = PickFirstString(members.Select(m => m.StockFeedProvider).ToArray());

                merged.InventoryCount = members.Select(m => m.InventoryCount).FirstOrDefault(v => v != 0);
                merged.AvgListedPrice = members.Select(m => m.AvgListedPrice).FirstOrDefault(v => v != 0);
                merged.AvgDaysInStock = members.Select(m => m.AvgDaysInStock).FirstOrDefault(v => v != 0);
                merged.SoldLast30Days = members.Select(m => m.SoldLast30Days).FirstOrDefault(v => v != 0);

                // Consolidate SIC codes, officers and FCA permissions (deduplicated by key fields)
                merged.SicCodes = members.SelectMany(m => m.SicCodes ?? Enumerable.Empty<DealerDatabase.Data.Entities.DealerSicCode>())
                    .GroupBy(s => (s?.Code ?? string.Empty), StringComparer.OrdinalIgnoreCase)
                    .Select(g => new DealerDatabase.Data.Entities.DealerSicCode { Code = g.Key })
                    .ToList();

                merged.Officers = members.SelectMany(m => m.Officers ?? Enumerable.Empty<DealerDatabase.Data.Entities.DealerOfficer>())
                    .GroupBy(o => (o?.Name ?? string.Empty), StringComparer.OrdinalIgnoreCase)
                    .Select(g => g.First())
                    .ToList();

                merged.FcaPermissions = members.SelectMany(m => m.FcaPermissions ?? Enumerable.Empty<DealerDatabase.Data.Entities.DealerFcaPermission>())
                    .GroupBy(p => (p?.Permission ?? string.Empty), StringComparer.OrdinalIgnoreCase)
                    .Select(g => new DealerDatabase.Data.Entities.DealerFcaPermission { Permission = g.Key })
                    .ToList();

                mergedMasters.Add(merged);


            }
            return mergedMasters;

        }
    }
}
