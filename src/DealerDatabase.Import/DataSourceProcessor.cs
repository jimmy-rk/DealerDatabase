using DealerDatabase.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace DealerDatabase
{
    public static class DataSourceProcessor
    {
        public static JsonElement LoadJsonSource(string filename)
        {
            string path = Path.Combine(SolutionPaths.DataDirectory, filename);
            if (!File.Exists(path)) return default;
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            return doc.RootElement.Clone();
        }

        public static XDocument LoadXmlSource(string filename)
        {
            string path = Path.Combine(SolutionPaths.DataDirectory, filename);
            return File.Exists(path) ? XDocument.Load(path) : null;
        }


        public static (string[]? header, List<string[]> rows) LoadCsvSourceWithHeader(string filename)
        {
            string path = Path.Combine(SolutionPaths.DataDirectory, filename);
            if (!File.Exists(path)) return (null, new List<string[]>());

            var lines = new List<string>();
            try
            {
                using var fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var sr = new StreamReader(fs);
                string? line = sr.ReadLine();
                if (line == null) return (null, new List<string[]>());
                // first line is header
                var header = ParseCsvLine(line);
                while ((line = sr.ReadLine()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    lines.Add(line);
                }
                var rows = lines.Select(l => ParseCsvLine(l)).ToList();
                return (header, rows);
            }
            catch
            {
                var all = File.ReadAllLines(path).Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();
                if (all.Length == 0) return (null, new List<string[]>());
                var header = ParseCsvLine(all[0]);
                var rows = all.Skip(1).Select(l => ParseCsvLine(l)).ToList();
                return (header, rows);
            }
        }

        public static List<string[]> LoadCsvSource(string filename)
        {
            string path = Path.Combine(SolutionPaths.DataDirectory, filename);
            if (!File.Exists(path)) return new();

            // Basic robust CSV line splitting (skipping header).
            // Open with shared read to avoid failures when files are temporarily locked by other processes (e.g., Excel).
            var lines = new List<string>();
            try
            {
                using var fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var sr = new StreamReader(fs);
                string? line;
                bool first = true;
                while ((line = sr.ReadLine()) != null)
                {
                    if (first)
                    {
                        first = false; // skip header
                        continue;
                    }
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    lines.Add(line);
                }
            }
            catch
            {
                // Fallback to simple read if shared-open fails for any reason
                lines.AddRange(File.ReadAllLines(path).Skip(1).Where(l => !string.IsNullOrWhiteSpace(l)));
            }

            return lines.Select(line => ParseCsvLine(line)).ToList();
        }

        private static string[] ParseCsvLine(string line)
        {
            // Handles comma separation while respecting basic quoted values if present
            var tokens = new List<string>();
            bool inQuotes = false;
            int startIndex = 0;

            for (int i = 0; i < line.Length; i++)
            {
                if (line[i] == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (line[i] == ',' && !inQuotes)
                {
                    tokens.Add(line.Substring(startIndex, i - startIndex).Trim('"'));
                    startIndex = i + 1;
                }
            }
            tokens.Add(line.Substring(startIndex).Trim('"'));
            return tokens.ToArray();
        }
    }
}
