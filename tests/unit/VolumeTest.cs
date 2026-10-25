using GdUnit4;
using static GdUnit4.Assertions;
namespace Hoshikawa.Tests;

[TestSuite]
public class VolumeTest
{
    // Full is 0 dB, each step down is quieter, and the bottom step is silent.
    [TestCase]
    public void EachStepIsQuieterThanTheLastAndTheBottomIsSilent()
    {
        AssertThat(Volume.Db(Volume.Steps)).IsEqual(0f);
        AssertThat(Volume.Db(0)).IsEqual(-80f);
        for(int level=1;level<=Volume.Steps;level++)AssertThat(Volume.Db(level)).IsGreater(Volume.Db(level-1));
    }
    [TestCase]
    public void BothVolumesStartFull()
    {
        AssertThat(Volume.Buses).IsEqual(new[]{"Voice","Music"});
        AssertThat(Volume.Buses.Select(Volume.Level)).IsEqual(new[]{Volume.Steps,Volume.Steps});
    }
    // The bus names are also the sliders' labels, drawn through T().
    [TestCase("ja")]
    [TestCase("zh_TW")]
    public void TheSettingsScreensWordsAreTranslated(string lang)
    {
        var ui=Localization.ParseStrings(File.ReadAllText(Repo.Path($"assets/dialogue/ui.{lang}.json")));
        AssertThat(Volume.Buses.Append("Settings").Where(s=>!ui.ContainsKey(s))).IsEmpty();
    }
}
