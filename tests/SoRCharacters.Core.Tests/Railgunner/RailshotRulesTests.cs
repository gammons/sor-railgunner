using SoRCharacters.Core.Railgunner;

namespace SoRCharacters.Core.Tests.Railgunner;

public class RailshotRulesTests
{
    private static readonly RailgunnerSettings S = new RailgunnerSettings();

    [Theory]
    [InlineData(0.99f, false)]
    [InlineData(1.0f, true)]
    public void IsCharged_AtThreshold(float h, bool e) => Assert.Equal(e, RailshotRules.IsCharged(h, S));

    [Fact]
    public void OnRelease_FiresWhenChargedAndAlive() =>
        Assert.Equal(ReleaseOutcome.Fire, RailshotRules.OnRelease(5f, true, S));

    [Fact]
    public void OnRelease_CancelsWhenNotCharged() =>
        Assert.Equal(ReleaseOutcome.Cancel, RailshotRules.OnRelease(0.5f, true, S));

    [Fact]
    public void OnRelease_CancelsWhenDead() =>
        Assert.Equal(ReleaseOutcome.Cancel, RailshotRules.OnRelease(5f, false, S));

    [Theory]
    [InlineData(0f, 0.8f)]
    [InlineData(0.5f, 0.8f)]
    [InlineData(1.0f, 1f)]
    [InlineData(2.99f, 1f)]
    [InlineData(3.0f, 2f)]
    [InlineData(10f, 2f)]
    public void HumVolume_StepsWithChargeTime(float h, float e) => Assert.Equal(e, RailshotRules.HumVolume(h, S));

    [Fact]
    public void HumVolume_FollowsSettings()
    {
        var s = new RailgunnerSettings { ChargeSeconds = 2f, HumLoudAfterSeconds = 1f };
        Assert.Equal(0.8f, RailshotRules.HumVolume(1.9f, s));
        Assert.Equal(1f, RailshotRules.HumVolume(2.9f, s));
        Assert.Equal(2f, RailshotRules.HumVolume(3f, s));
    }

    [Fact]
    public void RechargeCount_Rounds()
    {
        var s = new RailgunnerSettings { RechargeSeconds = 59.2f };
        Assert.Equal(60, RailshotRules.RechargeCount(s));
    }

    [Fact]
    public void RechargeCount_DefaultIs15() => Assert.Equal(15, RailshotRules.RechargeCount(S, false));

    [Fact]
    public void RechargeCount_SupercapacitorsShortensTo10() => Assert.Equal(10, RailshotRules.RechargeCount(S, true));

    [Fact]
    public void RechargeCount_SupercapacitorsFollowsMultiplier()
    {
        var s = new RailgunnerSettings { SupercapacitorsRechargeMultiplier = 0.5f };
        Assert.Equal(8, RailshotRules.RechargeCount(s, true));
        Assert.Equal(15, RailshotRules.RechargeCount(s, false));
    }

    [Fact]
    public void RechargeCount_SupercapacitorsNeverBelowOne()
    {
        var s = new RailgunnerSettings { RechargeSeconds = 0.5f, SupercapacitorsRechargeMultiplier = 0.1f };
        Assert.Equal(1, RailshotRules.RechargeCount(s, true));
    }

    [Theory]
    [InlineData(0f, false)]
    [InlineData(2.99f, false)]
    [InlineData(3f, true)]
    [InlineData(10f, true)]
    public void ShouldAutoFire_AfterArmedMaxSeconds(float armed, bool e) => Assert.Equal(e, RailshotRules.ShouldAutoFire(armed, S));

    [Fact]
    public void ShouldAutoFire_FollowsSettings()
    {
        var s = new RailgunnerSettings { ArmedMaxSeconds = 1.5f };
        Assert.False(RailshotRules.ShouldAutoFire(1.4f, s));
        Assert.True(RailshotRules.ShouldAutoFire(1.5f, s));
    }

    [Theory]
    [InlineData(0f, 3)]
    [InlineData(0.5f, 3)]
    [InlineData(0.99f, 3)]
    [InlineData(1f, 2)]
    [InlineData(1.99f, 2)]
    [InlineData(2f, 1)]
    [InlineData(2.99f, 1)]
    [InlineData(3f, 0)]
    [InlineData(7f, 0)]
    public void AutoFireCountdown_ShowsWholeSecondsLeft(float armed, int e) => Assert.Equal(e, RailshotRules.AutoFireCountdown(armed, S));

    [Fact]
    public void AutoFireCountdown_FollowsSettings()
    {
        var s = new RailgunnerSettings { ArmedMaxSeconds = 2.5f };
        Assert.Equal(3, RailshotRules.AutoFireCountdown(0f, s));
        Assert.Equal(2, RailshotRules.AutoFireCountdown(0.5f, s));
        Assert.Equal(1, RailshotRules.AutoFireCountdown(1.5f, s));
        Assert.Equal(0, RailshotRules.AutoFireCountdown(2.5f, s));
    }
}
