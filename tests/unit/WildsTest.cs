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
    // The 1 m ground with its collider, over the play area's margin and Tunnel Mountain, and which of its cells are standable floor (under
    // 50 degrees); worked out once, on every core.
    static readonly Rect2I Area=World.Fine.Merge(World.MazeFine);
    static readonly Lazy<(float[,] Height,bool[,] Floor)> Ground=new(()=>
    {
        int w=Area.Size.X+1,h=Area.Size.Y+1;var height=new float[w,h];var floor=new bool[w,h];
        Parallel.For(0,w,i=>{for(int j=0;j<h;j++){var p=Area.Position+new Vector2I(i,j);height[i,j]=World.IsFine(p)?World.Height(p.X,p.Y):float.NaN;}});
        for(int i=1;i<w-1;i++)for(int j=1;j<h-1;j++)floor[i,j]=new Vector2(height[i+1,j]-height[i-1,j],height[i,j+1]-height[i,j-1]).Length()/2<Mathf.Tan(Mathf.DegToRad(50));
        return (height,floor);
    });
    // Where Mio gets to from `from`: walking (a standable floor, steps up to her 1.24 m jump) or sliding down, never through the invisible
    // walls, off the 1 m ground nor, if given, the cells `blocked` names; across each of `links`, a built deck or a stair, either way. Back,
    // it is every cell from which she gets to `from`.
    internal static (bool[,] Seen,Rect2I Area) Reach(Vector2I from,Func<int,int,bool>? blocked=null,IEnumerable<(Vector2 A,Vector2 B)>? links=null,bool back=false)
    {
        var (height,floor)=Ground.Value;int w=height.GetLength(0),h=height.GetLength(1);var walls=World.Walls();
        bool Cross(Vector2 a,Vector2 b,Vector2 c,Vector2 d){float o(Vector2 p,Vector2 q,Vector2 r)=>(q-p).Cross(r-p);return o(a,b,c)*o(a,b,d)<=0&&o(c,d,a)*o(c,d,b)<=0;}
        (int,int) Cell(Vector2 p)=>((int)Mathf.Round(p.X)-Area.Position.X,(int)Mathf.Round(p.Y)-Area.Position.Y);
        var joined=new Dictionary<(int,int),List<(int,int)>>();
        foreach(var (a,b) in links??[]){foreach(var (x,y) in new[]{(a,b),(b,a)}){if(!joined.TryGetValue(Cell(x),out var to))joined[Cell(x)]=to=new();to.Add(Cell(y));}}
        var seen=new bool[w,h];var queue=new Queue<(int I,int J)>();var start=Cell(from);seen[start.Item1,start.Item2]=true;queue.Enqueue(start);
        while(queue.Count>0)
        {
            var (i,j)=queue.Dequeue();var p=new Vector2(Area.Position.X+i,Area.Position.Y+j);
            foreach(var (di,dj) in new[]{(1,0),(-1,0),(0,1),(0,-1)})
            {
                int a=i+di,b=j+dj;if(a<0||b<0||a>=w||b>=h||seen[a,b]||float.IsNaN(height[a,b])||blocked?.Invoke(Area.Position.X+a,Area.Position.Y+b)==true)continue;
                float rise=back?height[i,j]-height[a,b]:height[a,b]-height[i,j];var q=p+new Vector2(di,dj);
                if(!(floor[i,j]&&floor[a,b]?rise<=1.24f:rise<0)||walls.Any(s=>Cross(p,q,s.A,s.B)))continue;
                seen[a,b]=true;queue.Enqueue((a,b));
            }
            foreach(var (a,b) in joined.GetValueOrDefault((i,j))??[])if(!seen[a,b]&&blocked?.Invoke(Area.Position.X+a,Area.Position.Y+b)!=true){seen[a,b]=true;queue.Enqueue((a,b));}
        }
        return (seen,Area);
    }
    // The shortest walk in metres from `from` to `to` over the same moves as Reach(), eight ways (a diagonal only between two floors), a
    // link costing its own length; infinity if she cannot get there.
    internal static float Walk(Vector2 from,Vector2 to,Func<int,int,bool>? blocked=null,IEnumerable<(Vector2 A,Vector2 B)>? links=null)
    {
        var (height,floor)=Ground.Value;int w=height.GetLength(0),h=height.GetLength(1);
        (int,int) Cell(Vector2 p)=>((int)Mathf.Round(p.X)-Area.Position.X,(int)Mathf.Round(p.Y)-Area.Position.Y);
        var joined=new Dictionary<(int,int),List<((int,int) To,float D)>>();
        foreach(var (a,b) in links??[]){foreach(var (x,y) in new[]{(a,b),(b,a)}){if(!joined.TryGetValue(Cell(x),out var l))joined[Cell(x)]=l=new();l.Add((Cell(y),a.DistanceTo(b)));}}
        var dist=new float[w,h];for(int i=0;i<w;i++)for(int j=0;j<h;j++)dist[i,j]=float.PositiveInfinity;
        var start=Cell(from);var goal=Cell(to);dist[start.Item1,start.Item2]=0;var queue=new PriorityQueue<(int I,int J),float>();queue.Enqueue(start,0);
        bool Step(int i,int j,int a,int b){float rise=height[a,b]-height[i,j];return floor[i,j]&&floor[a,b]?rise<=1.24f:rise<0;}
        while(queue.TryDequeue(out var c,out float d))
        {
            var (i,j)=c;if(d>dist[i,j])continue;if(c==goal)return d;
            foreach(var (di,dj) in new[]{(1,0),(-1,0),(0,1),(0,-1),(1,1),(1,-1),(-1,1),(-1,-1)})
            {
                int a=i+di,b=j+dj;if(a<0||b<0||a>=w||b>=h||float.IsNaN(height[a,b])||blocked?.Invoke(Area.Position.X+a,Area.Position.Y+b)==true||!Step(i,j,a,b))continue;
                if(di!=0&&dj!=0&&!(floor[a,j]&&floor[i,b]))continue;
                float n=d+(di!=0&&dj!=0?Mathf.Sqrt2:1);if(n<dist[a,b]){dist[a,b]=n;queue.Enqueue((a,b),n);}
            }
            foreach(var ((a,b),dd) in joined.GetValueOrDefault((i,j))??[])
                if(blocked?.Invoke(Area.Position.X+a,Area.Position.Y+b)!=true&&d+dd<dist[a,b]){dist[a,b]=d+dd;queue.Enqueue((a,b),d+dd);}
        }
        return float.PositiveInfinity;
    }
    internal static IEnumerable<Vector2I> Cells((bool[,] Seen,Rect2I Area) reach)
    {
        for(int i=0;i<reach.Seen.GetLength(0);i++)for(int j=0;j<reach.Seen.GetLength(1);j++)if(reach.Seen[i,j])yield return reach.Area.Position+new Vector2I(i,j);
    }
    // From the village square Mio reaches no further than the cliff foot, the beach and the gorges below their falls, but for Tunnel
    // Mountain's floors and the cliffs she slides down off them (MazeTest keeps her to those).
    [TestCase]
    public void TheValleyIsClosed()
    {
        var far=Cells(Reach(new(0,21))).Where(c=>World.MazeEdge(c.X,c.Y)>3.5f).Select(c=>(Cell:c,Out:new Vector2(c.X,c.Y).DistanceTo(new(Mathf.Clamp(c.X,-World.HalfWidth,World.HalfWidth),Mathf.Clamp(c.Y,World.NorthEdge,World.SouthEdge))))).MaxBy(c=>c.Out);
        AssertThat(far.Out).OverrideFailureMessage($"walked {far.Out:F1} m out of the play area, to {far.Cell}").IsLessEqual(16);
    }
    // On the relay road she keeps to its trails' floors and their cliffs' lower lips, never the mountain above; the teahouse terrace
    // above the junction is reached only up the switchback, its ladder aside, and the quarry and the plaza only by the valley's and
    // the high road's own ends.
    [TestCase]
    public void TheRelayRoadKeepsToItsTrails()
    {
        var reach=Reach(new(0,21));
        var off=Cells(reach).Where(c=>c.X<=World.HalfWidth&&c.Y<World.RelaySouth&&c.Y>World.RelayNorth&&World.RelayEdge(c.X,c.Y)>2.8f).ToArray();
        AssertThat(off).OverrideFailureMessage($"{off.Length} cells off the trails, e.g. {string.Join(", ",off.Take(5))}").IsEmpty();
        bool Leg(int x,int z)=>x>=26&&x<=34&&z>=-40&&z<=-24;
        AssertThat(Reach(new(2,-30),Leg).Seen[14-reach.Area.Position.X,-58-reach.Area.Position.Y]).OverrideFailureMessage("the junction climbs to the teahouse past the switchback").IsFalse();
        AssertThat(Reach(new(-34,-132),Leg).Seen[2-reach.Area.Position.X,-160-reach.Area.Position.Y]).OverrideFailureMessage("the quarry is not a dead end").IsFalse();
    }
    // The relay road's ground is continuous: no step where one trail reach's nearest point gives way to another's, which the first Metal
    // review saw across the mountain from the air. It is walked in 5 cm strides along every metre line, and any stride rising over 20 cm,
    // a cliff or a step, is halved ten times: that leaves a cliff a fraction of a millimetre high and a step its full height.
    [TestCase]
    public void TheRelayGroundHasNoSteps()
    {
        float H(Vector2 p)=>World.Height(p.X,p.Y);
        float Jump(Vector2 a,Vector2 b){for(int i=0;i<10;i++){var m=(a+b)/2;if(Math.Abs(H(m)-H(a))>Math.Abs(H(b)-H(m)))b=m;else a=m;}return Math.Abs(H(b)-H(a));}
        var steps=new List<string>();
        void Walk(Vector2 from,Vector2 stride,int count){float h=H(from);for(int i=0;i<count;i++){var a=from+stride*i;float next=H(a+stride);if(Math.Abs(next-h)>.2f&&Jump(a,a+stride)>.05f)steps.Add($"({a.X:F2}, {a.Y:F2})");h=next;}}
        for(int z=(int)World.RelaySouth;z>=World.RelayNorth;z--)Walk(new(-World.HalfWidth,z),new(.05f,0),2560);
        for(int x=-(int)World.HalfWidth;x<=World.HalfWidth;x++)Walk(new(x,World.RelaySouth),new(0,-.05f),3280);
        AssertThat(steps).OverrideFailureMessage($"{steps.Count} steps, e.g. {string.Join(", ",steps.Take(5))}").IsEmpty();
    }
}
