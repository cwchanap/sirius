using GdUnit4;
using static GdUnit4.Assertions;
namespace Hoshikawa.Tests;

[TestSuite]
public class StatsTest
{
    // The player reads hp, sp, atk and def by id; any further row only has to be well formed.
    [TestCase]
    public void EveryStatIsWellFormed()
    {
        AssertThat(Stats.All.Select(s=>s.Id).Distinct().Count()).IsEqual(Stats.All.Length);
        AssertThat(new[]{"hp","sp","atk","def"}.Where(id=>Stats.All.All(s=>s.Id!=id))).IsEmpty();
        AssertThat(Stats.All.Where(s=>s.Base<=0||s.PerPoint<=0).Select(s=>s.Id)).IsEmpty();
    }
    // Like the items, the stats' names are table data, not literals in a T() call.
    [TestCase("ja")]
    [TestCase("zh_TW")]
    public void EveryStatIsTranslated(string lang)
    {
        var ui=Localization.ParseStrings(File.ReadAllText(Repo.Path($"assets/dialogue/ui.{lang}.json")));
        AssertThat(Stats.All.SelectMany(s=>new[]{s.Short,s.Name}).Where(s=>!ui.ContainsKey(s))).IsEmpty();
    }
    [TestCase]
    public void EachLevelCostsMoreThanTheLast()
    {
        AssertThat(Stats.Cost(1)).IsEqual(Stats.FirstCost);
        AssertThat(Enumerable.Range(1,30).Where(l=>Stats.Cost(l+1)<=Stats.Cost(l))).IsEmpty();
    }
    [TestCase]
    public void RaisingAStatSpendsTheNextLevelsCost()
    {
        var growth=new Growth();
        AssertThat(growth.Level).IsEqual(1);AssertThat(growth.Value("hp")).IsEqual(100f);AssertThat(growth.Raise("hp")).IsFalse();
        growth.Stardust=Stats.Cost(1)+Stats.Cost(2)-1;
        AssertThat(growth.Raise("hp")).IsTrue();AssertThat(growth.Level).IsEqual(2);AssertThat(growth.Value("hp")).IsEqual(110f);AssertThat(growth.Stardust).IsEqual(Stats.Cost(2)-1);
        AssertThat(growth.Raise("atk")).IsFalse();AssertThat(growth.Points("atk")).IsEqual(0);AssertThat(growth.Stardust).IsEqual(Stats.Cost(2)-1);
        growth.Reset();AssertThat(growth.Level).IsEqual(1);AssertThat(growth.Stardust).IsEqual(0);AssertThat(growth.Value("hp")).IsEqual(100f);
    }
    [TestCase]
    public void AttackAndDefenseScaleFromOne()
    {
        var growth=new Growth {Stardust=10000};
        AssertThat(growth.Scale("atk")).IsEqual(1f);AssertThat(growth.Scale("def")).IsEqual(1f);
        growth.Raise("atk");growth.Raise("atk");
        AssertThat(Math.Abs(growth.Scale("atk")-1.2f)).IsLess(.0001f);AssertThat(growth.Scale("def")).IsEqual(1f);
    }
    // Remedies come back with rest, so only the resonance cell has a price; she never buys past what she can carry.
    [TestCase]
    public void OnlyPricedItemsAreTradedWhileThereIsRoom()
    {
        var bag=new Inventory();var cell=Items.Get("cell");var growth=new Growth {Stardust=cell.Price*(cell.Max+1)};
        AssertThat(growth.Buy(bag,"remedy")).IsFalse();bag.Take("remedy");AssertThat(growth.Buy(bag,"remedy")).IsFalse();
        for(int i=0;i<cell.Max;i++)AssertThat(growth.Buy(bag,"cell")).IsTrue();
        AssertThat(growth.Buy(bag,"cell")).IsFalse();AssertThat(bag.Count("cell")).IsEqual(cell.Max);AssertThat(growth.Stardust).IsEqual(cell.Price);
        bag.Take("cell");growth.Stardust=cell.Price-1;
        AssertThat(growth.Buy(bag,"cell")).IsFalse();AssertThat(bag.Count("cell")).IsEqual(cell.Max-1);AssertThat(growth.Stardust).IsEqual(cell.Price-1);
    }
}
