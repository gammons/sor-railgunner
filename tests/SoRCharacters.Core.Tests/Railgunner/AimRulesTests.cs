using SoRCharacters.Core.Railgunner;

namespace SoRCharacters.Core.Tests.Railgunner;

public class AimRulesTests
{
    private const float Eps = 1e-4f;

    private static float AngleOf(Vec2 v) => (float)(Math.Atan2(v.Y, v.X) * 180.0 / Math.PI);

    private static Vec2 Dir(float degrees)
    {
        var r = degrees * Math.PI / 180.0;
        return new Vec2((float)Math.Cos(r), (float)Math.Sin(r));
    }

    private static void AssertUnit(Vec2 v) => Assert.InRange(Math.Sqrt(v.X * v.X + v.Y * v.Y), 1 - Eps, 1 + Eps);

    [Fact]
    public void RotateToward_WithinLimit_ReachesTargetExactly()
    {
        var r = AimRules.RotateToward(Dir(0), Dir(30), 45f);
        Assert.Equal(30f, AngleOf(r), 3);
        AssertUnit(r);
    }

    [Fact]
    public void RotateToward_BeyondLimit_TurnsByLimit()
    {
        var r = AimRules.RotateToward(Dir(0), Dir(90), 10f);
        Assert.Equal(10f, AngleOf(r), 3);
        AssertUnit(r);
    }

    [Fact]
    public void RotateToward_TurnsClockwiseWhenShorter()
    {
        var r = AimRules.RotateToward(Dir(0), Dir(-60), 20f);
        Assert.Equal(-20f, AngleOf(r), 3);
    }

    [Fact]
    public void RotateToward_TakesShortestWayAcrossTheWrap()
    {
        var r = AimRules.RotateToward(Dir(170), Dir(-170), 5f);
        Assert.Equal(175f, AngleOf(r), 3);
    }

    [Fact]
    public void RotateToward_NoOvershootAcrossTheWrap()
    {
        var r = AimRules.RotateToward(Dir(170), Dir(-170), 50f);
        Assert.Equal(-170f, AngleOf(r), 3);
    }

    [Fact]
    public void RotateToward_OppositeDirections_TurnsByLimit()
    {
        var r = AimRules.RotateToward(Dir(0), Dir(180), 30f);
        Assert.Equal(30f, Math.Abs(AngleOf(r)), 3);
        AssertUnit(r);
    }

    [Fact]
    public void RotateToward_NormalizesInputs()
    {
        var r = AimRules.RotateToward(new Vec2(5f, 0f), new Vec2(0f, 3f), 90f);
        Assert.Equal(0f, r.X, 4);
        Assert.Equal(1f, r.Y, 4);
    }

    [Fact]
    public void RotateToward_ZeroLimit_KeepsCurrent()
    {
        var r = AimRules.RotateToward(new Vec2(2f, 0f), Dir(90), 0f);
        Assert.Equal(1f, r.X, 4);
        Assert.Equal(0f, r.Y, 4);
    }

    [Fact]
    public void RotateToward_ZeroTarget_KeepsCurrent()
    {
        var r = AimRules.RotateToward(Dir(40), new Vec2(0f, 0f), 10f);
        Assert.Equal(40f, AngleOf(r), 3);
    }

    [Fact]
    public void RotateToward_ZeroCurrent_SnapsToTarget()
    {
        var r = AimRules.RotateToward(new Vec2(0f, 0f), new Vec2(0f, -2f), 10f);
        Assert.Equal(-90f, AngleOf(r), 3);
        AssertUnit(r);
    }

    [Fact]
    public void RotateToward_RepeatedSteps_Converge()
    {
        var v = Dir(0);
        for (var i = 0; i < 20; i++) v = AimRules.RotateToward(v, Dir(135), 10f);
        Assert.Equal(135f, AngleOf(v), 3);
    }
}
