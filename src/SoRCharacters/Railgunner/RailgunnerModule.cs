using System.Collections.Generic;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using RogueLibsCore;
using SoRCharacters.Core;
using SoRCharacters.Core.Railgunner;
using UnityEngine;

namespace SoRCharacters.Railgunner
{
    internal static class RailgunnerModule
    {
        public static RailgunnerSettings Settings { get; private set; } = new RailgunnerSettings();

        private const float SeenScanIntervalSeconds = 0.25f;
        private const string CustomChargeName = "SoRRailgunCharge";
        private const string CustomFireName = "SoRRailgunFire";
        private static readonly string[] SoundExtensions = { ".wav", ".ogg", ".mp3" };

        public const string ReadyToneName = "SoRRailgunReady";
        private static bool _readyToneLoaded;
        private static bool _customChargeLoaded;
        private static bool _customFireLoaded;

        public static bool ReadyToneLoaded => _readyToneLoaded && ModAudio.IsRegistered(ReadyToneName);

        public static string ActiveChargeSound => _customChargeLoaded && ModAudio.IsRegistered(CustomChargeName) ? CustomChargeName : Settings.ChargeSound;
        public static string ActiveFireSound => _customFireLoaded && ModAudio.IsRegistered(CustomFireName) ? CustomFireName : Settings.FireSound;

        public static void Register(Harmony harmony, ConfigFile config)
        {
            Settings = RailgunnerConfig.Bind(config);
            LoadCustomSounds();
            LoadReadyTone();

            RogueLibs.CreateCustomTrait<RailgunnerKit>()
                .WithName(new CustomNameInfo("Railgunner Kit"))
                .WithDescription(new CustomNameInfo("Marks the Railgunner. Charges a railshot that kills everything in a straight line."))
                .WithUnlock(new TraitUnlock(true) { UnlockCost = 0, CharacterCreationCost = 0, IsAvailableInCC = true });

            // Level-up offers for a special ability's upgrades come from Unlock.specialAbilities (ScrollingMenu.cs:1683), which
            // only matches cost3 (= CharacterCreationCost) against the trait tiers 2, 3, 5, 7 and 10 (ScrollingMenu.cs:1596-1617).
            // Like the vanilla Stomp upgrades (Unlocks.cs:2449-2463) they are unavailable, which hides them from the Character
            // Creator and the trait lists; TraitOK still accepts them (ScrollingMenu.cs:2395).
            RogueLibs.CreateCustomTrait<RailgunSupercapacitors>()
                .WithName(new CustomNameInfo("Supercapacitors"))
                .WithDescription(new CustomNameInfo($"Railshot recharges in {Settings.RechargeSeconds * Settings.SupercapacitorsRechargeMultiplier:0.#} s instead of {Settings.RechargeSeconds:0.#} s."))
                .WithUnlock(new TraitUnlock(true) { UnlockCost = 0, CharacterCreationCost = 3, IsAvailable = false, IsAvailableInCC = false, SpecialAbilities = new List<string> { "Railshot" } });

            RogueLibs.CreateCustomTrait<RailgunBreachingSlug>()
                .WithName(new CustomNameInfo("Breaching Slug"))
                .WithDescription(new CustomNameInfo("Railshot also cuts through steel walls."))
                .WithUnlock(new TraitUnlock(true) { UnlockCost = 0, CharacterCreationCost = 5, IsAvailable = false, IsAvailableInCC = false, SpecialAbilities = new List<string> { "Railshot" } });

            RogueLibs.CreateCustomAbility<Railshot>()
                .WithName(new CustomNameInfo("Railshot"))
                .WithDescription(new CustomNameInfo($"Hold to charge ({Settings.ChargeSeconds:0.#} s), keep holding to aim, let go to fire an instant beam that kills everything in a straight line and cuts through walls. Steel walls survive. Fires by itself after {Settings.ArmedMaxSeconds:0.#} s charged. {Settings.RechargeSeconds:0.#} s recharge."))
                .WithSprite(Plugin.LoadResource("railshot.png"))
                .WithUnlock(new AbilityUnlock(true) { UnlockCost = 0, CharacterCreationCost = 0, IsAvailableInCC = true });

            RogueLibs.CreateCustomItem<RailgunItem>()
                .WithName(new CustomNameInfo("Railgun"))
                .WithDescription(new CustomNameInfo("Why shoot one guy when you can shoot five? Just get them to stand in a nice, orderly line. They won't stay in it long."))
                .WithSprite(Plugin.LoadResource("railgun_held.png"))
                .WithUnlock(new ItemUnlock(true)
                {
                    UnlockCost = 0,
                    CharacterCreationCost = 0,
                    IsAvailable = false,
                    IsAvailableInCC = true,
                    IsAvailableInItemTeleporter = false,
                });

            HeldRailgun.Register();

            RogueLibs.CreateCustomBigQuest<RailgunnerLineEmUp>()
                .WithName(new CustomNameInfo("Line 'Em Up"))
                .WithDescription(new CustomNameInfo($"On every level, land {Settings.LineEmUpShots} railshots that each kill at least {Settings.LineEmUpKills} people."))
                .WithUnlock();

            try
            {
                RailgunnerPatches.Apply(harmony);
            }
            catch (System.Exception e)
            {
                Plugin.Log.LogError("[Railgunner] patches failed: " + e);
            }
        }

        private static void LoadCustomSounds()
        {
            string folder;
            try
            {
                folder = Path.Combine(Paths.PluginPath, "SoRCharacters", "sounds");
                Directory.CreateDirectory(folder);
                var readme = Path.Combine(folder, "README.txt");
                if (!File.Exists(readme))
                {
                    File.WriteAllText(readme, "Put a sound file named railgun_charge (.wav, .ogg or .mp3) here to replace the Railgunner's charge-up sound, or railgun_fire to replace the main fire sound. Restart the game after changing files.");
                }
            }
            catch (System.Exception e)
            {
                Plugin.Log.LogWarning("[Railgunner] could not prepare the sounds folder, using built-in sounds: " + e.Message);
                return;
            }

            _customChargeLoaded = TryLoadSound(folder, "railgun_charge", CustomChargeName);
            _customFireLoaded = TryLoadSound(folder, "railgun_fire", CustomFireName);
        }

        private static void LoadReadyTone()
        {
            try
            {
                if (ModAudio.TryLoadWav(Plugin.LoadResource("ready_tone.wav"), ReadyToneName, out var clip) && clip != null)
                {
                    ModAudio.Register(clip);
                    _readyToneLoaded = true;
                    return;
                }

                Plugin.Log.LogWarning("[Railgunner] could not load the ready tone, it is disabled: " + ModAudio.LastError);
            }
            catch (System.Exception e)
            {
                Plugin.Log.LogWarning("[Railgunner] could not load the ready tone, it is disabled: " + e.Message);
            }

            _readyToneLoaded = false;
        }

        private static bool TryLoadSound(string folder, string baseName, string clipName)
        {
            string? path = null;
            try
            {
                foreach (var ext in SoundExtensions)
                {
                    var candidate = Path.Combine(folder, baseName + ext);
                    if (!File.Exists(candidate)) continue;
                    path = candidate;
                    break;
                }

                if (path == null)
                {
                    Plugin.Log.LogInfo($"[Railgunner] no {baseName} file in the sounds folder, using the built-in sound");
                    return false;
                }

                if (!ModAudio.TryLoadFile(path, clipName, out var clip) || clip == null)
                {
                    Plugin.Log.LogWarning($"[Railgunner] could not load {Path.GetFileName(path)}, using the built-in sound: {ModAudio.LastError}");
                    return false;
                }

                ModAudio.Register(clip);
                Plugin.Log.LogInfo($"[Railgunner] loaded custom sound {Path.GetFileName(path)} as {clipName}");
                return true;
            }
            catch (System.Exception e)
            {
                Plugin.Log.LogWarning($"[Railgunner] could not load {(path == null ? baseName : Path.GetFileName(path))}, using the built-in sound: {e.Message}");
                return false;
            }
        }

        public static void Tick()
        {
            ModAudio.EnsureRegistered();
            var gc = GameController.gameController;
            if (gc == null || !gc.loadComplete || gc.mainGUI.menuGUI.titleScreen)
            {
                RailshotState.ClearAll();
                RailshotAlerts.Clear();
                AimLine.DestroyAll();
                RailshotEffects.Clear();
                return;
            }

            if (RailshotEffects.Any) RailshotFire.Guard("effects tick", RailshotEffects.Tick);

            var hostRunning = !gc.levelTransitioning && GameCompat.IsHost && !GameCompat.IsOnline;
            if (hostRunning && RailshotAlerts.Any) RailshotAlerts.Tick(Time.deltaTime);

            if (!RailshotState.Any)
            {
                RailshotState.PruneEnded();
                if (AimLine.Any) AimLine.DestroyAll();
                return;
            }

            if (!hostRunning)
            {
                return;
            }

            var holdInputRunning = Time.timeScale > 0f && !gc.mainGUI.menuGUI.onMenu;
            foreach (var pair in RailshotState.Snapshot())
            {
                var agent = pair.Value.Agent;
                if (agent == null || agent.dead || agent.KnockedOut())
                {
                    RailshotState.End(pair.Key);
                    AimLine.Hide(pair.Key);
                    Plugin.Log.LogInfo("[Railgunner] charging ended: shooter dead or knocked out");
                }
                else if (!holdInputRunning)
                {
                    pair.Value.LastHeldFrame = Time.frameCount;
                }
                else if (Time.frameCount - pair.Value.LastHeldFrame >= 2)
                {
                    RailshotState.End(pair.Key);
                    AimLine.Hide(pair.Key);
                    Plugin.Log.LogInfo("[Railgunner] charge cancelled (hold input stopped)");
                }
                else
                {
                    TurnAim(agent, pair.Value);
                    if (AutoFireDue(gc, agent, pair.Value)) continue;
                    Hum(gc, agent, pair.Value);
                    ScanWitnesses(gc, agent, pair.Value);
                }
            }

            AimLine.UpdateAll();
            RailshotState.PruneEnded();
        }

        // While armed the aim follows the mouse or stick at a limited turn rate; the aim line and the shot use it.
        private static void TurnAim(Agent agent, ChargeEntry entry)
        {
            if (!entry.Armed) return;
            RailshotFire.Guard("aim turn", () =>
            {
                var current = entry.AimDirection;
                var target = Aim.Direction(agent);
                var turned = AimRules.RotateToward(new Vec2(current.x, current.y), new Vec2(target.x, target.y),
                    Settings.AimTurnDegreesPerSecond * Time.deltaTime);
                entry.AimDirection = new Vector2(turned.X, turned.Y);
            });
        }

        // Pops "3", "2", "1" over him, one per second armed, then fires as a release would (Railshot.AutoFire).
        private static bool AutoFireDue(GameController gc, Agent agent, ChargeEntry entry)
        {
            if (!entry.Armed) return false;

            var armed = entry.ArmedFor;
            if (!RailshotRules.ShouldAutoFire(armed, Settings))
            {
                var left = RailshotRules.AutoFireCountdown(armed, Settings);
                if (left == entry.CountdownShown) return false;
                entry.CountdownShown = left;
                // "READY" is on screen at the start of the armed window, so the first number is skipped (READY, 2, 1).
                if (armed < 0.5f) return false;
                RailshotFire.Guard("auto-fire countdown", () => gc.spawnerMain.SpawnStatusText(agent, "Countdown", left.ToString()));
                return false;
            }

            RailshotFire.Guard("auto-fire", () =>
            {
                var railshot = agent.GetAbility<Railshot>();
                if (railshot != null)
                {
                    railshot.AutoFire(agent);
                    return;
                }

                RailshotState.End(agent);
                AimLine.Hide(agent);
                Plugin.Log.LogInfo("[Railgunner] auto-fire skipped: Railshot is no longer his ability");
            });
            if (RailshotState.IsCharging(agent))
            {
                RailshotState.End(agent);
                AimLine.Hide(agent);
            }
            return true;
        }

        // The "Attract" noise makes NPCs walk over (GoalNoiseReact.cs:99-108 reads the source's position); only the
        // volumes 0.8, 1 and 2 map to radii (Noise.cs).
        private static void Hum(GameController gc, Agent agent, ChargeEntry entry)
        {
            entry.HumCountdown -= Time.deltaTime;
            if (entry.HumCountdown > 0f) return;
            entry.HumCountdown = Settings.HumIntervalSeconds;

            RailshotFire.Guard("charging hum", () =>
            {
                var charged = System.Math.Max(0f, entry.LastHeldTime - entry.StartHeld);
                var noise = gc.spawnerMain.SpawnNoise(agent.tr.position, RailshotRules.HumVolume(charged, Settings), agent, "Attract");
                if (noise != null) noise.distraction = true;
            });

            if (entry.HumLogged) return;
            entry.HumLogged = true;
            Plugin.Log.LogInfo("[Railgunner] charging hum started");
        }

        // Every NPC who sees the shooter charging is alerted once per charge and turns hostile AlertSeconds later
        // (RailshotAlerts). His followers and NPCs already hostile to him are left alone.
        private static void ScanWitnesses(GameController gc, Agent shooter, ChargeEntry entry)
        {
            entry.SeenCountdown -= Time.deltaTime;
            if (entry.SeenCountdown > 0f) return;
            entry.SeenCountdown = SeenScanIntervalSeconds;

            foreach (var npc in gc.agentList)
            {
                if (npc == null || npc == shooter || npc.isPlayer != 0 || npc.dead || npc.ghost) continue;
                if (entry.SeenBy.Contains(npc.agentID)) continue;

                RailshotFire.Guard("seen charging scan", () =>
                {
                    if (!Visibility.CanSee(npc, shooter)) return;

                    var code = npc.relationships.GetRelCode(shooter);
                    if (code == relStatus.Aligned || code == relStatus.Loyal || code == relStatus.Submissive || code == relStatus.Hostile) return;

                    entry.SeenBy.Add(npc.agentID);
                    if (RailshotAlerts.IsPending(npc, shooter)) return;
                    RailshotAlerts.Alert(gc, npc, shooter);
                });
            }
        }
    }
}
