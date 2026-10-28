namespace Hoshikawa;

// Short and Name are English keys into the ui tables, translated where they are drawn. A stat's value is Base plus PerPoint for each point Mio puts in it.
public record Stat(string Id,string Short,string Name,float Base,float PerPoint);
// Every number levelling, stardust and stamina run on, in one place. A new stat is a row here, its two keys in both ui tables, and whatever reads its value.
public static class Stats
{
    public static readonly Stat[] All={
        new("hp","HP","Vitality",100,10),
        new("sp","SP","Stamina",100,10),
        // The sword's damage and posture damage scale by ATK / 10; a strike's damage on Mio by 10 / DEF.
        new("atk","ATK","Attack",10,1),
        new("def","DEF","Defense",10,1),
    };
    public static Stat Get(string id)=>All.Single(s=>s.Id==id);
    // Stardust a shut-down robot leaves Mio, and what the next level costs at each level.
    public const int PatrolReward=150,ScoutReward=60,DroneReward=25,FirstCost=100,CostStep=50;
    public static int Cost(int level)=>FirstCost+CostStep*(level-1);
    // Running spends SP each second; it comes back once she has stopped running a moment. Run dry, she cannot run again until it is back to Winded's share.
    public const float RunCost=20,Regen=40,RegenDelay=.5f,Winded=.3f;
}
// Mio's level, the points she has put in each stat, and the stardust she carries.
public class Growth
{
    private readonly Dictionary<string,int> points=new();
    public int Stardust;
    public int Points(string id)=>points.GetValueOrDefault(id);
    public int Level=>1+points.Values.Sum();
    public int NextCost=>Stats.Cost(Level);
    public float Value(string id){var s=Stats.Get(id);return s.Base+s.PerPoint*Points(id);}
    // How far a stat has grown from where she started: 1 at the start.
    public float Scale(string id)=>Value(id)/Stats.Get(id).Base;
    public bool Spend(int amount){if(amount>Stardust)return false;Stardust-=amount;return true;}
    public bool Raise(string id){if(!Spend(NextCost))return false;points[id]=Points(id)+1;return true;}
    // One more of a priced item, if she has room for it and the stardust.
    public bool Buy(Inventory bag,string id)
    {
        var item=Items.Get(id);
        return item.Price>0&&bag.Count(id)<bag.Capacity(id)&&Spend(item.Price)&&bag.Add(id);
    }
    public void Reset(){points.Clear();Stardust=0;}
}
