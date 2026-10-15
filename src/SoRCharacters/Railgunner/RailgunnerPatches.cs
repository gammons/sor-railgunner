using System;
using System.Reflection;
using HarmonyLib;

namespace SoRCharacters.Railgunner
{
    internal static class RailgunnerPatches
    {
        private static bool _speedFailed;
        private static bool _blockFailed;
        private static bool _damageFailed;
        private static bool _heldFailed;

        public static void Apply(Harmony harmony)
        {
            PatchOne(harmony, "speed", AccessTools.DeclaredMethod(typeof(Agent), "GetCurSpeed", new[] { typeof(bool) }),
                null, new HarmonyMethod(typeof(RailgunnerPatches), nameof(GetCurSpeedPostfix)));

            PatchOne(harmony, "attack block", AccessTools.DeclaredMethod(typeof(Gun), "GunUpdate", Type.EmptyTypes),
                new HarmonyMethod(typeof(RailgunnerPatches), nameof(GunUpdatePrefix)), null);

            // AimGun runs in Gun.GunLateUpdate after the game's animators and Melee.MeleeLateUpdate (Gun.cs:2017,
            // Updater.cs:513-514), right before the gun's depth is set from its angle (Gun.cs:2018-2027).
            PatchOne(harmony, "held railgun aim", AccessTools.DeclaredMethod(typeof(Gun), "AimGun", Type.EmptyTypes),
                new HarmonyMethod(typeof(RailgunnerPatches), nameof(AimGunPrefix)), null);

            PatchOne(harmony, "damage", AccessTools.DeclaredMethod(typeof(StatusEffects), "ChangeHealth",
                    new[] { typeof(float), typeof(PlayfieldObject), typeof(uint), typeof(float), typeof(string), typeof(byte) }),
                new HarmonyMethod(typeof(RailgunnerPatches), nameof(ChangeHealthPrefix)), null);

            PatchOne(harmony, "level reset", AccessTools.DeclaredMethod(typeof(LoadLevel), "SetupMore5", Type.EmptyTypes),
                null, new HarmonyMethod(typeof(RailgunnerPatches), nameof(SetupMore5Postfix)));
        }

        private static void PatchOne(Harmony harmony, string what, MethodBase? method, HarmonyMethod? prefix, HarmonyMethod? postfix)
        {
            try
            {
                if (method == null)
                {
                    Plugin.Log.LogError($"[Railgunner] {what} patch target not found; that effect is disabled");
                    return;
                }
                harmony.Patch(method, prefix: prefix, postfix: postfix);
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"[Railgunner] {what} patch failed: {e}");
            }
        }

        private static void GetCurSpeedPostfix(Agent __instance, ref float __result)
        {
            if (!RailshotState.Any) return;
            try
            {
                if (RailshotState.IsCharging(__instance))
                {
                    var settings = RailgunnerModule.Settings;
                    __result *= RailshotState.IsArmed(__instance) ? settings.ArmedSpeedMultiplier : settings.ChargedSpeedMultiplier;
                }
            }
            catch (Exception e)
            {
                if (!_speedFailed)
                {
                    _speedFailed = true;
                    Plugin.Log.LogError($"[Railgunner] speed effect failed: {e}");
                }
            }
        }

        private static void GunUpdatePrefix(Gun __instance)
        {
            try
            {
                if (RailshotState.BlocksAttack(__instance.agent))
                {
                    __instance.dontAttackOneFrame = true;
                }
            }
            catch (Exception e)
            {
                if (!_blockFailed)
                {
                    _blockFailed = true;
                    Plugin.Log.LogError($"[Railgunner] attack block failed: {e}");
                }
            }

            if (!HeldRailgun.Any) return;
            try
            {
                HeldRailgun.KeepAlive(__instance);
            }
            catch (Exception e)
            {
                if (!_heldFailed)
                {
                    _heldFailed = true;
                    Plugin.Log.LogError($"[Railgunner] held railgun keep-visible failed: {e}");
                }
            }
        }

        private static bool AimGunPrefix(Gun __instance)
        {
            if (!HeldRailgun.Any) return true;
            return !HeldRailgun.Aim(__instance);
        }

        private static void ChangeHealthPrefix(StatusEffects __instance, ref float healthNum)
        {
            if (!RailshotState.Any) return;
            if (healthNum >= 0f || healthNum <= -200f) return;
            try
            {
                var agent = __instance.agent;
                if (agent == null || !RailshotState.IsCharging(agent)) return;
                if (__instance.GotNonHitDamage()) return;
                healthNum *= RailgunnerModule.Settings.ChargedDamageTakenMultiplier;
            }
            catch (Exception e)
            {
                if (!_damageFailed)
                {
                    _damageFailed = true;
                    Plugin.Log.LogError($"[Railgunner] damage effect failed: {e}");
                }
            }
        }

        private static void SetupMore5Postfix()
        {
            try
            {
                RailshotState.ClearAll();
                RailshotAlerts.Clear();
                AimLine.DestroyAll();
                RailshotEffects.Clear();
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"[Railgunner] level reset failed: {e}");
            }
        }
    }
}
