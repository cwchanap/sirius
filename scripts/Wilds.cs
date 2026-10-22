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
    // points a metre apart. Each course meets a play-area river at its edge with that river's level, or the sea at the sea's.
    // The Hoshikawa and the Cedar River spring from gorges in the eastern mountains and fall into the valley; the Cedar River
    // leaves it again through a canyon in the western range and reaches the sea north of the Hoshikawa's mouth.
    public static readonly (string Name,Vector3[] Course)[] Rivers={
        ("Hoshikawa headwater",new Vector3[]{new(97,7.4f,-28),new(89,6.3f,-15),new(80,5.62f,-8),new(79,WaterLevel,-8),new(HalfWidth,WaterLevel,-8)}),
        ("Cedar headwater",new Vector3[]{new(95,7f,-134),new(88,6.2f,-122),new(80,5.7f,-111),new(79,CedarLevel,-111),new(HalfWidth,CedarLevel,-111)}),
        ("Cedar outlet",new Vector3[]{new(-HalfWidth,CedarLevel,-111),new(-74,.62f,-111),new(-82,.5f,-100),new(-86,.3f,-76),new(-86,0,-52),new(-83,WaterLevel,-34),new(-80,WaterLevel,-12)}),
    };
    // Where the river's floor runs level, either side of its course; the gorge's walls climb from there to the mountain 4.5 m further out.
    private const float GorgeFloor=3.5f,GorgeWall=4.5f;
    // 1 over the south-west sea, 0 over the mountains: west of the play area south of the headland at the Hoshikawa's mouth, and south of it
    // west of the south-east headland; each coast wanders.
    // The headlands' sea cliffs face it over 6 m.
    public static float Sea(float x,float z)=>Math.Max(x<-HalfWidth?Mathf.SmoothStep(-26,-20,z+6*Mathf.Sin(x*.05f)):0,z>SouthEdge?1-Mathf.SmoothStep(20,26,x+8*Mathf.Sin(z*.06f)):0);
    // Outside the play area the ground carries on from its nearest edge point: up a cliff into the mountains, down a beach under the sea,
    // or, along a river's course, through its gorge, whose floor eases over 6 m from the edge's own river bed to 80 cm under the course's
    // level, falls included. In the sea a gorge only ever deepens the seabed.
    private static float Outside(float x,float z)
    {
        float cx=Mathf.Clamp(x,-HalfWidth,HalfWidth),cz=Mathf.Clamp(z,NorthEdge,SouthEdge),d=new Vector2(x-cx,z-cz).Length(),sea=Sea(x,z),edge=Inside(cx,cz);
        // The beach starts down to the sea up to 6 m out from the edge, so the coast wanders instead of following it.
        float shore=3+3*Mathf.Sin(x*.07f+z*.05f)*Mathf.Sin(x*.031f-z*.083f);
        float y=edge+(1-sea)*Mountain(x,z,d)-sea*(3*Mathf.SmoothStep(shore,shore+8,d)+.08f*Math.Max(0,d-shore-8));
        foreach(var (_,course) in Rivers)
        {
            var (gap,level,_)=Along(course,x,z);
            if(gap>=GorgeFloor+GorgeWall)continue;
            float gorge=Mathf.Lerp(y,Mathf.Lerp(edge,level-.8f,Mathf.SmoothStep(0,6,d)),1-Mathf.SmoothStep(GorgeFloor,GorgeFloor+GorgeWall,gap));
            y=sea>0?Math.Min(y,gorge):gorge;
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
    // The distance from (x, z) to a course, the water level at its nearest point, and that point's segment.
    public static (float Gap,float Level,int Segment) Along(Vector3[] course,float x,float z)
    {
        float best=float.MaxValue,level=0;int segment=0;var p=new Vector2(x,z);
        for(int i=0;i<course.Length-1;i++)
        {
            Vector2 a=new(course[i].X,course[i].Z),b=new(course[i+1].X,course[i+1].Z);
            float t=Mathf.Clamp((p-a).Dot(b-a)/(b-a).LengthSquared(),0,1),gap=p.DistanceTo(a.Lerp(b,t));
            if(gap<best){best=gap;level=Mathf.Lerp(course[i].Y,course[i+1].Y,t);segment=i;}
        }
        return (best,level,segment);
    }
    // The water level beside (x, z) outside the play area, if any: the sea's, or a river's within its gorge.
    private static float? WaterOutside(float x,float z)
    {
        if(Sea(x,z)>0)return WaterLevel;
        foreach(var (_,course) in Rivers){var (gap,level,_)=Along(course,x,z);if(gap<GorgeFloor+GorgeWall)return level;}
        return null;
    }
    private void Wilds()
    {
        var sea=WaterMaterial(Vector2.Zero);
        bool Open(Vector2 c)=>!InPlay(c.X,c.Y)&&Sea(c.X,c.Y)>0;
        WaterGrid("Sea",Fine,1,Open,(_,_)=>WaterLevel,sea);
        WaterGrid("Open sea",Far,4,c=>Open(c)&&!Fine.HasPoint((Vector2I)c.Floor()),(_,_)=>WaterLevel,sea);
        // Past the 4 m mountains the sea runs on under the fog to the horizon.
        Mesh(this,"Sea to the horizon",new PlaneMesh{Size=new(720,960),SubdivideWidth=89},new(Far.Position.X-360,WaterLevel,-40+480),sea);
        Mesh(this,"Sea to the south horizon",new PlaneMesh{Size=new(220,720),SubdivideWidth=54},new(Far.Position.X+110,WaterLevel,Far.End.Y+360),sea);
        foreach(var (name,course) in Rivers)
        {
            var box=new Rect2(course[0].X,course[0].Z,0,0);foreach(var p in course)box=box.Expand(new(p.X,p.Z));box=box.Grow(GorgeFloor+GorgeWall+1);
            var area=new Rect2I((Vector2I)box.Position.Floor(),(Vector2I)box.Size.Ceil()+Vector2I.One).Intersection(Fine);
            // One mesh per straight reach, so each drifts its own way; neighbours share their edge vertices.
            for(int i=0;i<course.Length-1;i++)
            {
                if(course[i].Y-course[i+1].Y>1){Waterfall(name,course[i],course[i+1]);continue;}
                int segment=i;var flow=new Vector2(course[i+1].X-course[i].X,course[i+1].Z-course[i].Z).Normalized()*.35f;
                WaterGrid($"{name} {i}",area,1,c=>!InPlay(c.X,c.Y)&&Sea(c.X,c.Y)<=0&&Along(course,c.X,c.Y) is var a&&a.Gap<GorgeFloor+GorgeWall&&a.Segment==segment,
                    (x,z)=>Along(course,x,z).Level,WaterMaterial(flow));
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
    // Water over the terrain's own grid and diagonals: a cell `owns` admits is wet where a corner's ground lies under the water's level
    // there + 4 cm, so wherever the water meets dry cells its edge runs along the terrain mesh's chords, buried deeper than the shader's wave.
    // Cells across a fall, whose corners' levels differ, stay dry: the fall's curtain covers them.
    private void WaterGrid(string name,Rect2I area,int step,Func<Vector2,bool> owns,Func<float,float,float> level,Material material)
    {
        var st=new SurfaceTool();st.Begin(Godot.Mesh.PrimitiveType.Triangles);int cells=0;
        void V(int x,int z){st.SetNormal(Vector3.Up);st.AddVertex(new(x,level(x,z),z));}
        for(int z=area.Position.Y;z<area.End.Y;z+=step)for(int x=area.Position.X;x<area.End.X;x+=step)
        {
            if(!owns(new(x+step/2f,z+step/2f)))continue;
            var corners=new[]{(x,z),(x+step,z),(x,z+step),(x+step,z+step)};var levels=corners.Select(c=>level(c.Item1,c.Item2)).ToArray();
            if(levels.Max()-levels.Min()>.5f||corners.Select((c,i)=>Height(c.Item1,c.Item2)-levels[i]).Min()>=.04f)continue;
            V(x,z);V(x+step,z);V(x,z+step);V(x+step,z);V(x+step,z+step);V(x,z+step);cells++;
        }
        if(cells==0)return;
        st.Index();Mesh(this,name,st.Commit(),Vector3.Zero,material);
    }
    // A fall from `top` down to `foot`, a metre downstream: a curtain over the gorge floor's width, lipping over from the upper water and
    // dropping in front of the rock face to 30 cm under the pool.
    private void Waterfall(string name,Vector3 top,Vector3 foot)
    {
        var flow=new Vector2(foot.X-top.X,foot.Z-top.Z).Normalized();var across=new Vector3(-flow.Y,0,flow.X)*(GorgeFloor+.6f);var down=new Vector3(flow.X,0,flow.Y);
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
