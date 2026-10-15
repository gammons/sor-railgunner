using System;
using System.Collections.Generic;
using System.Globalization;

namespace SoRCharacters.Core.Config
{
    public static class ConfigParsing
    {
        public static Dictionary<string, int> ParseIntTable(string text, out List<string> errors)
        {
            var table = new Dictionary<string, int>(StringComparer.Ordinal);
            errors = new List<string>();

            foreach (var raw in (text ?? "").Split(','))
            {
                var entry = raw.Trim();
                if (entry.Length == 0) continue;

                var colon = entry.IndexOf(':');
                var name = colon < 0 ? "" : entry.Substring(0, colon).Trim();
                var valueText = colon < 0 ? "" : entry.Substring(colon + 1).Trim();

                if (colon < 0 || name.Length == 0)
                {
                    errors.Add("Entry '" + entry + "' is not in the form Name:Number; skipped.");
                }
                else if (!int.TryParse(valueText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
                {
                    errors.Add("Entry '" + entry + "' has a non-integer value; skipped.");
                }
                else if (value < 0)
                {
                    errors.Add("Entry '" + entry + "' has a negative value; skipped.");
                }
                else
                {
                    table[name] = value;
                }
            }

            return table;
        }

        public static HashSet<string> ParseNameList(string text)
        {
            var set = new HashSet<string>(StringComparer.Ordinal);
            foreach (var part in (text ?? "").Split(','))
            {
                var name = part.Trim();
                if (name.Length > 0) set.Add(name);
            }
            return set;
        }

        public static string FormatIntTable(IDictionary<string, int> table)
        {
            var keys = new List<string>(table.Keys);
            keys.Sort(StringComparer.Ordinal);
            var parts = new List<string>(keys.Count);
            foreach (var key in keys)
            {
                parts.Add(key + ":" + table[key].ToString(CultureInfo.InvariantCulture));
            }
            return string.Join(", ", parts);
        }

        public static string FormatNameList(IEnumerable<string> names)
        {
            var sorted = new List<string>(new HashSet<string>(names, StringComparer.Ordinal));
            sorted.Sort(StringComparer.Ordinal);
            return string.Join(", ", sorted);
        }
    }
}
