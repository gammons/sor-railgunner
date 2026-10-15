using System;

namespace SoRCharacters.Core.Railgunner
{
    public enum ReleaseOutcome
    {
        Cancel,
        Fire,
    }

    public static class RailshotRules
    {
        public static bool IsCharged(float heldSeconds, RailgunnerSettings s)
        {
            return heldSeconds >= s.ChargeSeconds;
        }

        public static ReleaseOutcome OnRelease(float heldSeconds, bool shooterAlive, RailgunnerSettings s)
        {
            return shooterAlive && IsCharged(heldSeconds, s) ? ReleaseOutcome.Fire : ReleaseOutcome.Cancel;
        }

        public static bool ShouldAutoFire(float armedSeconds, RailgunnerSettings s)
        {
            return armedSeconds >= s.ArmedMaxSeconds;
        }

        // Whole seconds left before the auto-fire (3, 2, 1 with the default), 0 once it is due.
        public static int AutoFireCountdown(float armedSeconds, RailgunnerSettings s)
        {
            if (ShouldAutoFire(armedSeconds, s)) return 0;
            return Math.Max(1, (int)Math.Ceiling((double)s.ArmedMaxSeconds - Math.Max(0f, armedSeconds)));
        }

        // Only these exact volumes map to attract radii in the game (Noise.cs).
        public static float HumVolume(float chargedForSeconds, RailgunnerSettings s)
        {
            if (chargedForSeconds >= s.ChargeSeconds + s.HumLoudAfterSeconds) return 2f;
            return IsCharged(chargedForSeconds, s) ? 1f : 0.8f;
        }

        // The multiplier is a rounded 2/3 (15 s * 0.6667 = 10.0005), so a small tolerance keeps 10 s from rounding up to 11.
        private const float RoundingTolerance = 0.01f;

        public static int RechargeCount(RailgunnerSettings s)
        {
            return RechargeCount(s, false);
        }

        public static int RechargeCount(RailgunnerSettings s, bool supercapacitors)
        {
            var seconds = supercapacitors ? s.RechargeSeconds * s.SupercapacitorsRechargeMultiplier : s.RechargeSeconds;
            return Math.Max(1, (int)Math.Ceiling(seconds - RoundingTolerance));
        }
    }
}
