using SoRCharacters.Core.Railgunner;

namespace SoRCharacters.Core.Tests.Railgunner;

public class BeamGeometryTests
{
    private static readonly Vec2 Origin = new Vec2(0f, 0f);
    private static readonly Vec2 East = new Vec2(1f, 0f);
    private const float Half = 0.16f;

    private static Func<TileCoord, TileKind> TileMap(params (int x, int y, TileKind kind)[] tiles)
    {
        return t =>
        {
            foreach (var (x, y, kind) in tiles)
                if (t.X == x && t.Y == y) return kind;
            return TileKind.Empty;
        };
    }

    [Fact]
    public void TilesAlong_StraightLine_InOrder()
    {
        var tiles = BeamGeometry.TilesAlong(Origin, East, 2.0f, Half);
        Assert.Equal(new[] { new TileCoord(0, 0), new TileCoord(1, 0), new TileCoord(2, 0), new TileCoord(3, 0) }, tiles);
        Assert.DoesNotContain(tiles, t => t.Y != 0);
    }

    [Fact]
    public void TilesAlong_WideBeam_IncludesNeighbourRows()
    {
        var tiles = BeamGeometry.TilesAlong(Origin, East, 1.0f, 0.5f);
        Assert.Contains(new TileCoord(1, 1), tiles);
        Assert.Contains(new TileCoord(1, -1), tiles);
    }

    [Fact]
    public void TilesAlong_Diagonal_NoGaps()
    {
        var tiles = BeamGeometry.TilesAlong(Origin, new Vec2(1f, 1f), 3f, Half);
        Assert.Contains(new TileCoord(2, 2), tiles);
        for (var i = 1; i < tiles.Count; i++)
        {
            Assert.True(Math.Abs(tiles[i].X - tiles[i - 1].X) <= 1 && Math.Abs(tiles[i].Y - tiles[i - 1].Y) <= 1,
                $"gap between {tiles[i - 1]} and {tiles[i]}");
        }
    }

    [Fact]
    public void Plan_CollectsBreakableAndSteel()
    {
        var at = TileMap((2, 0, TileKind.Breakable), (3, 0, TileKind.Steel), (4, 0, TileKind.Breakable));
        var plan = BeamGeometry.Plan(Origin, East, 5f, Half, at);
        Assert.Equal(new[] { new TileCoord(2, 0), new TileCoord(4, 0) }, plan.TilesToCut);
        Assert.Equal(new[] { new TileCoord(3, 0) }, plan.SteelPassed);
        Assert.Equal(5f, plan.StopDistance, 2);
    }

    [Fact]
    public void Plan_StopsAtBorder()
    {
        var at = TileMap((3, 0, TileKind.Border), (4, 0, TileKind.Breakable));
        var plan = BeamGeometry.Plan(Origin, East, 5f, Half, at);
        Assert.Equal(3 * 0.64f - 0.32f, plan.StopDistance, 2);
        Assert.DoesNotContain(new TileCoord(4, 0), plan.TilesToCut);
    }

    [Fact]
    public void Plan_NothingBeyondBorder()
    {
        var at = TileMap((2, 0, TileKind.Border), (5, 0, TileKind.Breakable));
        var plan = BeamGeometry.Plan(Origin, East, 5f, Half, at);
        Assert.Empty(plan.TilesToCut);
    }

    [Fact]
    public void Plan_DiagonalStopDistance()
    {
        // Beam dir (1,1)/sqrt2 from the origin. Tile (3,3) is centred at (1.92,1.92) and spans 1.6..2.24 on both axes.
        // The strip (|s| <= 0.16) first overlaps it at the tile's near corner (1.6,1.6), which lies on the beam axis
        // (s = 0), so the entry distance is the projection of that corner: (1.6 + 1.6) / sqrt2.
        var at = TileMap((3, 3, TileKind.Border));
        var plan = BeamGeometry.Plan(Origin, new Vec2(1f, 1f), 5f, Half, at);
        var expected = (1.6f + 1.6f) / MathF.Sqrt(2f);
        Assert.Equal(expected, plan.StopDistance, 2);
    }

    [Fact]
    public void Plan_BorderTie_ExcludesSameDistanceTiles()
    {
        // With halfWidth 0.5 the beam enters (3,0), (3,1) and (3,-1) all at x = 1.6, so they tie at the same distance.
        var above = TileMap((3, 0, TileKind.Breakable), (3, 1, TileKind.Border));
        var planAbove = BeamGeometry.Plan(Origin, East, 5f, 0.5f, above);
        Assert.DoesNotContain(new TileCoord(3, 0), planAbove.TilesToCut);
        Assert.Equal(1.6f, planAbove.StopDistance, 2);

        var below = TileMap((3, 0, TileKind.Breakable), (3, -1, TileKind.Border));
        var planBelow = BeamGeometry.Plan(Origin, East, 5f, 0.5f, below);
        Assert.DoesNotContain(new TileCoord(3, 0), planBelow.TilesToCut);
        Assert.Equal(1.6f, planBelow.StopDistance, 2);
    }

    [Fact]
    public void TilesAlong_NearAxis_NoGaps()
    {
        var tiles = BeamGeometry.TilesAlong(Origin, new Vec2(1f, 0.01f), 12.8f, Half);
        Assert.Equal(20, tiles[tiles.Count - 1].X);
        for (var i = 1; i < tiles.Count; i++)
        {
            Assert.True(Math.Abs(tiles[i].X - tiles[i - 1].X) <= 1 && Math.Abs(tiles[i].Y - tiles[i - 1].Y) <= 1,
                $"gap between {tiles[i - 1]} and {tiles[i]}");
        }
    }

    [Fact]
    public void TilesAlong_OffCentreOrigin()
    {
        var tiles = BeamGeometry.TilesAlong(new Vec2(0.3f, 0.1f), East, 1.0f, Half);
        Assert.Equal(new TileCoord(0, 0), tiles[0]);
        Assert.Contains(new TileCoord(1, 0), tiles);
        Assert.Contains(new TileCoord(2, 0), tiles);
    }

    [Fact]
    public void TilesAlong_ZeroDirection_Empty()
    {
        Assert.Empty(BeamGeometry.TilesAlong(Origin, new Vec2(0f, 0f), 5f, Half));
    }

    [Fact]
    public void CircleHit_ZeroDirection_False()
    {
        Assert.False(BeamGeometry.CircleHit(Origin, new Vec2(0f, 0f), 5f, Half, new Vec2(1f, 0f), 0.24f));
    }

    [Fact]
    public void CircleHit_EdgeAndBehind()
    {
        Assert.True(BeamGeometry.CircleHit(Origin, East, 5f, Half, new Vec2(3f, 0.3f), 0.24f));
        Assert.False(BeamGeometry.CircleHit(Origin, East, 5f, Half, new Vec2(3f, 0.41f), 0.24f));
        Assert.False(BeamGeometry.CircleHit(Origin, East, 5f, Half, new Vec2(-0.5f, 0f), 0.24f));
        Assert.True(BeamGeometry.CircleHit(Origin, East, 5f, Half, new Vec2(5.2f, 0f), 0.24f));
        Assert.False(BeamGeometry.CircleHit(Origin, East, 5f, Half, new Vec2(5.3f, 0f), 0.24f));
    }

    [Fact]
    public void BoxHit_Overlap()
    {
        Assert.True(BeamGeometry.BoxHit(Origin, East, 5f, Half, new Vec2(2f, -0.1f), new Vec2(2.5f, 0.1f)));
        Assert.False(BeamGeometry.BoxHit(Origin, East, 5f, Half, new Vec2(2f, 0.3f), new Vec2(2.5f, 0.6f)));
        Assert.False(BeamGeometry.BoxHit(Origin, East, 5f, Half, new Vec2(6f, -0.1f), new Vec2(6.5f, 0.1f)));
    }

    [Fact]
    public void BoxHit_BoxEntirelyBehindOriginIsMissed()
    {
        Assert.False(BeamGeometry.BoxHit(Origin, East, 5f, Half, new Vec2(-1.5f, -0.3f), new Vec2(-0.01f, 0.3f)));
        Assert.True(BeamGeometry.BoxHit(Origin, East, 5f, Half, new Vec2(-1.5f, -0.3f), new Vec2(0.5f, 0.3f)));
    }
}
