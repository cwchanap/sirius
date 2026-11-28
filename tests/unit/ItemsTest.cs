using GdUnit4;
using static GdUnit4.Assertions;
namespace Hoshikawa.Tests;

[TestSuite]
public class ItemsTest
{
    // Consumables are what the items screen's Use button spends; key items only sit in their row.
    [TestCase]
    public void EveryItemIsWellFormed()
    {
        AssertThat(Items.All.Select(i=>i.Id).Distinct().Count()).IsEqual(Items.All.Length);
        AssertThat(Items.All.Where(i=>!File.Exists(Repo.Path($"assets/art/ui/{i.Icon}.png"))).Select(i=>i.Id)).IsEmpty();
        AssertThat(Items.All.Where(i=>(i.Kind==ItemKind.Consumable)!=(i.Use!=null)||i.Max<1).Select(i=>i.Id)).IsEmpty();
        AssertThat(Items.All.Where(i=>i.Raises!=null&&Items.All.All(o=>o.Id!=i.Raises)).Select(i=>i.Id)).IsEmpty();
    }
    // Item names and texts are table data, not literals in a T() call, so the localization test does not see them.
    [TestCase("ja")]
    [TestCase("zh_TW")]
    public void EveryItemIsTranslated(string lang)
    {
        var ui=Localization.ParseStrings(File.ReadAllText(Repo.Path($"assets/dialogue/ui.{lang}.json")));
        AssertThat(Items.All.SelectMany(i=>new[]{i.Name,i.Text}).Where(s=>!ui.ContainsKey(s))).IsEmpty();
    }
    [TestCase]
    public void MioSetsOutWithThreeRemediesAndHerTerminal()
    {
        var bag=new Inventory();
        AssertThat(bag.Held.Select(i=>i.Id)).IsEqual(new[]{"remedy","terminal"});
        AssertThat(bag.Count("remedy")).IsEqual(3);AssertThat(bag.Add("remedy")).IsFalse();
        AssertThat(bag.Count("cell")).IsEqual(0);AssertThat(bag.Take("cell")).IsFalse();
    }
    [TestCase]
    public void EachPouchRaisesTheRemediesSheCarries()
    {
        var bag=new Inventory();
        AssertThat(bag.Count("remedy")).IsEqual(3);AssertThat(bag.Add("remedy")).IsFalse();
        AssertThat(bag.Add("pouch")).IsTrue();AssertThat(bag.Capacity("remedy")).IsEqual(4);AssertThat(bag.Add("remedy")).IsTrue();AssertThat(bag.Count("remedy")).IsEqual(4);
        // The pouch row's own limit is seven (four are placed in the prologue): seven raise three remedies to ten, and an eighth is refused.
        for(int i=0;i<6;i++)AssertThat(bag.Add("pouch")).IsTrue();
        AssertThat(bag.Capacity("remedy")).IsEqual(10);
        AssertThat(bag.Add("pouch")).IsFalse();AssertThat(bag.Capacity("remedy")).IsEqual(10);
        while(bag.Add("remedy"));AssertThat(bag.Count("remedy")).IsEqual(10);
    }
    [TestCase]
    public void TheRemedyRowFitsSevenAndTenSlotsAndKeepsTheOrdinaryRow()
    {
        // The panel's right edge is 342 and the row starts at 136: 8 px of padding, a 6 px gap and the H cap's own width (22.8 px at 12).
        foreach(var slots in new[]{3,4,5,6,7,10})
        {
            var (pitch,icon)=Hud.RemedyRow(slots,22.8f);
            AssertThat(icon).OverrideFailureMessage($"{slots} slots").IsEqual(pitch);
            AssertThat(pitch).IsLessEqual(26f);
            AssertThat(136+slots*pitch+6+22.8f).OverrideFailureMessage($"{slots} slots end at {136+slots*pitch+6+22.8f:F1}").IsLessEqual(342f-8f+.01f);
        }
        AssertThat(Hud.RemedyRow(1,22.8f).Pitch).IsEqual(26f);
        AssertThat(Hud.RemedyRow(3,22.8f)).IsEqual((26f,26f));
        AssertThat(Hud.RemedyRow(7,22.8f).Icon).IsGreater(24f);
        AssertThat(Hud.RemedyRow(10,22.8f).Icon).IsGreater(16f);
        // The H cap keeps its six-pixel gap and Skill stands clear of the longest row.
        AssertThat(136+10*Hud.RemedyRow(10,22.8f).Pitch+6).IsLess(398f-22.8f);
    }
    [TestCase]
    public void ASpentItemKeepsItsSlotUntilAReset()
    {
        var bag=new Inventory();bag.Add("cell");AssertThat(bag.Take("cell")).IsTrue();
        AssertThat(bag.Held.Select(i=>i.Id)).IsEqual(new[]{"remedy","cell","terminal"});
        for(int i=0;i<3;i++)bag.Take("remedy");bag.Fill("remedy");AssertThat(bag.Count("remedy")).IsEqual(3);
        bag.Reset();AssertThat(bag.Held.Select(i=>i.Id)).IsEqual(new[]{"remedy","terminal"});
    }
}
