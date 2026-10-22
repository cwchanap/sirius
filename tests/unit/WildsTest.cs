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
        bool Joins(Vector3 p)=>AtEdge(p,edge,-8,World.WaterLevel)||AtEdge(p,edge,-111,World.CedarLevel)||World.Sea(p.X,p.Z)>=1&&p.Y==World.WaterLevel;
        AssertThat(World.Rivers.Where(r=>!Joins(r.Course[^1])).Select(r=>r.Name)).IsEmpty();
        AssertThat(World.Rivers.Where(r=>World.InPlay(r.Course[0].X,r.Course[0].Z)&&!AtEdge(r.Course[0],-edge,-111,World.CedarLevel)).Select(r=>r.Name)).IsEmpty();
        // The Hoshikawa is the estuary's own level all the way to its falls; the Cedar River runs higher and so has to come down to it.
        AssertThat(World.CedarLevel).IsGreater(World.WaterLevel);
    }
    // No stretch of a river runs over a bed above its water: across the play area the bed under each sheet's middle 6 m, beyond it the
    // gorge floor along each course, falls aside.
    [TestCase]
    public void RiverBedsLieUnderTheirWater()
    {
        foreach(var (mid,level) in new[]{(-8f,World.WaterLevel),(-111f,World.CedarLevel)})
            AssertThat(Enumerable.Range(-64,129).Where(x=>Enumerable.Range(-30,61).Min(i=>World.Height(x,mid+i*.1f))>level-.1f)).IsEmpty();
        foreach(var (name,course) in World.Rivers)
            for(int i=0;i<course.Length-1;i++)
            {
                if(World.Fall(course[i],course[i+1]))continue;
                var dry=Enumerable.Range(0,21).Select(k=>course[i].Lerp(course[i+1],k/20f)).Where(p=>!World.InPlay(p.X,p.Z)&&World.Height(p.X,p.Z)>p.Y-.3f).ToArray();
                AssertThat(dry).OverrideFailureMessage($"{name} reach {i} runs dry at {string.Join(", ",dry)}").IsEmpty();
            }
    }
    // From the village square, walking (a standable floor under 50 degrees, steps up to Mio's 1.24 m jump) or sliding down, and never
    // through the invisible walls, reaches no further than the cliff foot, the beach and the gorges below their falls.
    [TestCase]
    public void TheValleyIsClosed()
    {
        var area=World.Fine;int w=area.Size.X+1,h=area.Size.Y+1;
        var height=new float[w,h];for(int i=0;i<w;i++)for(int j=0;j<h;j++)height[i,j]=World.Height(area.Position.X+i,area.Position.Y+j);
        bool Floor(int i,int j)=>i>0&&j>0&&i<w-1&&j<h-1&&new Vector2(height[i+1,j]-height[i-1,j],height[i,j+1]-height[i,j-1]).Length()/2<Mathf.Tan(Mathf.DegToRad(50));
        var walls=World.Walls();
        bool Cross(Vector2 a,Vector2 b,Vector2 c,Vector2 d){float o(Vector2 p,Vector2 q,Vector2 r)=>(q-p).Cross(r-p);return o(a,b,c)*o(a,b,d)<=0&&o(c,d,a)*o(c,d,b)<=0;}
        var seen=new bool[w,h];var queue=new Queue<(int I,int J)>();int si=-area.Position.X,sj=21-area.Position.Y;seen[si,sj]=true;queue.Enqueue((si,sj));
        float farthest=0;Vector2 far=default;
        while(queue.Count>0)
        {
            var (i,j)=queue.Dequeue();var p=new Vector2(area.Position.X+i,area.Position.Y+j);
            float outside=p.DistanceTo(new(Mathf.Clamp(p.X,-World.HalfWidth,World.HalfWidth),Mathf.Clamp(p.Y,World.NorthEdge,World.SouthEdge)));
            if(outside>farthest){farthest=outside;far=p;}
            foreach(var (di,dj) in new[]{(1,0),(-1,0),(0,1),(0,-1)})
            {
                int a=i+di,b=j+dj;if(a<0||b<0||a>=w||b>=h||seen[a,b])continue;
                float rise=height[a,b]-height[i,j];var q=p+new Vector2(di,dj);
                if(!(Floor(i,j)&&Floor(a,b)?rise<=1.24f:rise<0)||walls.Any(s=>Cross(p,q,s.A,s.B)))continue;
                seen[a,b]=true;queue.Enqueue((a,b));
            }
        }
        AssertThat(farthest).OverrideFailureMessage($"walked {farthest:F1} m out of the play area, to {far}").IsLessEqual(16);
    }
}
