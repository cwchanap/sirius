using GdUnit4;
using Godot;
using static GdUnit4.Assertions;
namespace Hoshikawa.Tests;

// The land beyond the play area and the water through it keep to common sense: water runs downhill to the sea, every river has water
// over its whole bed, and the valley is closed by its cliffs, falls and the sea rather than by walls in the open.
[TestSuite]
public class WildsTest
{
    static bool AtEdge(Vector3 p,float x,float z,float level)=>p.X==x&&p.Z==z&&p.Y==level;
    [TestCase]
    public void RiversNeverRunUphill()=>
        AssertThat(World.Rivers.Where(r=>r.Course.Zip(r.Course.Skip(1)).Any(p=>p.Second.Y>p.First.Y)).Select(r=>r.Name)).IsEmpty();
    // A course starts at a source in the mountains or where a valley river leaves the play area, and ends where one enters it, at that
    // river's level, or in the sea at the sea's.
    [TestCase]
    public void RiversJoinTheValleysRiversOrTheSea()
    {
        float edge=World.HalfWidth;
        bool Joins(Vector3 p)=>AtEdge(p,edge,-8,World.WaterLevel)||AtEdge(p,edge,World.CedarZ,World.CedarLevel)||World.Sea(p.X,p.Z)>=1&&p.Y==World.WaterLevel;
        AssertThat(World.Rivers.Where(r=>!Joins(r.Course[^1])).Select(r=>r.Name)).IsEmpty();
        AssertThat(World.Rivers.Where(r=>World.InPlay(r.Course[0].X,r.Course[0].Z)&&!AtEdge(r.Course[0],-edge,World.CedarZ,World.CedarLevel)).Select(r=>r.Name)).IsEmpty();
        // The Hoshikawa is the estuary's own level all the way to its falls; the Cedar River runs higher and so has to come down to it.
        AssertThat(World.CedarLevel).IsGreater(World.WaterLevel);
    }
    // No stretch of a river runs over a bed above its water: across the play area the bed under each sheet's middle 6 m, beyond it the
    // gorge floor along each course, falls aside, and so the head of a spring's ravine, above where its water wells up.
    [TestCase]
    public void RiverBedsLieUnderTheirWater()
    {
        foreach(var (mid,level) in new[]{(-8f,World.WaterLevel),(World.CedarZ,World.CedarLevel)})
            AssertThat(Enumerable.Range(-64,129).Where(x=>Enumerable.Range(-30,61).Min(i=>World.Height(x,mid+i*.1f))>level-.1f)).IsEmpty();
        foreach(var (name,course) in World.Rivers)
            for(int i=0;i<course.Length-1;i++)
            {
                if(World.Fall(course[i],course[i+1]))continue;
                bool Head(Vector3 p)=>!World.InPlay(course[0].X,course[0].Z)&&World.Along(course,p.X,p.Z).From<World.Spring;
                var dry=Enumerable.Range(0,21).Select(k=>course[i].Lerp(course[i+1],k/20f)).Where(p=>!World.InPlay(p.X,p.Z)&&!Head(p)&&World.Height(p.X,p.Z)>p.Y-.3f).ToArray();
                AssertThat(dry).OverrideFailureMessage($"{name} reach {i} runs dry at {string.Join(", ",dry)}").IsEmpty();
            }
    }
    // Where Mio gets to from `from`: walking (a standable floor under 50 degrees, steps up to her 1.24 m jump) or sliding down, never
    // through the invisible walls nor, if given, the cells `blocked` names.
    static (bool[,] Seen,Rect2I Area) Reach(Vector2I from,Func<int,int,bool>? blocked=null)
    {
        var area=World.Fine;int w=area.Size.X+1,h=area.Size.Y+1;
        var height=new float[w,h];for(int i=0;i<w;i++)for(int j=0;j<h;j++)height[i,j]=World.Height(area.Position.X+i,area.Position.Y+j);
        bool Floor(int i,int j)=>i>0&&j>0&&i<w-1&&j<h-1&&new Vector2(height[i+1,j]-height[i-1,j],height[i,j+1]-height[i,j-1]).Length()/2<Mathf.Tan(Mathf.DegToRad(50));
        var walls=World.Walls();
        bool Cross(Vector2 a,Vector2 b,Vector2 c,Vector2 d){float o(Vector2 p,Vector2 q,Vector2 r)=>(q-p).Cross(r-p);return o(a,b,c)*o(a,b,d)<=0&&o(c,d,a)*o(c,d,b)<=0;}
        var seen=new bool[w,h];var queue=new Queue<(int I,int J)>();int si=from.X-area.Position.X,sj=from.Y-area.Position.Y;seen[si,sj]=true;queue.Enqueue((si,sj));
        while(queue.Count>0)
        {
            var (i,j)=queue.Dequeue();var p=new Vector2(area.Position.X+i,area.Position.Y+j);
            foreach(var (di,dj) in new[]{(1,0),(-1,0),(0,1),(0,-1)})
            {
                int a=i+di,b=j+dj;if(a<0||b<0||a>=w||b>=h||seen[a,b]||blocked?.Invoke(area.Position.X+a,area.Position.Y+b)==true)continue;
                float rise=height[a,b]-height[i,j];var q=p+new Vector2(di,dj);
                if(!(Floor(i,j)&&Floor(a,b)?rise<=1.24f:rise<0)||walls.Any(s=>Cross(p,q,s.A,s.B)))continue;
                seen[a,b]=true;queue.Enqueue((a,b));
            }
        }
        return (seen,area);
    }
    static IEnumerable<Vector2I> Cells((bool[,] Seen,Rect2I Area) reach)
    {
        for(int i=0;i<reach.Seen.GetLength(0);i++)for(int j=0;j<reach.Seen.GetLength(1);j++)if(reach.Seen[i,j])yield return reach.Area.Position+new Vector2I(i,j);
    }
    // From the village square Mio reaches no further than the cliff foot, the beach and the gorges below their falls.
    [TestCase]
    public void TheValleyIsClosed()
    {
        var far=Cells(Reach(new(0,21))).Select(c=>(Cell:c,Out:new Vector2(c.X,c.Y).DistanceTo(new(Mathf.Clamp(c.X,-World.HalfWidth,World.HalfWidth),Mathf.Clamp(c.Y,World.NorthEdge,World.SouthEdge))))).MaxBy(c=>c.Out);
        AssertThat(far.Out).OverrideFailureMessage($"walked {far.Out:F1} m out of the play area, to {far.Cell}").IsLessEqual(16);
    }
    // On the relay road she keeps to its trails' floors and their cliffs' lower lips, never the mountain above; the teahouse terrace
    // above the junction is reached only up the switchback, its ladder aside, and the quarry and the plaza only by the valley's and
    // the high road's own ends.
    [TestCase]
    public void TheRelayRoadKeepsToItsTrails()
    {
        var reach=Reach(new(0,21));
        var off=Cells(reach).Where(c=>c.Y<World.RelaySouth&&c.Y>World.RelayNorth&&World.RelayEdge(c.X,c.Y)>2.8f).ToArray();
        AssertThat(off).OverrideFailureMessage($"{off.Length} cells off the trails, e.g. {string.Join(", ",off.Take(5))}").IsEmpty();
        bool Leg(int x,int z)=>x>=26&&x<=34&&z>=-40&&z<=-24;
        AssertThat(Reach(new(2,-30),Leg).Seen[14-reach.Area.Position.X,-58-reach.Area.Position.Y]).OverrideFailureMessage("the junction climbs to the teahouse past the switchback").IsFalse();
        AssertThat(Reach(new(-34,-132),Leg).Seen[2-reach.Area.Position.X,-160-reach.Area.Position.Y]).OverrideFailureMessage("the quarry is not a dead end").IsFalse();
    }
}
