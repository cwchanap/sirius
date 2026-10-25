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
    public void ThePouchRaisesTheRemediesSheCarries()
    {
        var bag=new Inventory();
        AssertThat(bag.Add("pouch")).IsTrue();AssertThat(bag.Add("pouch")).IsFalse();
        AssertThat(bag.Capacity("remedy")).IsEqual(4);AssertThat(bag.Add("remedy")).IsTrue();AssertThat(bag.Count("remedy")).IsEqual(4);
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
