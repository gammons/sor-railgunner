using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using SoRCharacters.Core.Presets;

namespace SoRCharacters.Presets
{
    internal static class PresetWriter
    {
        // Returns the updated "written" set: the incoming set plus every preset whose folder exists afterwards.
        // Names recorded earlier stay recorded even if the player deleted the folder, so they are not recreated.
        public static HashSet<string> WriteAll(IEnumerable<PresetDefinition> presets, string dataRoot, bool reset, ISet<string> written)
        {
            var result = new HashSet<string>(written, StringComparer.Ordinal);

            var charactersRoot = Path.Combine(dataRoot, "CloudData", "Characters");
            if (!Directory.Exists(charactersRoot))
            {
                Plugin.Log.LogInfo("Characters folder not found yet; presets will be written on a later launch");
                return result;
            }

            foreach (var preset in presets)
            {
                try
                {
                    if (WriteOne(preset, dataRoot, reset, written))
                    {
                        result.Add(preset.Name);
                    }
                }
                catch (Exception e)
                {
                    Plugin.Log.LogError("Preset '" + preset.Name + "' failed: " + e);
                    if (PresetRules.Validate(preset).Count == 0 && Directory.Exists(PresetRules.FolderFor(dataRoot, preset.Name)))
                    {
                        result.Add(preset.Name);
                    }
                }
            }
            return result;
        }

        // Returns true if the preset's folder exists after processing.
        private static bool WriteOne(PresetDefinition preset, string dataRoot, bool reset, ISet<string> written)
        {
            var problems = PresetRules.Validate(preset);
            if (problems.Count > 0)
            {
                foreach (var problem in problems)
                {
                    Plugin.Log.LogError("Preset '" + preset.Name + "' invalid: " + problem);
                }
                return false;
            }

            var folder = PresetRules.FolderFor(dataRoot, preset.Name);
            var folderExists = Directory.Exists(folder);
            if (PresetRules.Decide(folderExists, written.Contains(preset.Name), reset) == PresetWriteDecision.Skip)
            {
                Plugin.Log.LogInfo(folderExists
                    ? "Preset '" + preset.Name + "' exists, skipped"
                    : "Preset '" + preset.Name + "' was deleted by the player, not recreated");
                return folderExists;
            }

            byte[] bytes;
            using (var memory = new MemoryStream())
            {
                new BinaryFormatter().Serialize(memory, ToSaveData(preset));
                bytes = memory.ToArray();
            }

            Directory.CreateDirectory(folder);
            var path = PresetRules.FileFor(dataRoot, preset.Name);
            var temp = path + ".tmp";
            try
            {
                File.WriteAllBytes(temp, bytes);
                if (File.Exists(path))
                {
                    var backupDir = Path.Combine(dataRoot, "BackupData");
                    Directory.CreateDirectory(backupDir);
                    var backup = Path.Combine(backupDir,
                        "SoRCharacters-" + preset.Name + "-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".dat");
                    File.Copy(path, backup, true);
                    Plugin.Log.LogInfo("Preset '" + preset.Name + "' backed up to " + backup);
                    File.Replace(temp, path, null);
                }
                else
                {
                    File.Move(temp, path);
                }
            }
            catch
            {
                try { if (File.Exists(temp)) File.Delete(temp); } catch { }
                throw;
            }
            Plugin.Log.LogInfo("Preset '" + preset.Name + "' written to " + path);
            return true;
        }

        private static SaveCharacterData ToSaveData(PresetDefinition preset)
        {
            return new SaveCharacterData
            {
                characterName = preset.Name,
                characterDescription = preset.Description,
                bodyType = preset.BodyType,
                bodyColorName = preset.BodyColorName,
                hairType = preset.HairType,
                hairColorName = preset.HairColorName,
                facialHair = preset.FacialHair,
                skinColorName = preset.SkinColorName,
                legsColorName = preset.LegsColorName,
                eyesType = preset.EyesType,
                eyesColorName = preset.EyesColorName,
                endurance = preset.Endurance,
                strength = preset.Strength,
                accuracy = preset.Accuracy,
                speed = preset.Speed,
                specialAbility = preset.SpecialAbility,
                bigQuest = preset.BigQuest,
                traits = new List<string>(preset.Traits),
                items = new List<string>(preset.Items),
                startingHeadPiece = preset.StartingHeadPiece,
                exceededPoints = false,
            };
        }
    }
}
