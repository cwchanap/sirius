using Godot;
namespace Hoshikawa;

// The land beyond the play area: mountain walls on three sides, the south-west sea, and the rivers' courses into and out of the valley.
// Height() hands every point outside the play area to Outside(), so the terrain mesh, its collider and the water read the same ground.
public partial class World
{
    // The 1 m terrain and its collider reach this far past the play area, over the cliffs, gorges and beach; 4 m mountains carry on to Far.
    public const int Margin = 40;
    public static readonly Rect2I Fine = new(-HalfWidth-Margin,NorthEdge-Margin,2*(HalfWidth+Margin),SouthEdge-NorthEdge+2*Margin), Far = new(-180,NorthEdge-100,360,SouthEdge-NorthEdge+200);
    public static bool InPlay(float x,float z)=>Math.Abs(x)<=HalfWidth&&z>=NorthEdge&&z<=SouthEdge;
    // The rivers beyond the play area, upstream first: (x, water level, z) points whose levels never rise downstream, a fall being two
    // points a metre apart with straight reaches either side. Each course meets a play-area river at its edge with that river's level,
    // or the sea at the sea's. The Hoshikawa and the Cedar River spring from hollows high on the eastern range and run straight down its
    // slope, bending with its folds, to fall twice into the valley; the Cedar River leaves it again through a canyon in the western range
    // to its own mouth on the bay.
    public static readonly (string Name,Vector3[] Course)[] Rivers={
        ("Hoshikawa headwater",new Vector3[]{new(164,35,-64),new(148,29,-56),new(133,23.5f,-49),new(119,18.5f,-42.5f),new(107.9f,14.5f,-35.8f),new(100,13,-32.5f),new(99.08f,7.4f,-32.12f),new(91.7f,7.1f,-29),new(89,5.8f,-8.5f),new(80,5.62f,-8),new(79,WaterLevel,-8),new(HalfWidth,WaterLevel,-8)}),
        ("Cedar headwater",new Vector3[]{new(157.3f,44.6f,-129),new(146.7f,39.8f,-123.4f),new(134.9f,35,-122.1f),new(123.3f,30.2f,-125.2f),new(112,25.4f,-129.2f),new(106.3f,23,-131),new(100,20,-128),new(99.1f,11,-127.57f),new(91.9f,10.6f,-124.1f),new(89,6,-111.5f),new(80,5.7f,-111),new(79,CedarLevel,-111),new(HalfWidth,CedarLevel,-111)}),
        ("Cedar outlet",new Vector3[]{new(-HalfWidth,CedarLevel,-111),new(-74,.62f,-111),new(-92,.55f,-104),new(-110,.45f,-90),new(-124,.25f,-70),new(-131,WaterLevel,-48),new(-134,WaterLevel,-30),new(-136,WaterLevel,-14)}),
    };
    // A fall is a drop between two points under 1.5 m apart; anything longer is a reach, however steep.
    public static bool Fall(Vector3 top,Vector3 foot)=>new Vector2(foot.X-top.X,foot.Z-top.Z).Length()<1.5f&&top.Y-foot.Y>1;
    // Where the river's floor runs level, either side of its course; the gorge's walls climb from there to the mountain over at least
    // 4.5 m, and over 80% of their height where that is more, so a deep gorge opens into a valley. Past 44 m from the play area, above
    // the falls, they ease to 1.4 times their height by 70 m, opening the gorge into a mountain valley. A spring's ravine starts as a
    // fold in the hillside and cuts down to its full depth over its first 24 m, where its water wells up.
    // A reach's water fills whatever of its ground lies under its level within 16 m of its course.
    private const float GorgeFloor=3.5f,GorgeWall=4.5f,Spill=16;public const float Spring=24;
    // The floor's half width at `from` metres down a course, `d` out from the play area: wandering by up to 1.3 m once clear of the edge,
    // where it meets the valley's own channel, so no gorge runs on like a canal.
    public static float Half(float from,float d)=>GorgeFloor+(.8f*Mathf.Sin(from*.13f)+.5f*Mathf.Sin(from*.31f+1))*Mathf.SmoothStep(4,16,d);
    // How far (x, z) lies beyond the play area.
    public static float Beyond(float x,float z)=>new Vector2(x-Mathf.Clamp(x,-HalfWidth,HalfWidth),z-Mathf.Clamp(z,NorthEdge,SouthEdge)).Length();
    // 1 over the south-west sea, 0 over the mountains: west of the play area south of the headland at the Hoshikawa's mouth, and south of it
    // west of the south-east headland, each coast wandering. The headlands' sea cliffs face it over 6 m beside the play area, easing to
    // mountainsides further out, and rocky capes run out into it.
    public static float Sea(float x,float z)
    {
        float w=6+.4f*Math.Max(0,Beyond(x,z)-24);
        float sea=Math.Max(x<-HalfWidth?Mathf.SmoothStep(-20-w,-20,z+6*Mathf.Sin(x*.05f)):0,z>SouthEdge?1-Mathf.SmoothStep(20,20+w,x+8*Mathf.Sin(z*.06f)):0);
        var p=new Vector2(x,z);
        foreach(var (a,b,r) in Capes)
        {
            float t=Mathf.Clamp((p-a).Dot(b-a)/(b-a).LengthSquared(),0,1),narrow=r*(1-.45f*t)+2.5f*Mathf.Sin(x*.31f+z*.17f)*Mathf.Sin(x*.13f-z*.29f);
            sea*=1-(1-Mathf.SmoothStep(narrow-5,narrow,p.DistanceTo(a.Lerp(b,t))))*(1-.55f*t);
        }
        return sea;
    }
    // Rocky capes from the edge out into the sea, each a ridge narrowing and sinking from its root to its tip, so the coast does not
    // follow the play area's edges and corner.
    private static readonly (Vector2 Root,Vector2 Tip,float Radius)[] Capes={(new(-66,24),new(-92,36),10),(new(-66,60),new(-82,70),8),(new(-64,80),new(-92,104),11),(new(-20,82),new(-34,102),9)};
    // Outside the play area the ground carries on from its nearest edge point: up a cliff into the mountains, down a beach under the sea,
    // or, along a river's course, through its gorge, whose floor eases over 6 m from the edge's own river bed to 80 cm under the course's
    // level, falls included. In the sea a gorge only ever deepens the seabed. Under the mountains the ground eases over the first 12 m
    // from the edge's height to the uplands' 3.5 m, so no river bed, ditch or rise that meets the edge runs on across them as a trench
    // or a crease.
    private static float Outside(float x,float z)
    {
        float cx=Mathf.Clamp(x,-HalfWidth,HalfWidth),cz=Mathf.Clamp(z,NorthEdge,SouthEdge),d=new Vector2(x-cx,z-cz).Length(),sea=Sea(x,z),edge=Inside(cx,cz);
        // The beach starts down to the sea up to 6 m out from the edge, so the coast wanders instead of following it.
        float shore=3+3*Mathf.Sin(x*.07f+z*.05f)*Mathf.Sin(x*.031f-z*.083f);
        float y=Mathf.Lerp(edge,Mathf.Lerp(3.5f,edge,sea),Mathf.SmoothStep(0,12,d))+(1-sea)*Mountain(x,z,d)-sea*(3*Mathf.SmoothStep(shore,shore+8,d)+.08f*Math.Max(0,d-shore-8));
        foreach(var (_,course) in Rivers)
        {
            var (gap,level,_,from,smooth,soft)=Along(course,x,z);
            float cut=InPlay(course[0].X,course[0].Z)?1:Mathf.SmoothStep(0,Spring,from),half=Half(from,d),foot=Mathf.SmoothStep(half,half+2,gap);
            // The floor keeps its nearest point's level and distance, falls and all; over the walls' first 2 m both ease to the smooth ones.
            float floor=Mathf.Lerp(y,Mathf.Lerp(edge,Mathf.Lerp(level,smooth,foot)-.8f,Mathf.SmoothStep(0,6,d)),cut);
            float gorge=Mathf.Lerp(y,floor,1-Mathf.SmoothStep(half,half+Math.Max(GorgeWall,(y-floor)*Mathf.Lerp(.8f,1.4f,Mathf.SmoothStep(44,70,d))),Mathf.Lerp(gap,soft,foot)));
            // The bank rises to 50 cm above the water within 2 m of the floor's edge. Where a reach runs across a slope and the hillside
            // lies lower, the bank stands 2 m wide as a levee before falling away to it, so no reach spills.
            float bank=Mathf.Lerp(floor,level+.5f,Mathf.SmoothStep(half,half+2,gap))-(1-Mathf.SmoothStep(level+.5f,level+1.5f,y))*Math.Max(0,gap-half-4);
            y=sea>0?Math.Min(y,gorge):Math.Max(gorge,bank);
        }
        return y;
    }
    // An 8 to 12 m cliff, rising over 5 m from a foot that wanders up to 4 m out from the edge: steeper than Mio's 50 degree floor limit
    // over 70% of its height and taller than her 1.24 m jump. Then ridges rising to 5-40 m.
    private static float Mountain(float x,float z,float d)
    {
        float foot=2+2*Mathf.Sin(x*.11f+z*.07f)*Mathf.Sin(x*.05f-z*.13f),cliff=10+2*Mathf.Sin(x*.09f+1)*Mathf.Sin(z*.07f);
        return cliff*Mathf.SmoothStep(0,5,d-foot)+Mathf.SmoothStep(6,70,d)*(22+7*Mathf.Sin(x*.047f)+6*Mathf.Sin(z*.036f)+4*Mathf.Sin((x+z)*.061f));
    }
    // The distance from (x, z) to a course, the water level at its nearest point and that point's segment; then how far down the course
    // and at what level the nearest points on that point's run of reaches between falls lie, weighted by the inverse fourth power of
    // their distance, and the run's distance with its corners rounded over 4 m. Unlike the nearest point's, those change smoothly where
    // one reach gives way to the next round a bend, so the walls follow them; across a fall they step with the floor near it.
    public static (float Gap,float Level,int Segment,float From,float Smooth,float Soft) Along(Vector3[] course,float x,float z)
    {
        var p=new Vector2(x,z);float best=float.MaxValue,near=0;int segment=0;
        (float Gap,float T) Near(int i){Vector2 a=new(course[i].X,course[i].Z),b=new(course[i+1].X,course[i+1].Z);float t=Mathf.Clamp((p-a).Dot(b-a)/(b-a).LengthSquared(),0,1);return (p.DistanceTo(a.Lerp(b,t)),t);}
        for(int i=0;i<course.Length-1;i++){var (gap,t)=Near(i);if(gap<best){best=gap;near=t;segment=i;}}
        int first=Fall(course[segment],course[segment+1])?segment+(near<.5f?-1:1):segment,last=first;
        while(first>0&&!Fall(course[first-1],course[first]))first--;
        while(last<course.Length-2&&!Fall(course[last+1],course[last+2]))last++;
        // Sums over the run (0) and over every reach (1).
        float run=0,soft=float.MaxValue;var from=new float[2];var smooth=new float[2];var weights=new float[2];
        for(int i=0;i<course.Length-1;i++)
        {
            float length=new Vector2(course[i+1].X-course[i].X,course[i+1].Z-course[i].Z).Length();
            if(!Fall(course[i],course[i+1]))
            {
                var (gap,t)=Near(i);float w=1/(gap*gap*gap*gap+1e-3f),y=Mathf.Lerp(course[i].Y,course[i+1].Y,t);
                for(int k=i>=first&&i<=last?0:1;k<2;k++){from[k]+=w*(run+t*length);smooth[k]+=w*y;weights[k]+=w;}
                if(i>=first&&i<=last){float h=Math.Max(4-Math.Abs(gap-soft),0)/4;soft=Math.Min(gap,soft)-h*h;}
            }
            run+=length;
        }
        // Away from the floor the run gives way to the whole course, so a fall's step fades out across the walls by 20 m.
        float all=Mathf.SmoothStep(8,20,best);
        return (best,Mathf.Lerp(course[segment].Y,course[segment+1].Y,near),segment,Mathf.Lerp(from[0]/weights[0],from[1]/weights[1],all),Mathf.Lerp(smooth[0]/weights[0],smooth[1]/weights[1],all),soft);
    }
    // The water level beside (x, z) outside the play area, if any: the sea's, or a river's within its gorge.
    private static float? WaterOutside(float x,float z)
    {
        if(Sea(x,z)>0)return WaterLevel;
        foreach(var (_,course) in Rivers){var a=Along(course,x,z);if(a.Gap<Spill)return a.Level;}
        return null;
    }
    // Each course's water cells, 1 m over the 1 m ground and 4 m beyond: ground under its level within Spill of it, joined to it. The
    // joining is traced on 1 m cells everywhere, since a 4 m cell can straddle a levee and carry the water over it into the hollow behind;
    // a 4 m cell holds water only where one of its 1 m cells does.
    private static readonly Lazy<(List<Vector2I> Near,List<Vector2I> Far,HashSet<Vector2I> Joined)[]> RiverWater=new(()=>Rivers.Select(r=>
    {
        var course=r.Course;var box=new Rect2(course[0].X,course[0].Z,0,0);foreach(var p in course)box=box.Expand(new(p.X,p.Z));box=box.Grow(Spill+4);
        var area=new Rect2I((Vector2I)(box.Position/4).Floor()*4,(Vector2I)(box.Size/4).Ceil()*4+new Vector2I(4,4));
        bool Gorge(Vector2 c)=>!InPlay(c.X,c.Y)&&Sea(c.X,c.Y)<=0&&Along(course,c.X,c.Y).Gap<Spill;
        float Level(float x,float z)=>Along(course,x,z).Level;
        var joined=Wet(area.Intersection(Far),1,Gorge,Level,c=>Along(course,c.X,c.Y).Gap<2);var all=joined.ToHashSet();
        return (joined.Where(c=>Fine.HasPoint(c)).ToList(),Wet(area.Intersection(Far),4,c=>Gorge(c)&&!Fine.HasPoint((Vector2I)c.Floor()),Level)
            .Where(c=>Enumerable.Range(0,16).Any(i=>all.Contains(c+new Vector2I(i%4,i/4)))).ToList(),all);
    }).ToArray());
    // The corners of every river water cell, and the ground within one 4 m cell of them. Shore() paints a river's wet band only on that
    // ground, and below the river's level only under its water, so a hollow under a reach's level that its water cannot reach stays as dry
    // as it looks, while a 4 m bank vertex beside the water still carries its height up the band.
    private static readonly Lazy<HashSet<Vector2I>> Damp=new(()=>RiverWater.Value.SelectMany(w=>w.Joined)
        .SelectMany(c=>new[]{c,c+new Vector2I(1,0),c+new Vector2I(0,1),c+new Vector2I(1,1)}).ToHashSet());
    private static readonly Lazy<HashSet<Vector2I>> Banks=new(()=>Damp.Value.SelectMany(c=>Enumerable.Range(0,81).Select(i=>c+new Vector2I(i%9-4,i/9-4))).ToHashSet());
    private void Wilds()
    {
        var sea=WaterMaterial(Vector2.Zero);
        bool Open(Vector2 c)=>!InPlay(c.X,c.Y)&&Sea(c.X,c.Y)>0;
        WaterGrid("Sea",Wet(Fine,1,Open,(_,_)=>WaterLevel),1,(_,_)=>WaterLevel,sea);
        WaterGrid("Open sea",Wet(Far,4,c=>Open(c)&&!Fine.HasPoint((Vector2I)c.Floor()),(_,_)=>WaterLevel),4,(_,_)=>WaterLevel,sea);
        // Past the 4 m mountains the sea runs on under the fog to the horizon, west of the whole range as well as south of it, ending
        // further out than any view reaches.
        Mesh(this,"Sea to the horizon",new PlaneMesh{Size=new(2000,3000),SubdivideWidth=249},new(Far.Position.X-1000,WaterLevel,0),sea);
        Mesh(this,"Sea to the south horizon",new PlaneMesh{Size=new(220,1500-Far.End.Y),SubdivideWidth=54},new(Far.Position.X+110,WaterLevel,(1500+Far.End.Y)/2f),sea);
        foreach(var ((name,course),(near,far,_)) in Rivers.Zip(RiverWater.Value))
        {
            float Level(float x,float z)=>Along(course,x,z).Level;
            // One mesh per straight reach, so each drifts its own way; neighbours share their edge vertices. Past the 1 m ground the 4 m
            // ground carries the upper reaches on its own grid, meeting the 1 m water along a line of constant x or z.
            for(int i=0;i<course.Length-1;i++)
            {
                if(Fall(course[i],course[i+1])){var top=course[i];Waterfall($"{name} {i}",top,course[i+1],Half(Along(course,top.X,top.Z).From,Beyond(top.X,top.Z)));continue;}
                int segment=i;var flow=new Vector2(course[i+1].X-course[i].X,course[i+1].Z-course[i].Z).Normalized()*.35f;var material=WaterMaterial(flow);
                bool Reach(Vector2I c,int step)=>Along(course,c.X+step/2f,c.Y+step/2f).Segment==segment;
                WaterGrid($"{name} {i}",near.Where(c=>Reach(c,1)),1,Level,material);
                WaterGrid($"{name} {i} far",far.Where(c=>Reach(c,4)),4,Level,material);
            }
        }
        var walls=new StaticBody3D{Name="Invisible wall",CollisionMask=0};AddChild(walls);
        foreach(var (a,b) in Walls())
        {
            var d=b-a;
            walls.AddChild(new CollisionShape3D{Position=new((a.X+b.X)/2,8,(a.Y+b.Y)/2),Rotation=new(0,Mathf.Atan2(d.X,d.Y),0),Shape=new BoxShape3D{Size=new(.5f,40,d.Length()+.5f)}});
        }
        for(int i=0;i<6;i++){float s=1.5f+.3f*(i%3),x=CanyonChoke.X+(i%2)*1.2f,z=CanyonChoke.Y-5+i*2;Sphere(this,"Canyon choke boulder",new(x,Height(x,z)+s*.45f,z),new(s,s*.8f,s),stone).Rotation=new(0,i*1.7f,.2f);}
    }
    private ShaderMaterial WaterMaterial(Vector2 flow)
    {
        var water=new ShaderMaterial{Shader=GD.Load<Shader>("res://assets/shaders/water.gdshader")};water.SetShaderParameter("flow",flow);return water;
    }
    // The cells of `area` that `owns` admits and that hold water: a corner's ground lies under the water's level there + 4 cm, so wherever
    // the water meets dry cells its edge runs along the terrain mesh's chords, buried deeper than the shader's wave. Cells across a fall,
    // whose corners' levels differ by more than a steep reach's do, stay dry: the fall's curtain covers them. Given a `source`, only cells
    // joined side to side to one it admits hold water, so a hollow beyond a reach's levee, under its level but out of its reach, stays dry.
    private static List<Vector2I> Wet(Rect2I area,int step,Func<Vector2,bool> owns,Func<float,float,float> level,Func<Vector2,bool>? source=null)
    {
        var wet=new List<Vector2I>();
        for(int z=area.Position.Y;z<area.End.Y;z+=step)for(int x=area.Position.X;x<area.End.X;x+=step)
        {
            if(!owns(new(x+step/2f,z+step/2f)))continue;
            var corners=new[]{(x,z),(x+step,z),(x,z+step),(x+step,z+step)};var l=corners.Select(c=>level(c.Item1,c.Item2)).ToArray();
            // Along the 1 m ground's edge the terrain has a vertex every metre, each of which may let the water in.
            var rim=step>1&&FineSide(new(x,z),step) is var (a,b,_,_)?Enumerable.Range(1,step-1).Select(i=>a+(b-a)/step*i).ToArray():Array.Empty<Vector2I>();
            if(l.Max()-l.Min()>.7f*step||corners.Select((c,i)=>Height(c.Item1,c.Item2)-l[i]).Concat(rim.Select(q=>Height(q.X,q.Y)-level(q.X,q.Y))).Min()>=.04f)continue;
            wet.Add(new(x,z));
        }
        if(source==null)return wet;
        var all=wet.ToHashSet();var queue=new Queue<Vector2I>(wet.Where(c=>source(c+new Vector2(step/2f,step/2f))));var joined=queue.ToHashSet();
        while(queue.TryDequeue(out var c))foreach(var d in new Vector2I[]{new(step,0),new(-step,0),new(0,step),new(0,-step)})if(all.Contains(c+d)&&joined.Add(c+d))queue.Enqueue(c+d);
        return wet.Where(joined.Contains).ToList();
    }
    // Water over `cells`, each `step` metres on the terrain's own grid and diagonals. Each cell leans with its slope, and each vertex with
    // the cells around it, which water.gdshader measures depth square to and breaks into white water running on across them on a steep reach.
    private void WaterGrid(string name,IEnumerable<Vector2I> cells,int step,Func<float,float,float> level,Material material)
    {
        var lean=cells.ToDictionary(c=>c,c=>{float a=level(c.X,c.Y),b=level(c.X+step,c.Y),d=level(c.X,c.Y+step),e=level(c.X+step,c.Y+step);return new Vector3(a-b+d-e,2*step,a-d+b-e).Normalized();});
        if(lean.Count==0)return;
        var st=new SurfaceTool();st.Begin(Godot.Mesh.PrimitiveType.Triangles);
        void V(int x,int z){st.SetNormal(new Vector2I[]{new(x,z),new(x-step,z),new(x,z-step),new(x-step,z-step)}.Select(c=>lean.GetValueOrDefault(c)).Aggregate((m,n)=>m+n).Normalized());st.AddVertex(new(x,level(x,z),z));}
        foreach(var (x,z) in lean.Keys){V(x,z);V(x+step,z);V(x,z+step);V(x+step,z);V(x+step,z+step);V(x,z+step);}
        st.Index();Mesh(this,name,st.Commit(),Vector3.Zero,material);
    }
    // A fall from `top` down to `foot`, a metre downstream: a curtain over the gorge floor's width, `half` either side, lipping over from
    // the upper water and dropping in front of the rock face to 30 cm under the pool.
    private void Waterfall(string name,Vector3 top,Vector3 foot,float half)
    {
        var flow=new Vector2(foot.X-top.X,foot.Z-top.Z).Normalized();var across=new Vector3(-flow.Y,0,flow.X)*(half+.6f);var down=new Vector3(flow.X,0,flow.Y);
        var lip=new Vector3(top.X,top.Y,top.Z)-down*.6f;var edge=new Vector3(foot.X,top.Y-.25f,foot.Z)+down*.25f;var bottom=new Vector3(foot.X,foot.Y-.3f,foot.Z)+down*.45f;
        var st=new SurfaceTool();st.Begin(Godot.Mesh.PrimitiveType.Triangles);
        var rows=new[]{(lip,0f),(edge,.12f),(bottom,1f)};
        for(int r=0;r<2;r++)for(int i=0;i<8;i++)
        {
            void V(int row,int col){var (p,v)=rows[row];st.SetUV(new(col/8f,v));st.SetNormal(-down);st.AddVertex(p-across+across*2*col/8f);}
            V(r,i);V(r,i+1);V(r+1,i);V(r,i+1);V(r+1,i+1);V(r+1,i);
        }
        var material=new ShaderMaterial{Shader=GD.Load<Shader>("res://assets/shaders/waterfall.gdshader")};
        Mesh(this,$"{name} falls",st.Commit(),Vector3.Zero,material).CastShadow=GeometryInstance3D.ShadowCastingSetting.Off;
    }
    // The Cedar River's canyon is choked with boulders a few metres in: the water slips between them, walkers cannot.
    public static readonly Vector2 CanyonChoke=new(-70,-111);
    // The only invisible walls: one half a metre deep off the beach, from inside the north-west headland round to inside the south-east
    // one, and one across the canyon behind its boulder choke.
    public static List<(Vector2 A,Vector2 B)> Walls()
    {
        float Out(Vector2 from,Vector2 step){var p=from;for(int i=0;i<60;i++,p+=step)if(Height(p.X,p.Y)<WaterLevel-.5f)return (p-from).Length();return float.NaN;}
        var line=new List<Vector2>();float last=8;
        for(int z=-44;z<=SouthEdge;z+=4){float d=Out(new(-HalfWidth,z),Vector2.Left*.5f);if(!float.IsNaN(d))last=d;line.Add(new(-HalfWidth-last,z));}
        last=8;
        for(int x=-HalfWidth;x<=48;x+=4){float d=Out(new(x,SouthEdge),Vector2.Down*.5f);if(!float.IsNaN(d))last=d;line.Add(new(x,SouthEdge+last));}
        var walls=Enumerable.Range(0,line.Count-1).Select(i=>(line[i],line[i+1])).ToList();
        walls.Add((CanyonChoke+Vector2.Up*(GorgeFloor+GorgeWall+2),CanyonChoke+Vector2.Down*(GorgeFloor+GorgeWall+2)));
        return walls;
    }
}
