using SoRCharacters.Core.Railgunner;

namespace SoRCharacters.Core.Tests.Railgunner;

public class LineEmUpStateTests
{
    [Fact]
    public void Constructor_StoresRule()
    {
        var l = new LineEmUpState(3, 4);
        Assert.Equal(3, l.ShotsNeeded);
        Assert.Equal(4, l.KillsPerShot);
        Assert.Equal(0, l.QualifyingShots);
        Assert.False(l.Completed);
    }

    [Fact]
    public void RecordShot_BelowKillsPerShot_DoesNotCount()
    {
        var l = new LineEmUpState(3, 3);
        l.RecordShot(2);
        l.RecordShot(0);
        Assert.Equal(0, l.QualifyingShots);
        Assert.False(l.Completed);
    }

    [Fact]
    public void RecordShot_ThreeQualifyingShots_Completes()
    {
        var l = new LineEmUpState(3, 3);
        l.RecordShot(3);
        l.RecordShot(4);
        Assert.False(l.Completed);
        l.RecordShot(3);
        Assert.Equal(3, l.QualifyingShots);
        Assert.True(l.Completed);
    }

    [Fact]
    public void RecordShot_BigShot_CountsOnce()
    {
        var l = new LineEmUpState(3, 3);
        l.RecordShot(5);
        Assert.Equal(1, l.QualifyingShots);
    }

    [Fact]
    public void ProgressText_BeforeCompletion()
    {
        var l = new LineEmUpState(3, 3);
        Assert.Equal("Railshots with 3+ kills: 0/3", l.ProgressText);
        l.RecordShot(3);
        l.RecordShot(2);
        Assert.Equal("Railshots with 3+ kills: 1/3", l.ProgressText);
    }

    [Fact]
    public void ProgressText_AfterCompletion()
    {
        var l = new LineEmUpState(3, 3);
        l.RecordShot(3);
        l.RecordShot(3);
        l.RecordShot(3);
        Assert.Equal("Lined them up 3 times!", l.ProgressText);
    }

    [Fact]
    public void RecordShot_PastCompletion_KeepsCounting()
    {
        var l = new LineEmUpState(3, 3);
        for (var i = 0; i < 4; i++) l.RecordShot(3);
        Assert.Equal(4, l.QualifyingShots);
        Assert.True(l.Completed);
        Assert.Equal("Lined them up 4 times!", l.ProgressText);
    }
}
