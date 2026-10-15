using System;
using System.Collections.Generic;
using System.IO;

namespace SoRCharacters.Core.Presets
{
    public enum PresetWriteDecision
    {
        Write,
        Skip,
    }

    public static class PresetRules
    {
        private const int MinStat = 0;
        private const int MaxStat = 3;

        private const char WrittenSeparator = '|';

        public static PresetWriteDecision Decide(bool folderExists, bool previouslyWritten, bool resetRequested)
        {
            return resetRequested || (!folderExists && !previouslyWritten)
                ? PresetWriteDecision.Write
                : PresetWriteDecision.Skip;
        }

        public static HashSet<string> ParseWritten(string value)
        {
            var set = new HashSet<string>(StringComparer.Ordinal);
            foreach (var part in (value ?? "").Split(WrittenSeparator))
            {
                var name = part.Trim();
                if (name.Length > 0) set.Add(name);
            }
            return set;
        }

        public static string FormatWritten(IEnumerable<string> names)
        {
            var sorted = new List<string>(new HashSet<string>(names, StringComparer.Ordinal));
            sorted.Sort(StringComparer.Ordinal);
            return string.Join(WrittenSeparator.ToString(), sorted);
        }

        public static string FolderFor(string dataRoot, string name)
        {
            return Path.Combine(dataRoot, "CloudData", "Characters", name);
        }

        public static string FileFor(string dataRoot, string name)
        {
            return Path.Combine(FolderFor(dataRoot, name), name + ".dat");
        }

        public static IReadOnlyList<string> Validate(PresetDefinition preset)
        {
            var problems = new List<string>();

            if (string.IsNullOrWhiteSpace(preset.Name))
            {
                problems.Add("Name must not be blank.");
            }
            else if (preset.Name == "." || preset.Name == ".." || ContainsPathCharacter(preset.Name))
            {
                problems.Add("Name '" + preset.Name + "' must not contain path characters or '|', or be '.' or '..'.");
            }

            CheckStat(problems, "Endurance", preset.Endurance);
            CheckStat(problems, "Strength", preset.Strength);
            CheckStat(problems, "Accuracy", preset.Accuracy);
            CheckStat(problems, "Speed", preset.Speed);

            if (string.IsNullOrWhiteSpace(preset.SpecialAbility))
            {
                problems.Add("SpecialAbility must not be blank.");
            }

            return problems;
        }

        private static bool ContainsPathCharacter(string name)
        {
            return name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
                || name.IndexOfAny(new[] { '/', '\\', ':', WrittenSeparator }) >= 0;
        }

        private static void CheckStat(List<string> problems, string label, int value)
        {
            if (value < MinStat || value > MaxStat)
            {
                problems.Add(label + " must be between " + MinStat + " and " + MaxStat + " (was " + value + ").");
            }
        }
    }
}
