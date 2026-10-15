using System;

namespace SoRCharacters.Core.Railgunner
{
    public static class AimRules
    {
        private const double Tiny = 1e-12;

        // Turns current toward target by at most maxDegrees the shortest way round, without overshooting. Exactly
        // opposite directions turn counter-clockwise. A zero current snaps to the target; a zero target keeps current.
        public static Vec2 RotateToward(Vec2 current, Vec2 target, float maxDegrees)
        {
            var curLen = Math.Sqrt((double)current.X * current.X + (double)current.Y * current.Y);
            var tgtLen = Math.Sqrt((double)target.X * target.X + (double)target.Y * target.Y);
            if (curLen < Tiny && tgtLen < Tiny) return new Vec2(1f, 0f);
            if (curLen < Tiny) return new Vec2((float)(target.X / tgtLen), (float)(target.Y / tgtLen));

            var cx = current.X / curLen;
            var cy = current.Y / curLen;
            if (tgtLen < Tiny || !(maxDegrees > 0f)) return new Vec2((float)cx, (float)cy);

            var tx = target.X / tgtLen;
            var ty = target.Y / tgtLen;
            var delta = Math.Atan2(cx * ty - cy * tx, cx * tx + cy * ty);
            var max = maxDegrees * Math.PI / 180.0;
            if (Math.Abs(delta) <= max) return new Vec2((float)tx, (float)ty);

            var step = delta >= 0 ? max : -max;
            var cos = Math.Cos(step);
            var sin = Math.Sin(step);
            return new Vec2((float)(cx * cos - cy * sin), (float)(cx * sin + cy * cos));
        }
    }
}
