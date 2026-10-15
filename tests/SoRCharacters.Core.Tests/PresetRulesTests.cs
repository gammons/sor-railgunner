using SoRCharacters.Core.Presets;

namespace SoRCharacters.Core.Tests;

public class PresetRulesTests
{
    private static PresetDefinition ValidPreset() => new PresetDefinition
    {
        Name = "SoR Smoke Test",
        Description = "Temporary preset from the SoRCharacters smoke test.",
        BodyType = "Assassin",
        BodyColorName = "White",
        HairType = "Afro",
        HairColorName = "Brown",
        FacialHair = "None",
        SkinColorName = "SuperPaleSkin",
        LegsColorName = "",
        EyesType = "Eyes",
        EyesColorName = "",
        StartingHeadPiece = "",
        Endurance = 2,
        Strength = 2,
        Accuracy = 2,
        Speed = 2,
        SpecialAbility = "SmokeTestAbility",
        BigQuest = "",
        Traits = new List<string> { "SmokeTestKit" },
        Items = new List<string>(),
    };

    [Fact] public void Decide_NoFolder_NotWritten_Writes() => Assert.Equal(PresetWriteDecision.Write, PresetRules.Decide(false, false, false));
    [Fact] public void Decide_NoFolder_PreviouslyWritten_Skips() => Assert.Equal(PresetWriteDecision.Skip, PresetRules.Decide(false, true, false));
    [Fact] public void Decide_FolderExists_NotWritten_Skips() => Assert.Equal(PresetWriteDecision.Skip, PresetRules.Decide(true, false, false));
    [Fact] public void Decide_FolderExists_PreviouslyWritten_Skips() => Assert.Equal(PresetWriteDecision.Skip, PresetRules.Decide(true, true, false));
    [Fact] public void Decide_Reset_NoFolder_PreviouslyWritten_Writes() => Assert.Equal(PresetWriteDecision.Write, PresetRules.Decide(false, true, true));
    [Fact] public void Decide_Reset_FolderExists_Writes() => Assert.Equal(PresetWriteDecision.Write, PresetRules.Decide(true, true, true));

    [Fact] public void ParseWritten_Blank_IsEmpty()
    {
        Assert.Empty(PresetRules.ParseWritten(""));
        Assert.Empty(PresetRules.ParseWritten("  "));
    }

    [Fact] public void ParseWritten_SplitsOnPipe_TrimsAndDropsEmpties()
    {
        var set = PresetRules.ParseWritten("Repo Man| Railgunner ||");
        Assert.Equal(2, set.Count);
        Assert.Contains("Repo Man", set);
        Assert.Contains("Railgunner", set);
    }

    [Fact] public void ParseWritten_IsCaseSensitive()
    {
        var set = PresetRules.ParseWritten("Repo Man");
        Assert.DoesNotContain("repo man", set);
    }

    [Fact] public void FormatWritten_SortsOrdinalAndDeduplicates()
    {
        Assert.Equal("Railgunner|Repo Man|apple", PresetRules.FormatWritten(new[] { "apple", "Railgunner", "Repo Man", "Railgunner" }));
        Assert.Equal("", PresetRules.FormatWritten(new string[0]));
    }

    [Fact] public void FormatWritten_RoundTripsThroughParse()
    {
        var names = new[] { "Railgunner", "Repo Man" };
        Assert.Equal(names, PresetRules.ParseWritten(PresetRules.FormatWritten(names)).OrderBy(n => n, StringComparer.Ordinal));
    }

    [Fact] public void Paths_UseCloudDataCharacters()
    {
        Assert.Equal(Path.Combine("/root", "CloudData", "Characters", "Repo Man"), PresetRules.FolderFor("/root", "Repo Man"));
        Assert.Equal(Path.Combine("/root", "CloudData", "Characters", "Repo Man", "Repo Man.dat"), PresetRules.FileFor("/root", "Repo Man"));
    }

    [Fact] public void Validate_AcceptsValidPreset() => Assert.Empty(PresetRules.Validate(ValidPreset()));

    [Theory]
    [InlineData("")] [InlineData("   ")] [InlineData("a/b")] [InlineData("a\\b")] [InlineData("a:b")] [InlineData("..")] [InlineData("a|b")]
    public void Validate_RejectsPathCharactersAndBlank(string name)
    {
        var p = ValidPreset(); p.Name = name;
        Assert.NotEmpty(PresetRules.Validate(p));
    }

    [Theory] [InlineData(-1)] [InlineData(4)]
    public void Validate_RejectsStatsOutside0To3(int value)
    {
        var p = ValidPreset(); p.Speed = value;
        Assert.NotEmpty(PresetRules.Validate(p));
        p = ValidPreset(); p.Strength = value;
        Assert.NotEmpty(PresetRules.Validate(p));
        p = ValidPreset(); p.Endurance = value;
        Assert.NotEmpty(PresetRules.Validate(p));
        p = ValidPreset(); p.Accuracy = value;
        Assert.NotEmpty(PresetRules.Validate(p));
    }

    [Theory] [InlineData(0)] [InlineData(3)]
    public void Validate_AcceptsStatBounds(int value)
    {
        var p = ValidPreset();
        p.Strength = p.Endurance = p.Accuracy = p.Speed = value;
        Assert.Empty(PresetRules.Validate(p));
    }

    [Fact] public void Validate_RejectsBlankSpecialAbility()
    {
        var p = ValidPreset(); p.SpecialAbility = "";
        Assert.NotEmpty(PresetRules.Validate(p));
    }
}
