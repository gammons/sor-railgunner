using SoRCharacters.Core.Config;

namespace SoRCharacters.Core.Tests;

public class ConfigParsingTests
{
    [Fact] public void ParseIntTable_ParsesTrimmedEntries()
    {
        var t = ConfigParsing.ParseIntTable(" Television:1, Computer : 2 ", out var errors);
        Assert.Empty(errors); Assert.Equal(1, t["Television"]); Assert.Equal(2, t["Computer"]); Assert.Equal(2, t.Count);
    }

    [Theory] [InlineData("Television")] [InlineData("Television:x")] [InlineData(":2")] [InlineData("TV:-1")]
    public void ParseIntTable_SkipsMalformedWithError(string entry)
    {
        var t = ConfigParsing.ParseIntTable(entry + ", Computer:2", out var errors);
        Assert.Single(errors); Assert.Equal(2, t["Computer"]); Assert.Single(t);
    }

    [Fact] public void ParseNameList_TrimsAndDropsEmpty()
        => Assert.Equal(new HashSet<string> { "Turret", "SecurityCam" }, ConfigParsing.ParseNameList(" Turret,, SecurityCam ,"));

    [Fact] public void FormatIntTable_RoundTrips()
    {
        var src = new Dictionary<string, int> { ["Television"] = 1, ["Computer"] = 2 };
        Assert.Equal(src, ConfigParsing.ParseIntTable(ConfigParsing.FormatIntTable(src), out _));
    }

    [Fact] public void Tiles_ToUnits() => Assert.Equal(1.92f, Tiles.ToUnits(3), 3);
}
