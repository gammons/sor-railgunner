using SoRCharacters.Core.Railgunner;

namespace SoRCharacters.Core.Tests.Railgunner;

public class RailgunnerSettingsTests
{
    [Fact]
    public void Defaults_MatchSpec()
    {
        var s = new RailgunnerSettings();
        Assert.Equal(1.0f, s.ChargeSeconds);
        Assert.Equal(0.5f, s.ChargedSpeedMultiplier);
        Assert.Equal(1.5f, s.ChargedDamageTakenMultiplier);
        Assert.Equal(15f, s.RechargeSeconds);
        Assert.Equal(0.5f, s.BeamWidthTiles);
        Assert.Equal(20f, s.BeamRangeTiles);
        Assert.Equal(3, s.LineEmUpKills);
        Assert.Equal(3, s.LineEmUpShots);
        Assert.Equal(0.4f, s.ShakeSeconds);
        Assert.Equal(200f, s.ShakeOffset);
        Assert.Equal("ZombieSpitCharge", s.ChargeSound);
        Assert.Equal("MindControlFire", s.FireSound);
        Assert.Equal("LaserGunFire", s.FireSound2);
        Assert.Equal(1.0f, s.HumIntervalSeconds);
        Assert.Equal(2.0f, s.HumLoudAfterSeconds);
        Assert.Equal(0.5f, s.ReadyToneVolume);
        Assert.Equal(0.6667f, s.SupercapacitorsRechargeMultiplier);
        Assert.Equal(2.0f, s.AlertSeconds);
        Assert.Equal(0.15f, s.ArmedSpeedMultiplier);
        Assert.Equal(90f, s.AimTurnDegreesPerSecond);
        Assert.Equal(3f, s.ArmedMaxSeconds);
        Assert.Equal(0.25f, s.BeamFadeSeconds);
        Assert.Equal(0.5f, s.AfterglowSeconds);
        Assert.Equal(12, s.SparksPerTile);
        Assert.Equal(0.12f, s.FlashSeconds);
    }
}
