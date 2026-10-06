using System;
using System.Linq;

namespace DealerDatabase
{
    public static class Normaliser
    {
        public static string NormaliseCompanyNumber(string regNo)
        {
            if (string.IsNullOrWhiteSpace(regNo) || regNo.Equals("nan", StringComparison.OrdinalIgnoreCase)) return null;
            return regNo.Trim().ToUpperInvariant().PadLeft(8, '0');
        }

        public static string NormalisePostcode(string pc)
        {
            if (string.IsNullOrWhiteSpace(pc) || pc.Equals("nan", StringComparison.OrdinalIgnoreCase)) return null;
            string clean = pc.Replace(" ", "").ToUpperInvariant();
            return clean.Length > 3 ? clean.Insert(clean.Length - 3, " ") : clean;
        }

        public static string NormalisePhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone) || phone.Equals("nan", StringComparison.OrdinalIgnoreCase)) return null;
            string clean = new string(phone.Where(char.IsDigit).ToArray());
            if (clean.StartsWith("0")) clean = "44" + clean[1..];
            return "+" + clean;
        }

        public static string NormaliseDomain(string url)
        {
            if (string.IsNullOrWhiteSpace(url) || url.Equals("nan", StringComparison.OrdinalIgnoreCase)) return null;
            try
            {
                var uri = new Uri(url.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? url : "https://" + url);
                return uri.Host.Replace("www.", "", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return url.ToLowerInvariant();
            }
        }
    }
}