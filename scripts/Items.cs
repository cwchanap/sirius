namespace Hoshikawa;

public enum ItemKind { Consumable, Key }
// Name and Text are English keys into the ui tables, translated where they are drawn; Icon is one of Hud.Icons. Use is what a consumable
// does, false when it would do nothing now, and then it is not spent. Each one held of an item with Raises lets Mio carry one more of that item.
// An item with a Price is traded at the save point for that much stardust.
public record Item(string Id,string Name,string Text,string Icon,ItemKind Kind,int Max,Func<Player,bool>? Use=null,string? Raises=null,int Price=0);
public static class Items
{
    public static readonly Item[] All={
        new("remedy","Remedy","Restores 45 vitality. Resting refills them.","remedy",ItemKind.Consumable,3,p=>p.Heal()),
        new("cell","Resonance cell","Restores one charge of Resonance Cut. Resting does not refill these; they are traded where Mio rests.","resonance",ItemKind.Consumable,3,p=>p.Recharge(),Price:80),
        new("pouch","Remedy pouch","Found at the old quarry. Mio carries one more remedy.","remedy",ItemKind.Key,1,Raises:"remedy"),
        new("terminal","Wrist terminal","Gold casing and a little jade lamp. Zero's voice reaches Mio through it.","zero",ItemKind.Key,1),
    };
    public static Item Get(string id)=>All.Single(i=>i.Id==id);
}
// What Mio carries, by item id. An item keeps its slot once held, even spent, until the prologue restarts.
public class Inventory
{
    private readonly Dictionary<string,int> counts=new();
    public Inventory()=>Reset();
    // What she sets out with: her remedies and the terminal Zero speaks through.
    public void Reset(){counts.Clear();Add("terminal");Fill("remedy");}
    public int Count(string id)=>counts.GetValueOrDefault(id);
    public int Capacity(string id)=>Items.Get(id).Max+Items.All.Where(i=>i.Raises==id).Sum(i=>Count(i.Id));
    public IEnumerable<Item> Held=>Items.All.Where(i=>counts.ContainsKey(i.Id));
    public bool Add(string id){if(Count(id)>=Capacity(id))return false;counts[id]=Count(id)+1;return true;}
    public bool Take(string id){if(Count(id)<=0)return false;counts[id]--;return true;}
    public void Fill(string id)=>counts[id]=Capacity(id);
}
