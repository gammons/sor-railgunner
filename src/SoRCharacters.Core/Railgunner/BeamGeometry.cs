using System;
using System.Collections.Generic;

namespace SoRCharacters.Core.Railgunner
{
    public enum TileKind
    {
        Empty,
        Breakable,
        Steel,
        Border,
    }

    public readonly struct TileCoord : IEquatable<TileCoord>
    {
        public int X { get; }
        public int Y { get; }

        public TileCoord(int x, int y)
        {
            X = x;
            Y = y;
        }

        public bool Equals(TileCoord other) => X == other.X && Y == other.Y;

        public override bool Equals(object? obj) => obj is TileCoord other && Equals(other);

        public override int GetHashCode() => unchecked((X * 397) ^ Y);

        public override string ToString() => $"({X},{Y})";
    }

    public sealed class BeamPlan
    {
        public BeamPlan(float stopDistance, IReadOnlyList<TileCoord> tilesToCut, IReadOnlyList<TileCoord> steelPassed)
        {
            StopDistance = stopDistance;
            TilesToCut = tilesToCut;
            SteelPassed = steelPassed;
        }

        public float StopDistance { get; }
        public IReadOnlyList<TileCoord> TilesToCut { get; }
        public IReadOnlyList<TileCoord> SteelPassed { get; }
    }

    public static class BeamGeometry
    {
        private const double Half = Tiles.Unit / 2.0;
        private const double Unit = Tiles.Unit;
        private const double MinHalfWidth = 1e-4;
        private const double RejectMargin = Half * 1.4142135623730951 + 1e-6;

        public static IReadOnlyList<TileCoord> TilesAlong(Vec2 origin, Vec2 dir, float rangeUnits, float halfWidthUnits)
        {
            var entries = Walk(origin, dir, rangeUnits, halfWidthUnits);
            var result = new List<TileCoord>(entries.Count);
            foreach (var entry in entries)
                result.Add(entry.Tile);
            return result;
        }

        public static BeamPlan Plan(Vec2 origin, Vec2 dir, float rangeUnits, float halfWidthUnits, Func<TileCoord, TileKind> tileAt)
        {
            var entries = Walk(origin, dir, rangeUnits, halfWidthUnits);
            var kinds = new TileKind[entries.Count];
            var stopIndex = -1;
            for (var i = 0; i < entries.Count; i++)
            {
                kinds[i] = tileAt(entries[i].Tile);
                if (kinds[i] == TileKind.Border)
                {
                    stopIndex = i;
                    break;
                }
            }

            var cut = new List<TileCoord>();
            var steel = new List<TileCoord>();
            var stop = rangeUnits;
            var limit = double.MaxValue;
            if (stopIndex >= 0)
            {
                stop = (float)entries[stopIndex].Distance;
                limit = entries[stopIndex].Distance - 1e-6;
            }

            var count = stopIndex >= 0 ? stopIndex : entries.Count;
            for (var i = 0; i < count; i++)
            {
                if (entries[i].Distance >= limit) continue;
                if (kinds[i] == TileKind.Breakable) cut.Add(entries[i].Tile);
                else if (kinds[i] == TileKind.Steel) steel.Add(entries[i].Tile);
            }
            return new BeamPlan(stop, cut, steel);
        }

        public static bool CircleHit(Vec2 origin, Vec2 dir, float lengthUnits, float halfWidthUnits, Vec2 center, float radius)
        {
            if (!TryFrame(dir, out var dx, out var dy)) return false;
            var halfWidth = Math.Max(halfWidthUnits, MinHalfWidth);
            var rx = center.X - origin.X;
            var ry = center.Y - origin.Y;
            var t = rx * dx + ry * dy;
            var s = -rx * dy + ry * dx;
            var nt = Math.Min(Math.Max(t, 0.0), lengthUnits);
            var ns = Math.Min(Math.Max(s, -halfWidth), halfWidth);
            var ex = t - nt;
            var es = s - ns;
            return ex * ex + es * es <= (double)radius * radius;
        }

        public static bool BoxHit(Vec2 origin, Vec2 dir, float lengthUnits, float halfWidthUnits, Vec2 min, Vec2 max)
        {
            if (!TryFrame(dir, out var dx, out var dy)) return false;
            var clip = new Clipper();
            clip.SetBox(origin, dx, dy, min.X, min.Y, max.X, max.Y);
            return clip.Run(lengthUnits, Math.Max(halfWidthUnits, MinHalfWidth), out _);
        }

        private readonly struct Entry
        {
            public readonly TileCoord Tile;
            public readonly double Distance;

            public Entry(TileCoord tile, double distance)
            {
                Tile = tile;
                Distance = distance;
            }
        }

        // Convex polygon clipper in beam space (t along the beam, s across it) using two reusable buffers
        // of interleaved (t, s) pairs; a polygon grows by at most one vertex per clip plane (4 + 4 = 8).
        private sealed class Clipper
        {
            private double[] _src = new double[32];
            private double[] _dst = new double[32];
            private int _count;

            public void SetSquare(Vec2 origin, double dx, double dy, double cx, double cy, double half)
            {
                SetBox(origin, dx, dy, cx - half, cy - half, cx + half, cy + half);
            }

            public void SetBox(Vec2 origin, double dx, double dy, double minX, double minY, double maxX, double maxY)
            {
                _count = 0;
                Add(origin, dx, dy, minX, minY);
                Add(origin, dx, dy, maxX, minY);
                Add(origin, dx, dy, maxX, maxY);
                Add(origin, dx, dy, minX, maxY);
            }

            // Clips to t in [0, length], |s| <= halfWidth. Returns false if nothing is left;
            // otherwise the smallest t of what remains.
            public bool Run(double length, double halfWidth, out double minT)
            {
                ClipHalfPlane(0, 1, 0.0);
                ClipHalfPlane(0, -1, length);
                ClipHalfPlane(1, 1, halfWidth);
                ClipHalfPlane(1, -1, halfWidth);

                minT = double.MaxValue;
                for (var i = 0; i < _count; i++)
                    minT = Math.Min(minT, _src[i * 2]);
                return _count > 0;
            }

            private void Add(Vec2 origin, double dx, double dy, double wx, double wy)
            {
                var rx = wx - origin.X;
                var ry = wy - origin.Y;
                _src[_count * 2] = rx * dx + ry * dy;
                _src[_count * 2 + 1] = -rx * dy + ry * dx;
                _count++;
            }

            // Keeps the part of the polygon where sign * coord[axis] + offset >= 0 (axis 0 = t, 1 = s).
            private void ClipHalfPlane(int axis, double sign, double offset)
            {
                if (_count == 0) return;

                var outCount = 0;
                var prevT = _src[(_count - 1) * 2];
                var prevS = _src[(_count - 1) * 2 + 1];
                var prevValue = sign * (axis == 0 ? prevT : prevS) + offset;
                for (var i = 0; i < _count; i++)
                {
                    var curT = _src[i * 2];
                    var curS = _src[i * 2 + 1];
                    var curValue = sign * (axis == 0 ? curT : curS) + offset;
                    if ((curValue >= 0) != (prevValue >= 0))
                    {
                        var f = prevValue / (prevValue - curValue);
                        _dst[outCount * 2] = prevT + (curT - prevT) * f;
                        _dst[outCount * 2 + 1] = prevS + (curS - prevS) * f;
                        outCount++;
                    }
                    if (curValue >= 0)
                    {
                        _dst[outCount * 2] = curT;
                        _dst[outCount * 2 + 1] = curS;
                        outCount++;
                    }
                    prevT = curT;
                    prevS = curS;
                    prevValue = curValue;
                }

                var swap = _src;
                _src = _dst;
                _dst = swap;
                _count = outCount;
            }
        }

        private static bool TryFrame(Vec2 dir, out double dx, out double dy)
        {
            var len = Math.Sqrt((double)dir.X * dir.X + (double)dir.Y * dir.Y);
            if (len < 1e-9)
            {
                dx = 0;
                dy = 0;
                return false;
            }
            dx = dir.X / len;
            dy = dir.Y / len;
            return true;
        }

        private static int TileIndex(double coord)
        {
            return (int)Math.Floor((coord + Half) / Unit);
        }

        private static List<Entry> Walk(Vec2 origin, Vec2 dir, float rangeUnits, float halfWidthUnits)
        {
            var entries = new List<Entry>();
            if (!TryFrame(dir, out var dx, out var dy) || rangeUnits < 0f) return entries;
            var halfWidth = Math.Max(halfWidthUnits, MinHalfWidth);

            double minX = double.MaxValue, minY = double.MaxValue, maxX = double.MinValue, maxY = double.MinValue;
            for (var i = 0; i < 4; i++)
            {
                var t = (i & 1) == 0 ? 0.0 : rangeUnits;
                var s = (i & 2) == 0 ? -halfWidth : halfWidth;
                var px = origin.X + t * dx - s * dy;
                var py = origin.Y + t * dy + s * dx;
                minX = Math.Min(minX, px);
                maxX = Math.Max(maxX, px);
                minY = Math.Min(minY, py);
                maxY = Math.Max(maxY, py);
            }

            var clip = new Clipper();
            var sMax = halfWidth + RejectMargin;
            var tMax = rangeUnits + RejectMargin;
            var x1 = TileIndex(maxX);
            var y1 = TileIndex(maxY);
            for (var tx = TileIndex(minX); tx <= x1; tx++)
            {
                for (var ty = TileIndex(minY); ty <= y1; ty++)
                {
                    var cx = tx * Unit;
                    var cy = ty * Unit;
                    var rx = cx - origin.X;
                    var ry = cy - origin.Y;
                    var tc = rx * dx + ry * dy;
                    var sc = -rx * dy + ry * dx;
                    if (tc < -RejectMargin || tc > tMax || sc > sMax || sc < -sMax) continue;

                    clip.SetSquare(origin, dx, dy, cx, cy, Half);
                    if (clip.Run(rangeUnits, halfWidth, out var d))
                        entries.Add(new Entry(new TileCoord(tx, ty), d));
                }
            }

            entries.Sort((a, b) =>
            {
                var c = a.Distance.CompareTo(b.Distance);
                if (c != 0) return c;
                c = a.Tile.Y.CompareTo(b.Tile.Y);
                return c != 0 ? c : a.Tile.X.CompareTo(b.Tile.X);
            });
            return entries;
        }
    }
}
