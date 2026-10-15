using BepInEx.Configuration;

namespace SoRCharacters
{
    internal sealed class ModConfig
    {
        public ConfigEntry<bool> ResetPresets { get; }
        public ConfigEntry<string> WrittenPresets { get; }

        public ModConfig(ConfigFile config)
        {
            ResetPresets = config.Bind(
                "Presets",
                "ResetPresets",
                false,
                "Rewrite the mod's character presets on next start, then turn this off.");

            WrittenPresets = config.Bind(
                "Presets",
                "Written",
                "",
                "Presets the mod has already created. A preset listed here is not recreated if you delete it. Managed by the mod.");
        }
    }
}
