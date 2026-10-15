using BepInEx.Configuration;
using SoRCharacters.Core.Railgunner;

namespace SoRCharacters.Railgunner
{
    internal static class RailgunnerConfig
    {
        private const string Section = "Railgunner";
        private const float MapWidthTiles = 160f;
        private const float MaxBeamWidthTiles = 8f;
        private const float MaxEffectSeconds = 5f;
        private const int MaxSparksPerTile = 50;
        private const float MaxFlashSeconds = 1f;

        public static RailgunnerSettings Bind(ConfigFile config)
        {
            var s = new RailgunnerSettings();

            s.ChargeSeconds = BindFloat(config, "ChargeSeconds", s.ChargeSeconds, "Seconds the special ability must be held to charge a railshot.");
            s.ChargedSpeedMultiplier = BindFloat(config, "ChargedSpeedMultiplier", s.ChargedSpeedMultiplier, "Movement speed multiplier while charging up, before \"READY\" (0.5 = half speed).");
            s.ArmedSpeedMultiplier = BindFloat(config, "ArmedSpeedMultiplier", s.ArmedSpeedMultiplier, "Movement speed multiplier once charged (\"READY\"), at most 1 (0.15 = 15% speed).", 1f);
            s.ArmedMaxSeconds = BindFloat(config, "ArmedMaxSeconds", s.ArmedMaxSeconds, "Seconds he can stay charged (\"READY\") before the railshot fires on its own.");
            s.AimTurnDegreesPerSecond = BindFloat(config, "AimTurnDegreesPerSecond", s.AimTurnDegreesPerSecond, "Once charged, the most the aim can turn toward the mouse or stick, in degrees per second.");
            s.ChargedDamageTakenMultiplier = BindFloat(config, "ChargedDamageTakenMultiplier", s.ChargedDamageTakenMultiplier, "Damage taken multiplier while charged (1.5 = 50% more damage).");
            s.RechargeSeconds = BindFloat(config, "RechargeSeconds", s.RechargeSeconds, "Seconds before the railshot can be charged again after firing.");
            s.BeamWidthTiles = BindFloat(config, "BeamWidthTiles", s.BeamWidthTiles, "Width of the railshot beam in tiles (at most 8).", MaxBeamWidthTiles);
            s.BeamRangeTiles = BindFloat(config, "BeamRangeTiles", s.BeamRangeTiles, "Length of the railshot beam in tiles (at most 160, the width of the map).", MapWidthTiles);
            s.LineEmUpKills = BindInt(config, "LineEmUpKills", s.LineEmUpKills, "Kills a single railshot needs to count for the Line 'Em Up quest.");
            s.LineEmUpShots = BindInt(config, "LineEmUpShots", s.LineEmUpShots, "Railshots with enough kills needed per level for the Line 'Em Up quest.");
            s.ShakeSeconds = BindFloat(config, "ShakeSeconds", s.ShakeSeconds, "Seconds of screen shake when the railshot fires.");
            s.ShakeOffset = BindFloat(config, "ShakeOffset", s.ShakeOffset, "Strength of the screen shake when the railshot fires.");
            s.HumIntervalSeconds = BindFloat(config, "HumIntervalSeconds", s.HumIntervalSeconds, "Seconds between the humming noises that draw NPCs while charging.");
            s.HumLoudAfterSeconds = BindFloat(config, "HumLoudAfterSeconds", s.HumLoudAfterSeconds, "Seconds after the charge completes before the hum gets loudest.");
            s.ReadyToneVolume = BindFloat(config, "ReadyToneVolume", s.ReadyToneVolume, "Volume of the looping tone that plays while the railshot is charged, as a multiplier of the sound volume (at most 1).", 1f);
            s.SupercapacitorsRechargeMultiplier = BindFloat(config, "SupercapacitorsRechargeMultiplier", s.SupercapacitorsRechargeMultiplier, "Multiplier on the railshot recharge time with the Supercapacitors trait (0.6667 = a third faster, at most 1).", 1f);
            s.AlertSeconds = BindFloat(config, "AlertSeconds", s.AlertSeconds, "Seconds an NPC who saw him charging stays alerted (\"!\") before turning hostile.");
            s.BeamFadeSeconds = BindFloat(config, "BeamFadeSeconds", s.BeamFadeSeconds, "Seconds the blue-white beam takes to fade out after the railshot fires (at most 5).", MaxEffectSeconds);
            s.AfterglowSeconds = BindFloat(config, "AfterglowSeconds", s.AfterglowSeconds, "Seconds the faint blue afterglow along the beam's path takes to fade out (at most 5).", MaxEffectSeconds);
            s.FlashSeconds = BindFloat(config, "FlashSeconds", s.FlashSeconds, "Seconds the big white muzzle flash along the beam takes to fade out (at most 1).", MaxFlashSeconds);
            s.SparksPerTile = BindInt(config, "SparksPerTile", s.SparksPerTile, "Sparks sprayed per tile along the beam (0 = none along the beam, at most 50; the bursts at kills, cut walls and the end stay).", 0, MaxSparksPerTile);
            s.ChargeSound = BindString(config, "ChargeSound", s.ChargeSound, "Name of the game sound played when charging starts.");
            s.FireSound = BindString(config, "FireSound", s.FireSound, "Name of the game sound played when the railshot fires.");
            s.FireSound2 = BindString(config, "FireSound2", s.FireSound2, "Name of a second game sound layered over the first when the railshot fires.");

            return s;
        }

        private static string BindString(ConfigFile config, string key, string fallback, string description)
        {
            var value = config.Bind(Section, key, fallback, description).Value;
            if (string.IsNullOrWhiteSpace(value))
            {
                Warn(key, $"value is empty; using default '{fallback}'.");
                return fallback;
            }
            return value.Trim();
        }

        private static void Warn(string key, string message)
        {
            Plugin.Log.LogWarning($"[Railgunner] Config '{key}': {message}");
        }

        private static int BindInt(ConfigFile config, string key, int fallback, string description, int min = 1, int max = int.MaxValue)
        {
            var value = config.Bind(Section, key, fallback, description).Value;
            if (value < min)
            {
                Warn(key, $"{value} is not at least {min}; using default {fallback}.");
                return fallback;
            }
            if (value > max)
            {
                Warn(key, $"{value} is above the maximum {max}; using {max}.");
                return max;
            }
            return value;
        }

        private static float BindFloat(ConfigFile config, string key, float fallback, string description, float max = float.PositiveInfinity)
        {
            var value = config.Bind(Section, key, fallback, description).Value;
            if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0f)
            {
                Warn(key, $"{value} is not a finite number greater than zero; using default {fallback}.");
                return fallback;
            }
            if (value > max)
            {
                Warn(key, $"{value} is above the maximum {max}; using {max}.");
                return max;
            }
            return value;
        }
    }
}
