using System.Collections.Generic;
using UnityEngine;

namespace SoRCharacters.Railgunner
{
    internal sealed class ChargeEntry
    {
        public ChargeEntry(Agent agent, float startHeld, int frame)
        {
            Agent = agent;
            StartHeld = startHeld;
            LastHeldTime = startHeld;
            LastHeldFrame = frame;
        }

        public Agent Agent { get; }
        public float StartHeld { get; }
        public int LastHeldFrame { get; set; }
        public float LastHeldTime { get; set; }
        public float HumCountdown { get; set; }
        public bool HumLogged { get; set; }
        public bool ReadyToneStarted { get; set; }
        public float SeenCountdown { get; set; }
        public HashSet<int> SeenBy { get; } = new HashSet<int>();
        public bool Armed { get; private set; }
        public float ArmedHeldTime { get; private set; }
        public Vector2 AimDirection { get; set; }
        public int CountdownShown { get; set; }

        public float ArmedFor => Armed ? System.Math.Max(0f, LastHeldTime - ArmedHeldTime) : 0f;

        public void Arm(float heldTime, Vector2 direction)
        {
            Armed = true;
            ArmedHeldTime = heldTime;
            AimDirection = direction;
        }
    }

    internal static class RailshotState
    {
        private static readonly Dictionary<int, ChargeEntry> Charging = new Dictionary<int, ChargeEntry>();
        private static readonly Dictionary<int, int> EndedFrame = new Dictionary<int, int>();

        public static bool Any => Charging.Count != 0;

        public static bool IsCharging(Agent a)
        {
            return Charging.Count != 0 && Charging.ContainsKey(a.agentID);
        }

        public static void Begin(Agent a, float heldAtStart)
        {
            if (Charging.TryGetValue(a.agentID, out var previous)) StopReadyTone(previous);
            Charging[a.agentID] = new ChargeEntry(a, heldAtStart, Time.frameCount);
            EndedFrame.Remove(a.agentID);
            PlayChargeSound(a);
            HeldRailgun.Show(a);
        }

        // AudioHandler.Stop only matches sound helpers for this clip name on this object, so it is a no-op when
        // nothing is playing; vanilla calls it the same way (StatusEffects.cs:14200).
        private static void PlayChargeSound(Agent a)
        {
            RailshotFire.Guard("charge sound", () => GameController.gameController.audioHandler.Play(a, RailgunnerModule.ActiveChargeSound));
        }

        private static void StopChargeSound(Agent a)
        {
            if (a == null) return;
            var settings = RailgunnerModule.Settings;
            var charge = RailgunnerModule.ActiveChargeSound;
            if (charge == RailgunnerModule.ActiveFireSound || charge == settings.FireSound2) return;
            RailshotFire.Guard("charge sound stop", () => GameController.gameController.audioHandler.Stop(a, charge));
        }

        // AudioHandler.Play picks looping from a switch on the clip name (AudioHandler.cs:1624-), so a custom clip never
        // loops by itself. SoundHelper.UpdateSoundHelper recycles a helper once clipLength runs out unless
        // SoundHelper.looping is set (SoundHelper.cs:291-309), so both that and AudioSource.loop are set here.
        public static void StartReadyTone(Agent a)
        {
            if (!RailgunnerModule.ReadyToneLoaded || GameCompat.IsOnline || !GameCompat.IsHost) return;
            if (!Charging.TryGetValue(a.agentID, out var entry) || entry.ReadyToneStarted) return;
            entry.ReadyToneStarted = true;

            RailshotFire.Guard("ready tone", () =>
            {
                var gc = GameController.gameController;
                gc.audioHandler.Stop(a, RailgunnerModule.ReadyToneName);
                gc.audioHandler.Play(a, RailgunnerModule.ReadyToneName);

                var volume = RailgunnerModule.Settings.ReadyToneVolume;
                var found = 0;
                foreach (var helper in gc.soundHelperList)
                {
                    if (helper.clipName != RailgunnerModule.ReadyToneName || helper.playfieldObject != a) continue;
                    found++;
                    helper.looping = true;
                    helper.stopOnDeath = true;
                    helper.audioSource.loop = true;
                    // The game derives volume as (preSettingsVol * SFX volume)^2 and re-applies that whenever the
                    // volume settings change (AudioHandler.cs:1546-1562), so the multiplier goes into preSettingsVol
                    // as a square root; the current volume is scaled directly.
                    helper.preSettingsVol *= Mathf.Sqrt(volume);
                    helper.audioSource.volume *= volume;
                    helper.originalVol *= volume;
                }

                Plugin.Log.LogInfo(found > 0
                    ? "[Railgunner] ready tone started"
                    : "[Railgunner] ready tone did not start (sound off or blocked)");
            });
        }

        public static void StopReadyTone(Agent a)
        {
            if (Charging.TryGetValue(a.agentID, out var entry)) StopReadyTone(entry);
        }

        // AudioHandler.Stop matches the helper by clip name and playfield object with Unity's ==, which also matches an
        // object that was destroyed (same instance), so a destroyed agent is still found. StopAllSounds on level
        // change (Fader.cs:104, LoadLevel.cs:13266) is the backstop.
        private static void StopReadyTone(ChargeEntry entry)
        {
            if (!entry.ReadyToneStarted) return;
            entry.ReadyToneStarted = false;
            RailshotFire.Guard("ready tone stop", () =>
            {
                var audio = GameController.gameController.audioHandler;
                audio.Stop(entry.Agent, RailgunnerModule.ReadyToneName);
                if (entry.Agent == null) audio.Stop(null, RailgunnerModule.ReadyToneName);
            });
        }

        public static void Touch(Agent a, float heldTime)
        {
            if (Charging.Count != 0 && Charging.TryGetValue(a.agentID, out var entry))
            {
                entry.LastHeldFrame = Time.frameCount;
                entry.LastHeldTime = heldTime;
            }
        }

        // Once charged ("READY"): slower walk, and the aim turns from this direction at a limited rate.
        public static void Arm(Agent a, float heldTime, Vector2 direction)
        {
            if (Charging.TryGetValue(a.agentID, out var entry)) entry.Arm(heldTime, direction);
        }

        public static bool IsArmed(Agent a)
        {
            return Charging.Count != 0 && Charging.TryGetValue(a.agentID, out var entry) && entry.Armed;
        }

        // The turn-limited aim while armed; the raw aim otherwise.
        public static Vector2 FireDirection(Agent a)
        {
            return Charging.TryGetValue(a.agentID, out var entry) && entry.Armed ? entry.AimDirection : Aim.Direction(a);
        }

        public static float ChargedFor(Agent a, float heldTime)
        {
            return Charging.TryGetValue(a.agentID, out var entry) ? System.Math.Max(0f, heldTime - entry.StartHeld) : 0f;
        }

        public static void End(Agent a)
        {
            End(a.agentID);
        }

        public static void End(int agentId)
        {
            if (Charging.TryGetValue(agentId, out var entry))
            {
                Charging.Remove(agentId);
                EndedFrame[agentId] = Time.frameCount;
                StopChargeSound(entry.Agent);
                StopReadyTone(entry);
            }
            HeldRailgun.Release(agentId);
        }

        public static void ClearAll()
        {
            foreach (var entry in Charging.Values)
            {
                StopChargeSound(entry.Agent);
                StopReadyTone(entry);
            }
            Charging.Clear();
            EndedFrame.Clear();
            HeldRailgun.ReleaseAll();
        }

        public static List<KeyValuePair<int, ChargeEntry>> Snapshot()
        {
            return new List<KeyValuePair<int, ChargeEntry>>(Charging);
        }

        // Pressing attack mid-charge makes PlayerControl.Update release the ability in frame N, while Gun.GunUpdate
        // reads the attack button from Rewired directly in the same frame N, before or after that release. Treating
        // the agent as still blocked for the frame in which charging ended covers the "after" ordering.
        public static bool BlocksAttack(Agent a)
        {
            if (Charging.Count == 0 && EndedFrame.Count == 0) return false;
            if (Charging.ContainsKey(a.agentID)) return true;
            return EndedFrame.TryGetValue(a.agentID, out var frame) && frame == Time.frameCount;
        }

        public static void PruneEnded()
        {
            if (EndedFrame.Count == 0) return;

            var now = Time.frameCount;
            List<int>? stale = null;
            foreach (var pair in EndedFrame)
            {
                if (pair.Value >= now - 1) continue;
                (stale ??= new List<int>()).Add(pair.Key);
            }

            if (stale == null) return;
            foreach (var id in stale) EndedFrame.Remove(id);
        }
    }
}
