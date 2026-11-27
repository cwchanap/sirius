using GdUnit4;
using Godot;
using static GdUnit4.Assertions;
namespace Hoshikawa.Tests;

// Hoshikawa Station's inside, flooded as Mio walks it on the same cells the builder lays (World.StationCells), each a cell clear of the
// walls on its floor and up or down the stairs: leg 1 is reached with every shortcut shut, nothing past the grand stair, S3, S7 or S12 is
// reached with them all open, each of S10, S8 and S2 is earned by a long walk and worked from the side the walk comes to last, and every
// place leads back to the Machine room's Starwell.
[TestSuite]
public class StationTest
{
    static readonly Dictionary<string,(Vector2 At,int Floor)> Places=new()
    {
        ["Parcels room"]=(new(122,10),World.F1),["Freight office"]=(new(116,22),World.F1),["Freight lift hall"]=(new(108,26),World.B1),["Pump room"]=(new(97,15),World.B1),
        ["Machine room"]=(new(74,15),World.B1),["Baggage tunnel"]=(new(88,28),World.B1),["Gallery east"]=(new(76,5),World.B1),["Mall north alley east"]=(new(100,-6),World.B1),
        ["Mall middle alley east"]=(new(110,-19),World.B1),["Mall south alley"]=(new(80,-32),World.B1),["Mall east alley"]=(new(122,-26),World.B1),["Mall middle alley west"]=(new(20,-19),World.B1),
        ["Mall north alley west"]=(new(40,-6),World.B1),["Gallery west"]=(new(40,5),World.B1),["Boiler room"]=(new(13,15),World.B1),["Coal store"]=(new(13,30),World.B1),
        ["Archive"]=(new(32,28),World.B1),["Station arcade"]=(new(50,15),World.B1),["Waiting room"]=(new(44,10),World.F1),["Concourse"]=(new(70,14),World.F1),
        ["South hall"]=(new(66,-12),World.F1),["Kissaten"]=(new(40,-8),World.F1),["Left luggage"]=(new(92,-6),World.F1),["Post office"]=(new(115,-10),World.F1),
        ["Food hall"]=(new(100,-30),World.F1),["Entrance hall"]=(new(66,-30),World.F1),
    };
    static readonly (Vector2 At,int Floor)[] Beyond={(new(70,30),World.F1),(new(90,30),World.F1),(new(34,24),World.F1),(new(9,7),World.F1),(new(20,17),World.F1),
        (new(9,28),World.F1),(new(12,-8),World.F1),(new(30,-25),World.F1),(new(100,17),World.F1)};
    static readonly (Vector2 At,int Floor) Start=(World.StationExits.Single(e=>e.Name=="PARCELS DOOR").Landing,World.F1),Well=(World.MachineWell,World.B1);
    // The cells Mio can stand in, with the named doors open: floor, a stair's well and ramp, and the leaves of open doors, each with its four
    // neighbours so, a cell's margin standing for her own girth.
    static bool[,,] Clear(params string[] open)
    {
        var cells=World.StationCells;var leaf=new bool[2,World.StationW,World.StationD];
        foreach(var d in World.StationDoors.Where(d=>open.Contains(d.Name)))foreach(var r in d.Leaves)
        {var (i0,j0,i1,j1)=World.Cells(r);for(int i=i0;i<i1;i++)for(int j=j0;j<j1;j++)leaf[d.Floor-World.B1,i,j]=true;}
        bool Raw(int f,int i,int j)=>i>=0&&j>=0&&i<World.StationW&&j<World.StationD&&(cells[f][i,j] is World.Open or World.Hole or World.Ramp||cells[f][i,j]==World.Leaf&&leaf[f,i,j]);
        var clear=new bool[2,World.StationW,World.StationD];
        for(int f=0;f<2;f++)for(int i=0;i<World.StationW;i++)for(int j=0;j<World.StationD;j++)clear[f,i,j]=Raw(f,i,j)&&Raw(f,i-1,j)&&Raw(f,i+1,j)&&Raw(f,i,j-1)&&Raw(f,i,j+1);
        return clear;
    }
    // Walk lengths in metres from a start over the clear cells, eight ways, up and down each stair at the cost of its ramp.
    static float[,,] Walk((Vector2 At,int Floor) from,params string[] open)
    {
        var clear=Clear(open);var cells=World.StationCells;var d=new float[2,World.StationW,World.StationD];
        for(int f=0;f<2;f++)for(int i=0;i<World.StationW;i++)for(int j=0;j<World.StationD;j++)d[f,i,j]=float.PositiveInfinity;
        var s=World.StationCellAt(from.At);var queue=new PriorityQueue<(int F,int I,int J),float>();int f0=from.Floor-World.B1;d[f0,s.X,s.Y]=0;queue.Enqueue((f0,s.X,s.Y),0);
        float c=World.StationCell,diagonal=c*MathF.Sqrt(2);
        while(queue.TryDequeue(out var p,out float at))
        {
            if(at>d[p.F,p.I,p.J])continue;
            void Step(int f,int i,int j,float cost){if(i<0||j<0||i>=World.StationW||j>=World.StationD||!clear[f,i,j]||at+cost>=d[f,i,j])return;d[f,i,j]=at+cost;queue.Enqueue((f,i,j),at+cost);}
            for(int di=-1;di<=1;di++)for(int dj=-1;dj<=1;dj++)if((di|dj)!=0&&(di==0||dj==0||clear[p.F,p.I+di,p.J]&&clear[p.F,p.I,p.J+dj]))Step(p.F,p.I+di,p.J+dj,di!=0&&dj!=0?diagonal:c);
            // A well's cell on the floor above stands over its ramp's: the climb is the ramp's own pitch, counted once, at the cell's step.
            if(cells[p.F][p.I,p.J]==World.Hole&&p.F>0)Step(p.F-1,p.I,p.J,0);
            if(cells[p.F][p.I,p.J]==World.Ramp&&p.F<1)Step(p.F+1,p.I,p.J,0);
        }
        return d;
    }
    static float At(float[,,] d,(Vector2 At,int Floor) p){var c=World.StationCellAt(p.At);return d[p.Floor-World.B1,c.X,c.Y];}
    static float At(float[,,] d,string place)=>At(d,Places[place]);
    static (Vector2,int) Side(string door,bool from){var d=World.StationDoors.Single(x=>x.Name==door);return (from?d.From:d.To,d.Floor);}
    [TestCase]
    public void LegOneIsWalkedWithEveryShortcutShut()
    {
        var d=Walk(Start);
        AssertThat(Places.Keys.Where(p=>float.IsInfinity(At(d,p)))).IsEmpty();
        AssertThat(Beyond.Where(p=>!float.IsInfinity(At(d,p)))).IsEmpty();
        // The first walk, in at the parcels door, round B1 and the mall, up through the waiting room and the south wing to the main doors:
        // the plan's 631 m route looks into every room on the way, so the shortest way, cutting its corners, is most of it.
        float leg=At(d,Side("Main doors",true));AssertThat(leg).OverrideFailureMessage($"leg 1's shortest walk is {leg:F0} m against the plan's 631").IsBetween(480,631);
    }
    [TestCase]
    public void NothingPastTheGrandStairOrTheDoorsShutFromThisSide()
    {
        var d=Walk(Start,"Steel plate","Parcels counter","Main doors");
        AssertThat(Beyond.Where(p=>!float.IsInfinity(At(d,p)))).IsEmpty();
        foreach(var name in new[]{"Staff gate","Department store door","Staff stair door"})
        {
            AssertThat(float.IsInfinity(At(d,Side(name,true)))).OverrideFailureMessage($"{name}'s far side, where it opens, is reached").IsTrue();
            AssertThat(float.IsInfinity(At(d,Side(name,false)))).OverrideFailureMessage($"{name} is not on leg 1").IsFalse();
        }
    }
    // Each shortcut joins two places a long walk apart, and the walk from the Machine room reaches its other side first.
    [TestCase("Steel plate",150)]
    [TestCase("Parcels counter",250)]
    [TestCase("Main doors",45)]
    public void EachShortcutIsEarnedAndWorkedFromTheFarSide(string door,float saves)
    {
        var shut=Walk(Side(door,false));float around=At(shut,Side(door,true));
        AssertThat(around).OverrideFailureMessage($"{door}: {around:F0} m round with it shut").IsGreater(saves);
        var (to,_)=Side(door,false);var (from,_)=Side(door,true);AssertThat(At(Walk(Side(door,false),door),Side(door,true))).IsLess(to.DistanceTo(from)+3);
        var fromWell=Walk(Well);AssertThat(At(fromWell,Side(door,false))).IsLess(At(fromWell,Side(door,true)));
    }
    [TestCase]
    public void EveryPlaceLeadsBackToTheStarwell()
    {
        foreach(var open in new[]{new string[0],["Steel plate"],["Steel plate","Parcels counter"],["Steel plate","Parcels counter","Main doors"]})
        {
            var fromDoor=Walk(Start,open);var fromWell=Walk(Well,open);
            int stranded=0;for(int f=0;f<2;f++)for(int i=0;i<World.StationW;i++)for(int j=0;j<World.StationD;j++)if(!float.IsInfinity(fromDoor[f,i,j])&&float.IsInfinity(fromWell[f,i,j]))stranded++;
            AssertThat(stranded).OverrideFailureMessage($"{stranded} cells with {string.Join(", ",open)} open cannot walk back to the Starwell").IsEqual(0);
        }
    }
    // Every robot, pickup, load, sign, door side and the Starwell stands where Mio can.
    [TestCase]
    public void WhatStandsInsideStandsOnItsFloors()
    {
        var clear=Clear(World.StationDoors.Select(d=>d.Name).ToArray());
        bool On(Vector2 at,int floor){var c=World.StationCellAt(at);return clear[floor-World.B1,c.X,c.Y];}
        var things=World.StationRobots.Select(r=>($"{r.Kind} at {r.At}",r.At,r.Floor)).Concat(World.StationPickups.Select(p=>($"{p.Item} at {p.At}",p.At,p.Floor)))
            .Concat(World.StationLoads.SelectMany(l=>new[]{(l.Name,l.From,l.Floor),(l.Name,l.To,l.Floor)})).Concat(World.StationSigns.Select(s=>(s.Text,s.At,s.Floor)))
            .Concat(World.StationDoors.SelectMany(d=>new[]{(d.Name,d.From,d.Floor),(d.Name,d.To,d.Floor)})).Concat(World.StationExits.SelectMany(e=>new[]{(e.Name,e.At,e.Floor),(e.Name,e.Landing,e.Floor)}))
            .Append(("Machine room well",World.MachineWell+new Vector2(0,-2),World.B1)).Append(("Grand stair bars",World.StairBars,World.F1));
        AssertThat(things.Where(t=>!On(t.Item2,t.Item3)).Select(t=>t.Item1)).IsEmpty();
        var robots=World.StationRobots;
        AssertThat(robots.Length).IsEqual(37);AssertThat(robots.Count(r=>r.Floor==World.B1)).IsEqual(23);
        AssertThat(robots.Count(r=>r.Kind=="patrol")).IsEqual(6);AssertThat(robots.Count(r=>r.Kind=="scout")).IsEqual(21);AssertThat(robots.Count(r=>r.Sleeps!=null)).IsEqual(10);
        AssertThat(robots.Count(r=>r.Kind=="drone")).IsEqual(10);AssertThat(robots.Where(r=>r.Sleeps!=null).All(r=>r.Kind=="scout")).IsTrue();
        AssertThat(World.StationPickups.Count(p=>p.Item=="pouch")).IsEqual(3);AssertThat(World.StationPickups.Count(p=>p.Item=="cell")).IsEqual(5);
        AssertThat(World.SavePoints.Single(s=>s.Place=="Machine room")).IsEqual(("Machine room",new Vector2(World.StationPoint(World.MachineWell,World.B1).X,World.StationPoint(World.MachineWell,World.B1).Z),(float?)World.StationPoint(World.MachineWell,World.B1).Y));
    }
    [TestCase("ja")]
    [TestCase("zh_TW")]
    public void ItsWordsAreTranslated(string lang)
    {
        var ui=Localization.ParseStrings(File.ReadAllText(Repo.Path($"assets/dialogue/ui.{lang}.json")));
        var words=World.StationSigns.Select(s=>s.Text).Concat(World.StationExits.Select(e=>e.Name)).Concat(World.StationDoors.Select(d=>d.Kind.ToUpperInvariant())).Append(World.StationName.ToUpperInvariant());
        AssertThat(words.Where(w=>!ui.ContainsKey(w))).IsEmpty();
    }
}
