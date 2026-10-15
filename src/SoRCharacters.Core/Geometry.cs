using System;

namespace SoRCharacters.Core
{
    public readonly struct Vec2
    {
        public float X { get; }
        public float Y { get; }

        public Vec2(float x, float y)
        {
            X = x;
            Y = y;
        }

        public static float Distance(Vec2 a, Vec2 b)
        {
            var dx = a.X - b.X;
            var dy = a.Y - b.Y;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }
    }

    public static class Tiles
    {
        public const float Unit = 0.64f;

        public static float ToUnits(float tiles)
        {
            return tiles * Unit;
        }
    }
}
