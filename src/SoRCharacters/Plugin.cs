using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using RogueLibsCore;
using SoRCharacters.Core.Presets;
using SoRCharacters.Presets;
using SoRCharacters.Railgunner;
using UnityEngine;

namespace SoRCharacters
{
    [BepInPlugin("grant.sor.characters", "SoRCharacters", Plugin.Version)]
    [BepInDependency(RogueLibs.GUID, RogueLibs.CompiledVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string Version = "0.9.0";

        internal static ManualLogSource Log = null!;
        internal static ModConfig Settings = null!;
        internal static Harmony Harmony = new Harmony("grant.sor.characters");

        private void Awake()
        {
            Log = Logger;
            Settings = new ModConfig(Config);

            try
            {
                RailgunnerModule.Register(Harmony, Config);
            }
            catch (Exception e)
            {
                Log.LogError("SoRCharacters disabled: " + e);
                return;
            }

            _presetsPending = true;

            Log.LogInfo("SoRCharacters " + Version + " loaded");
        }

        private readonly HashSet<string> _loggedTickErrors = new HashSet<string>();
        private bool _presetsPending;

        private void Update()
        {
            if (_presetsPending)
            {
                TryWritePresets();
            }
            RunOnce("tick", RailgunnerModule.Tick);
        }

        // The game picks its save folder from fields on GameController (usingMyDocuments is serialized, dataFolder is
        // set in GameController.Awake), so wait until it exists and use the same rule.
        private void TryWritePresets()
        {
            var gc = GameController.gameController;
            if (gc == null || string.IsNullOrEmpty(gc.dataFolder))
            {
                return;
            }
            _presetsPending = false;

            try
            {
                var root = DataRoot.Choose(gc.usingMyDocuments, gc.macVersion, gc.linuxVersion, gc.usingUWP,
                    Application.persistentDataPath,
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/" + gc.dataFolder);
                Log.LogInfo("Preset root: " + root);

                var resetPresets = Settings.ResetPresets.Value;
                var written = PresetRules.ParseWritten(Settings.WrittenPresets.Value);
                var updated = PresetWriter.WriteAll(new[] { RailgunnerPreset.Create() }, root, resetPresets, written);
                Settings.WrittenPresets.Value = PresetRules.FormatWritten(updated);
                if (resetPresets)
                {
                    Settings.ResetPresets.Value = false;
                }
                Config.Save();
            }
            catch (Exception e)
            {
                Log.LogError("Writing presets failed: " + e);
            }
        }

        private void RunOnce(string what, Action action, string module = "Railgunner")
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                if (_loggedTickErrors.Add(ErrorKey(module + " " + what, e)))
                {
                    Log.LogError($"[{module}] {what} failed: {e}");
                }
            }
        }

        private static string ErrorKey(string what, Exception e)
        {
            var trace = e.StackTrace;
            var newline = trace?.IndexOf('\n') ?? -1;
            var firstFrame = trace == null ? "" : (newline < 0 ? trace : trace.Substring(0, newline)).Trim();
            return what + "|" + e.GetType().FullName + "|" + firstFrame;
        }

        internal static byte[] LoadResource(string fileName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var expected = typeof(Plugin).Namespace + ".Resources." + fileName;
            foreach (var name in assembly.GetManifestResourceNames())
            {
                if (!string.Equals(name, expected, StringComparison.Ordinal)) continue;
                using var stream = assembly.GetManifestResourceStream(name)!;
                using var buffer = new MemoryStream();
                stream.CopyTo(buffer);
                return buffer.ToArray();
            }
            throw new FileNotFoundException("Embedded resource not found: " + expected);
        }
    }
}
