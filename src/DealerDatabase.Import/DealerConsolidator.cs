using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Xml.Linq;

namespace DealerDatabase
{
    public static class DealerConsolidator
    {
        public static Dictionary<string, DealerDatabase.Data.Entities.Dealer> RunConsolidation(
                JsonElement chRoot,
                JsonElement fcaRoot,
                List<string[]> icoRows,
                List<string[]> mcRows,
                List<string[]> crawledRows,
                XDocument safDoc)
        {
            var masterMap = new Dictionary<string, DealerDatabase.Data.Entities.Dealer>();

            ProcessCompaniesHouse(chRoot, masterMap);
            ProcessFcaRegister(fcaRoot, masterMap);
            ProcessIcoRegister(icoRows, masterMap);
            ProcessMarketCheck(mcRows, masterMap);
            ProcessWebCrawl(crawledRows, masterMap);
            ProcessSafMembers(safDoc, masterMap);

            return masterMap;
        }

        private static void ProcessCompaniesHouse(JsonElement chRoot, Dictionary<string, DealerDatabase.Data.Entities.Dealer> masterMap)
        {
            if (chRoot.ValueKind == JsonValueKind.Undefined || !chRoot.TryGetProperty("items", out var chItems))
                return;

            foreach (var item in chItems.EnumerateArray())
            {
                string compNo = Normaliser.NormaliseCompanyNumber(item.GetProperty("company_number").GetString());
                string name = item.GetProperty("company_name").GetString();

                var builder = new DealerDatabase.Data.Entities.Dealer
                {
                    Key = $"CH_{compNo}",
                    CompanyNumber = compNo,
                    LegalName = name,
                    TradingName = name,
                    Name = name,
                    CompanyStatus = item.GetProperty("company_status").GetString(),
                    CompanyType = item.GetProperty("type").GetString(),
                    IncorporationDate = item.TryGetProperty("date_of_creation", out var doc) ? doc.GetString() : null,
                    MatchedSources = { "Companies House" }
                };

                if (item.TryGetProperty("registered_office_address", out var addr))
                {
                    builder.AddressLine1 = addr.TryGetProperty("address_line_1", out var a1) ? a1.GetString() : null;
                    // Locality / town from Companies House should be stored in Town
                    builder.Town = addr.TryGetProperty("locality", out var loc) ? loc.GetString() : null;
                    builder.Postcode = Normaliser.NormalisePostcode(addr.TryGetProperty("postal_code", out var pc) ? pc.GetString() : null);
                    builder.Country = addr.TryGetProperty("country", out var ctry) ? ctry.GetString() : "United Kingdom";
                    builder.AddressSource = "Companies House";
                }

                if (item.TryGetProperty("sic_codes", out var sic))
                {
                    // Normalise and deduplicate SIC codes from Companies House
                    var codes = sic
                        .EnumerateArray()
                        .Select(s => s.GetString())
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .Select(s => s.Trim())
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    // Map into DealerSicCode entities
                    builder.SicCodes = codes.Select(c => new DealerDatabase.Data.Entities.DealerSicCode { Code = c }).ToList();
                }

                if (item.TryGetProperty("officers", out var off))
                {
                    foreach (var o in off.EnumerateArray())
                    {
                        var officerName = o.TryGetProperty("name", out var n) ? n.GetString() : null;
                        if (string.IsNullOrWhiteSpace(officerName)) continue;

                        builder.Officers.Add(new DealerDatabase.Data.Entities.DealerOfficer
                        {
                            Name = officerName,
                            Role = o.TryGetProperty("officer_role", out var r) ? r.GetString() : null,
                            AppointedOn = o.TryGetProperty("appointed_on", out var a) ? a.GetString() : null,
                            Occupation = o.TryGetProperty("occupation", out var occ) ? occ.GetString() : null,
                            Nationality = o.TryGetProperty("nationality", out var nat) ? nat.GetString() : null
                        });
                    }
                }

                masterMap[builder.Key] = builder;
            }
        }

        private static void ProcessFcaRegister(JsonElement fcaRoot, Dictionary<string, DealerDatabase.Data.Entities.Dealer> masterMap)
        {
            if (fcaRoot.ValueKind == JsonValueKind.Undefined || !fcaRoot.TryGetProperty("Data", out var fcaData))
                return;

            foreach (var item in fcaData.EnumerateArray())
            {
                string frn = item.GetProperty("FRN").ToString();
                string orgName = item.GetProperty("Organisation Name").GetString();
                string normNo = Normaliser.NormaliseCompanyNumber(item.TryGetProperty("Companies House Number", out var cn) ? cn.GetString() : null);

                string key = normNo != null && masterMap.ContainsKey($"CH_{normNo}") ? $"CH_{normNo}" : $"FCA_{frn}";

                if (masterMap.TryGetValue(key, out var existing))
                {
                    existing.FcaFrn = frn;
                    existing.FcaStatus = item.GetProperty("Status").GetString();
                    existing.FcaStatusEffectiveDate = item.TryGetProperty("Status Effective Date", out var sed) ? sed.GetString() : null;
                    // Prefer FCA address when present (override Companies House address)
                    if (item.TryGetProperty("Address", out var fcaAddr))
                    {
                        var a1 = fcaAddr.TryGetProperty("Address Line 1", out var al1) ? al1.GetString() : null;
                        var a2 = fcaAddr.TryGetProperty("Address Line 2", out var al2) ? al2.GetString() : null;
                        var town = fcaAddr.TryGetProperty("Town", out var townp) ? townp.GetString() : null;
                        var pc = fcaAddr.TryGetProperty("Postcode", out var pcp) ? pcp.GetString() : null;

                        if (!string.IsNullOrWhiteSpace(a1)) existing.AddressLine1 = a1;
                        // Secondary address line from FCA
                        if (!string.IsNullOrWhiteSpace(a2)) existing.AddressLine2 = a2;
                        // Town from FCA register should be stored in Town
                        if (!string.IsNullOrWhiteSpace(town)) existing.Town = town;
                        if (!string.IsNullOrWhiteSpace(pc)) existing.Postcode = Normaliser.NormalisePostcode(pc);
                        existing.Country = "United Kingdom";
                        existing.AddressSource = "FCA Register";
                    }
                    if (item.TryGetProperty("Permissions", out var perms))
                    {
                        var permsList = perms
                            .EnumerateArray()
                            .Select(p => p.GetString())
                            .Where(p => !string.IsNullOrWhiteSpace(p))
                            .Select(p => p.Trim())
                            .Distinct(StringComparer.OrdinalIgnoreCase)
                            .Select(p => new DealerDatabase.Data.Entities.DealerFcaPermission { Permission = p })
                            .ToList();

                        existing.FcaPermissions = permsList;
                    }
                    existing.MatchedSources.Add("FCA Register");
                }
                else
                {
                    masterMap[key] = new DealerDatabase.Data.Entities.Dealer
                    {
                        Key = key,
                        CompanyNumber = normNo,
                        LegalName = orgName,
                        TradingName = orgName,
                        Name = orgName,
                        // If FCA provides an address, populate it
                        AddressLine1 = item.TryGetProperty("Address", out var newAddr) && newAddr.TryGetProperty("Address Line 1", out var nal1) ? nal1.GetString() : null,
                        AddressLine2 = item.TryGetProperty("Address", out var newAddr2) && newAddr2.TryGetProperty("Address Line 2", out var nal2) ? nal2.GetString() : null,
                        // Town from FCA
                        Town = item.TryGetProperty("Address", out var _nal) && _nal.TryGetProperty("Town", out var ntown) ? ntown.GetString() : null,
                        Postcode = item.TryGetProperty("Address", out var _naddr) && _naddr.TryGetProperty("Postcode", out var npc) ? Normaliser.NormalisePostcode(npc.GetString()) : null,
                        Country = item.TryGetProperty("Address", out var _naddr2) ? "United Kingdom" : null,
                        AddressSource = item.TryGetProperty("Address", out var _xaddr) ? "FCA Register" : null,
                        FcaFrn = frn,
                        FcaStatus = item.GetProperty("Status").GetString(),
                        FcaStatusEffectiveDate = item.TryGetProperty("Status Effective Date", out var sed) ? sed.GetString() : null,
                        MatchedSources = { "FCA Register" },
                        FcaPermissions = item.TryGetProperty("Permissions", out var pperms) ? pperms.EnumerateArray().Select(p => p.GetString()).Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Select(s => new DealerDatabase.Data.Entities.DealerFcaPermission { Permission = s }).ToList() : new List<DealerDatabase.Data.Entities.DealerFcaPermission>()
                    };
                }
            }
        }

        private static void ProcessIcoRegister(List<string[]> icoRows, Dictionary<string, DealerDatabase.Data.Entities.Dealer> masterMap)
        {
            foreach (var cols in icoRows)
            {
                if (cols.Length < 3) continue;
                string regNo = cols[0];
                string orgName = cols[1];
                string compNo = Normaliser.NormaliseCompanyNumber(cols[2]);

                // Best-effort parse of address columns. Many ICO CSV exports have:
                // [0]=regNo, [1]=orgName, [2]=companyNo, [3]=organisation_address_line1,
                // [4]=organisation_address_line2, [5]=organisation_address_line3, [6]=organisation_postcode, [8]=expiry
                string? orgAddr1 = cols.Length > 3 ? cols[3] : null;
                string? orgAddr2 = cols.Length > 4 ? cols[4] : null;
                string? orgAddr3 = cols.Length > 5 ? cols[5] : null;
                string? orgPostcode = cols.Length > 6 ? cols[6] : null;
                string? expiry = cols.Length > 8 ? cols[8] : null;

                string key = compNo != null && masterMap.ContainsKey($"CH_{compNo}") ? $"CH_{compNo}" : $"ICO_{regNo}";
                if (masterMap.TryGetValue(key, out var existing))
                {
                    existing.IcoRegistrationNumber = regNo;
                    existing.IcoExpiryDate = expiry;

                    // Only fill address fields from ICO when they are missing from Companies House / FCA
                    if (string.IsNullOrWhiteSpace(existing.AddressLine1) && !string.IsNullOrWhiteSpace(orgAddr1))
                        existing.AddressLine1 = orgAddr1;
                    if (string.IsNullOrWhiteSpace(existing.AddressLine2) && !string.IsNullOrWhiteSpace(orgAddr2))
                        existing.AddressLine2 = orgAddr2;
                    // organisation_address_line3 is often locality/town — map to Town when Town is empty
                    if (string.IsNullOrWhiteSpace(existing.Town) && !string.IsNullOrWhiteSpace(orgAddr3))
                        existing.Town = orgAddr3;
                    if (string.IsNullOrWhiteSpace(existing.Postcode) && !string.IsNullOrWhiteSpace(orgPostcode))
                        existing.Postcode = Normaliser.NormalisePostcode(orgPostcode);

                    existing.MatchedSources.Add("ICO Register");
                }
                else
                {
                    masterMap[key] = new DealerDatabase.Data.Entities.Dealer
                    {
                        Key = key,
                        CompanyNumber = compNo,
                        LegalName = orgName,
                        TradingName = orgName,
                        Name = orgName,
                        IcoRegistrationNumber = regNo,
                        IcoExpiryDate = expiry,
                        // Populate address fields from ICO if provided
                        AddressLine1 = string.IsNullOrWhiteSpace(orgAddr1) ? null : orgAddr1,
                        AddressLine2 = string.IsNullOrWhiteSpace(orgAddr2) ? null : orgAddr2,
                        Town = string.IsNullOrWhiteSpace(orgAddr3) ? null : orgAddr3,
                        Postcode = string.IsNullOrWhiteSpace(orgPostcode) ? null : Normaliser.NormalisePostcode(orgPostcode),
                        MatchedSources = { "ICO Register" }
                    };
                }
            }
        }

        private static void ProcessMarketCheck(List<string[]> mcRows, Dictionary<string, DealerDatabase.Data.Entities.Dealer> masterMap)
        {
            foreach (var cols in mcRows)
            {
                if (cols.Length < 27) continue;
                string sellerName = cols[1];
                string postcode = Normaliser.NormalisePostcode(cols[7]);
                string phone = Normaliser.NormalisePhone(cols[8]);
                string website = Normaliser.NormaliseDomain(cols[9]);
                // Best-effort street/city extraction from MarketCheck CSV (defensive indices)
                static string? PickFirst(params string?[] candidates)
                {
                    foreach (var c in candidates)
                    {
                        if (string.IsNullOrWhiteSpace(c)) continue;
                        var t = c.Trim();
                        if (string.Equals(t, "nan", StringComparison.OrdinalIgnoreCase)) continue;
                        if (t == "\"\"" || t == "\"") continue;
                        return t;
                    }
                    return null;
                }

                // Common candidate positions observed in different exports — prefer cols[4], fall back to 3,6
                string? street = PickFirst(cols.Length > 4 ? cols[4] : null, cols.Length > 3 ? cols[3] : null, cols.Length > 6 ? cols[6] : null);
                // City candidates: cols[5], cols[6], cols[4]
                string? city = PickFirst(cols.Length > 5 ? cols[5] : null, cols.Length > 6 ? cols[6] : null, cols.Length > 4 ? cols[4] : null);

                var match = masterMap.Values.FirstOrDefault(b => (b.Postcode != null && b.Postcode == postcode) || (website != null && b.Website == website));
                // Debug: emit parsed MarketCheck fields for known problematic rows (helps trace mapping)
                try
                {
                    if (string.Equals(cols[0], "MC508842", StringComparison.OrdinalIgnoreCase) || sellerName?.Contains("Carver", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        Console.WriteLine($"[MarketCheck-debug] RowId={cols[0]} Seller='{sellerName}' street='{street}' city='{city}' postcode='{postcode}' website='{website}'");
                    }
                }
                catch { }
                if (match != null)
                {
                    match.TradingName ??= sellerName;
                    match.SellerType = cols[2];
                    match.FranchiseMake = cols[3].Equals("nan", StringComparison.OrdinalIgnoreCase) ? null : cols[3];
                    match.Phone ??= phone;
                    // If address parts are missing from other sources, fill from MarketCheck
                    // If existing values are empty or invalid, overwrite with MarketCheck values
                    if (!string.IsNullOrWhiteSpace(street) && (string.IsNullOrWhiteSpace(match.AddressLine1) || string.Equals(match.AddressLine1, "nan", StringComparison.OrdinalIgnoreCase)))
                        match.AddressLine1 = street;
                    if (!string.IsNullOrWhiteSpace(city) && (string.IsNullOrWhiteSpace(match.Town) || string.Equals(match.Town, "nan", StringComparison.OrdinalIgnoreCase)))
                        match.Town = city;
                    if (!string.IsNullOrWhiteSpace(postcode) && (string.IsNullOrWhiteSpace(match.Postcode) || string.Equals(match.Postcode, "nan", StringComparison.OrdinalIgnoreCase)))
                        match.Postcode = postcode;
                    match.Website ??= website;
                    match.Email ??= cols[10].Equals("nan", StringComparison.OrdinalIgnoreCase) ? null : cols[10];
                    if (int.TryParse(cols[11], out var inv)) match.InventoryCount = inv;
                    if (decimal.TryParse(cols[12], out var avgPrice)) match.AvgListedPrice = avgPrice;
                    if (int.TryParse(cols[14], out var days)) match.AvgDaysInStock = days;
                    if (int.TryParse(cols[15], out var sold)) match.SoldLast30Days = sold;
                    match.StockFeedProvider = cols[26];
                    match.MatchedSources.Add("MarketCheck");
                }
                else
                {
                    // Use the CSV's first column (cols[0]) as the stable key when available.
                    // Many MarketCheck exports have an identifier like 'MC508842' in cols[0].
                    var rawId = cols.Length > 0 ? cols[0]?.Trim() : null;
                    var key = !string.IsNullOrWhiteSpace(rawId) ? rawId : $"MC_{Guid.NewGuid().ToString()[..8]}";
                    var builder = new DealerDatabase.Data.Entities.Dealer
                    {
                        Key = key,
                        TradingName = sellerName,
                        LegalName = sellerName,
                        SellerType = cols[2],
                        FranchiseMake = cols[3].Equals("nan", StringComparison.OrdinalIgnoreCase) ? null : cols[3],
                        AddressLine1 = string.IsNullOrWhiteSpace(street) ? null : street,
                        Town = string.IsNullOrWhiteSpace(city) ? null : city,
                        Postcode = postcode,
                        Phone = phone,
                        Website = website,
                        Email = cols[10].Equals("nan", StringComparison.OrdinalIgnoreCase) ? null : cols[10],
                        StockFeedProvider = cols[26],
                        MatchedSources = { "MarketCheck" }
                    };
                    if (int.TryParse(cols[11], out var inv2)) builder.InventoryCount = inv2;
                    if (decimal.TryParse(cols[12], out var avgPrice2)) builder.AvgListedPrice = avgPrice2;
                    if (int.TryParse(cols[14], out var days2)) builder.AvgDaysInStock = days2;
                    if (int.TryParse(cols[15], out var sold2)) builder.SoldLast30Days = sold2;
                    masterMap[key] = builder;
                }
            }
        }

        private static void ProcessWebCrawl(List<string[]> crawledRows, Dictionary<string, DealerDatabase.Data.Entities.Dealer> masterMap)
        {
            foreach (var cols in crawledRows)
            {
                if (cols.Length < 30) continue;
                string website = Normaliser.NormaliseDomain(cols[2]);
                var match = masterMap.Values.FirstOrDefault(b => website != null && b.Website == website);
                if (match != null)
                {
                    match.TradingName ??= cols[9].Equals("nan", StringComparison.OrdinalIgnoreCase) ? null : cols[9];
                    match.VatNumber ??= cols[16].Equals("nan", StringComparison.OrdinalIgnoreCase) ? null : cols[16];
                    decimal.TryParse(cols[23], out var gr); if (gr > 0) match.GoogleRating = gr;
                    int.TryParse(cols[24], out var gcr); if (gcr > 0) match.GoogleReviewCount = gcr;
                    decimal.TryParse(cols[25], out var ts); if (ts > 0) match.TrustpilotScore = ts;
                    match.FacebookUrl ??= cols[26].Equals("nan", StringComparison.OrdinalIgnoreCase) ? null : cols[26];
                    match.InstagramUrl ??= cols[27].Equals("nan", StringComparison.OrdinalIgnoreCase) ? null : cols[27];
                    match.WebsitePlatform ??= cols[21].Equals("nan", StringComparison.OrdinalIgnoreCase) ? null : cols[21];
                    match.WarrantyOffered ??= cols[30].Equals("nan", StringComparison.OrdinalIgnoreCase) ? null : cols[30];
                    match.MatchedSources.Add("Web Crawl");
                }
            }
        }

        private static void ProcessSafMembers(XDocument safDoc, Dictionary<string, DealerDatabase.Data.Entities.Dealer> masterMap)
        {
            if (safDoc == null) return;

            foreach (var member in safDoc.Descendants("Member"))
            {
                string postcode = Normaliser.NormalisePostcode(member.Element("Postcode")?.Value);
                var match = masterMap.Values.FirstOrDefault(b => b.Postcode != null && b.Postcode == postcode);
                if (match != null)
                {
                    match.SafStatus = member.Element("Status")?.Value;
                    match.SafExpiryDate = member.Element("Expiry")?.Value;
                    match.MatchedSources.Add("SAF Members");
                }
            }
        }
    }
}