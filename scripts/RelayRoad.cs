using Godot;
namespace Hoshikawa;

// The Old Relay Road, from the stone bridge's north end to the viaduct: a block of mountain that Height() cuts trails into. The road
// climbs a switchback from the junction to a ruined teahouse on the cliff above it, runs north along the eastern slope high over the
// forest valley, and drops into the relay plaza under the viaduct; the forest valley leaves the junction for a dead end at an old quarry.
public partial class World
{
    // The relay road's ground runs from 12 m north of the river's middle to the viaduct, where Cedar Pass takes over.
    public const float RelaySouth=-14,RelayNorth=-178;
    // Each trail is a run of (x, ground level, z) points, the half width of level ground either side of them and the half width of the
    // road paint down their middle (0 for none). The junction's and the plaza's are wide floors; where two trails' floors meet at different
    // levels, the higher one ends at a cliff.
    public static readonly (string Name,float Half,float Paint,Vector3[] Points)[] Trails={
        ("Junction",12,0,new Vector3[]{new(0,.82f,-13),new(2,1.2f,-24),new(4,1.6f,-34)}),
        ("Switchback",3.5f,2,new Vector3[]{new(0,.82f,-13),new(6,1.1f,-21),new(14,1.3f,-25),new(44,3.4f,-34),new(48,3.7f,-38),new(46,4,-42),new(28,7.5f,-54)}),
        ("Teahouse terrace",8,0,new Vector3[]{new(6,7.5f,-55),new(22,7.5f,-58)}),
        ("High road",3.5f,2,new Vector3[]{new(28,7.5f,-54),new(27,7.6f,-62),new(28,8.2f,-78),new(28,8.8f,-98),new(22,9.4f,-118),new(16,7.5f,-130),new(8,4.5f,-140),new(5,3,-147),new(2,3,-156)}),
        ("Relay plaza",12,2,new Vector3[]{new(2,3,-156),new(1.5f,3,-174)}),
        ("Station road",3.5f,2,new Vector3[]{new(12,3,-160.5f),new(30,3.6f,-161),new(44,4.3f,-159.7f)}),
        ("Rockfall",3.5f,0,new Vector3[]{new(44,4.3f,-159.7f),new(51.5f,8.5f,-160)}),
        ("Forest valley",6,1.1f,new Vector3[]{new(3,1.2f,-24),new(-6,1.4f,-34),new(-14,2,-58),new(-4,2.6f,-80),new(16,3,-96),new(2,3.4f,-112),new(-14,3.8f,-122),new(-28,4.2f,-128)}),
        ("Old quarry",9,0,new Vector3[]{new(-28,4.2f,-128),new(-36,4.4f,-134)}),
    };
    // The distance from (x, z) to a trail's points and the ground level at its nearest point.
    public static (float Gap,float Level) Near(Vector3[] points,float x,float z)
    {
        (float Gap,float Level) best=(float.MaxValue,0);
        for(int i=0;i<points.Length-1;i++){var near=Near(points[i],points[i+1],x,z);if(near.Gap<best.Gap)best=near;}
        return best;
    }
    // The same for one reach, from a to b.
    public static (float Gap,float Level) Near(Vector3 a,Vector3 b,float x,float z)
    {
        Vector2 p=new(x,z),from=new(a.X,a.Z),to=new(b.X,b.Z);float t=Mathf.Clamp((p-from).Dot(to-from)/(to-from).LengthSquared(),0,1);
        return (p.DistanceTo(from.Lerp(to,t)),Mathf.Lerp(a.Y,b.Y,t));
    }
    // Every trail the carving rule cuts, the relay road's and the maze's, and each of their reaches with its floor's half width, built on
    // first use: the two tables live in two files, whose static fields initialize in no set order.
    public static IEnumerable<(string Name,float Half,float Paint,Vector3[] Points)> AllTrails=>Trails.Concat(MazeTrails);
    // Flat, since Height() runs the whole table at every point and the maze's 1 m ground has it run a few hundred thousand times.
    private readonly record struct Reach(float Ax,float Az,float Dx,float Dz,float Inverse,float Ay,float Dy,float Half,float Paint,bool Maze);
    private static Reach[]? cut;
    private static Reach[] Cut=>cut??=Trails.Select(t=>(Trail:t,Maze:false)).Concat(MazeTrails.Select(t=>(Trail:t,Maze:true))).SelectMany(p=>p.Trail.Points.Zip(p.Trail.Points.Skip(1),
        (a,b)=>new Reach(a.X,a.Z,b.X-a.X,b.Z-a.Z,1/((b.X-a.X)*(b.X-a.X)+(b.Z-a.Z)*(b.Z-a.Z)),a.Y,b.Y-a.Y,p.Trail.Half,p.Trail.Paint,p.Maze))).ToArray();
    // How far (x, z) lies from a reach's line, and the level at its nearest point.
    private static float Gap(in Reach r,float x,float z,out float level)
    {
        float px=x-r.Ax,pz=z-r.Az,t=Math.Clamp((px*r.Dx+pz*r.Dz)*r.Inverse,0,1),ex=px-t*r.Dx,ez=pz-t*r.Dz;level=r.Ay+t*r.Dy;return MathF.Sqrt(ex*ex+ez*ez);
    }
    // Each point takes the level of the trail reach whose floor it lies nearest outside of, blended over about half a metre where two
    // floors meet so that they meet at a cliff, and further into the mountain, so its ridges carry no crease; reach by reach, not trail
    // by trail, or the spur inside a bend would step where its nearest point jumps from one reach to the next. Beyond a floor's edge the
    // rock rises 5-7 m over 2.5 m, steeper than Mio's 50 degree floor limit over all but its foot and brow, then on into ridges.
    // The river's bank south of RelaySouth+2 and Cedar Pass north of RelayNorth+2 count as floors too, at their own ground's level there,
    // so the mountain rises from them in the same cliffs; both stop at the play area's east edge, where the maze's mountain begins.
    // With the ground, how far (x, z) lies outside the nearest floor, and outside the nearest of the maze's.
    private static (float Ground,float Edge,float Maze,float Relay) Carve(float x,float z)
    {
        var reaches=Cut;var list=Listed(x,z);int n=list.Length;Span<float> g=stackalloc float[n+2],l=stackalloc float[n+2];float relay=float.MaxValue,maze=float.MaxValue;
        for(int i=0;i<n;i++){ref readonly var r=ref reaches[list[i]];g[i]=Gap(r,x,z,out l[i])-r.Half;if(r.Maze)maze=Math.Min(maze,g[i]);else relay=Math.Min(relay,g[i]);}
        float east=x-HalfWidth,west=Math.Min(x,HalfWidth);
        g[n]=Math.Max(RelaySouth+2-z,east);l[n]=BaseHeight(west,Math.Max(z,RelaySouth+2));g[n+1]=Math.Max(z-RelayNorth-2,east);l[n+1]=BaseHeight(west,Math.Min(z,RelayNorth+2));
        relay=Math.Min(relay,Math.Min(g[n],g[n+1]));float edge=Math.Min(relay,maze);
        float soft=.3f+.3f*Math.Max(0,edge),weights=0,sum=0;
        for(int i=0;i<n+2;i++){float k=(g[i]-edge)/soft;if(k<Faint){float w=MathF.Exp(-k);weights+=w;sum+=w*l[i];}}
        float cliff=6+1.2f*Mathf.Sin(x*.13f+z*.07f)*Mathf.Sin(x*.05f-z*.11f);
        return (sum/weights+cliff*Mathf.SmoothStep(.3f,2.8f,edge)+Mathf.SmoothStep(4,40,edge)*(9+4*Mathf.Sin(x*.06f)*Mathf.Sin(z*.045f)),edge,maze,relay);
    }
    private static float RelayGround(float x,float z)=>Carve(x,z).Ground;
    // A reach more than 16 softenings further out than the nearest weighs under 1e-7 of it in the blend, and is left out. So for each 16 m
    // square Carve() is asked about, the reaches that can count anywhere in it: any that close in the blend, and any that can be the nearest
    // of all, of the maze's or of the relay road's. A gap changes by at most a square's half diagonal across it, so the lists give what the
    // whole table would, and Carve() runs a few dozen reaches instead of every one.
    private const float Faint=16;private const int Square=16;private static readonly Rect2I Squares=new(-112,-272,560,304);
    private static int[][]? listed;private static int[]? every;
    private static int[] Listed(float x,float z)
    {
        var reaches=Cut;every??=Enumerable.Range(0,reaches.Length).ToArray();
        if(!Squares.HasPoint(new(Mathf.FloorToInt(x),Mathf.FloorToInt(z))))return every;
        int w=Squares.Size.X/Square;
        listed??=Enumerable.Range(0,w*(Squares.Size.Y/Square)).Select(k=>
        {
            float cx=Squares.Position.X+(k%w+.5f)*Square,cz=Squares.Position.Y+(k/w+.5f)*Square,r=Square*.7072f;
            var d=reaches.Select(q=>Gap(q,cx,cz,out _)-q.Half).ToArray();
            float planes=Math.Min(Math.Max(RelaySouth+2-cz,cx-HalfWidth),Math.Max(cz-RelayNorth-2,cx-HalfWidth));
            float maze=d.Where((_,i)=>reaches[i].Maze).DefaultIfEmpty(float.MaxValue).Min()+r,relay=Math.Min(planes,d.Where((_,i)=>!reaches[i].Maze).DefaultIfEmpty(float.MaxValue).Min())+r;
            float edge=Math.Min(maze,relay),far=edge+Faint*(.3f+.3f*Math.Max(0,edge));
            return Enumerable.Range(0,d.Length).Where(i=>d[i]-r<(reaches[i].Maze?Math.Max(far,maze):Math.Max(far,relay))).ToArray();
        }).ToArray();
        return listed[(Mathf.FloorToInt(z)-Squares.Position.Y)/Square*w+(Mathf.FloorToInt(x)-Squares.Position.X)/Square];
    }
    // How far (x, z) lies from the middle of the painted road, less the paint's half width over 2 m: terrain.gdshader paints the road
    // from vertex UV2.y, fading it out between 2 and 3.3. Hoshikawa's street and the pass road follow RoadX(); the relay road's trails
    // take over under the stone bridge's north abutment and hand back to RoadX() under the viaduct's broken arch. East of the play area
    // only the maze's painted trails are painted.
    public static float RoadGap(float x,float z)
    {
        bool east=x>HalfWidth;
        if(!east&&(z>-13||z<RelayNorth+2))return Math.Abs(x-RoadX(z));
        float gap=!east&&z<RelayNorth+12?Math.Abs(x-RoadX(z)):9;
        foreach(ref readonly var r in Cut.AsSpan())if(r.Paint>0)gap=Math.Min(gap,Gap(r,x,z,out _)-r.Paint+2);
        return gap;
    }
    // How far (x, z) lies outside the nearest floor, the maze's, the river's bank and Cedar Pass included: under 0 on a floor, over 2.8 on
    // the mountain above its cliffs.
    public static float RelayEdge(float x,float z)=>Carve(x,z).Edge;
    // The moving parts, all in one table. A shortcut (a ladder, a stair or a gate) opens once, from its far side: Mio works it at From, and
    // its Body, turned Yaw about the vertical, swings from its Closed angle to its Open one, a ladder or stair about its own X axis, a gate
    // about the vertical; Game keeps which are open through defeats, until the prologue restarts. A trap (a rotten plank, or a rolling load of
    // rocks, logs or barrels) is sprung by a step, at its plank or within reach of To, the foot of a load's run from From, and a rest re-arms
    // it: Size is a plank's reach, or a load's radius, and Span half a load's length across its run.
    public record Part(string Name,string Kind,Node3D Body,Vector3 From,Vector3 To=default,float Closed=0,float Open=0,float Yaw=0,float Size=0,float Span=0)
    {
        public bool Shortcut=>Kind is "ladder" or "stair" or "gate";
        public bool Rolls=>Kind is "rocks" or "logs" or "barrels";
        public Transform3D Rest {get;}=Body.Transform;
        public void Swing(float angle)=>Body.Quaternion=new Quaternion(Vector3.Up,Yaw)*new Quaternion(Kind=="gate"?Vector3.Up:Vector3.Right,angle);
    }
    public readonly List<Part> Parts=new();
    // Items lying off the road, each an Items id; Game hides each one Mio takes until the prologue restarts.
    public record Pickup(string Item,Node3D Node);
    public readonly List<Pickup> Pickups=new();
    // The ruined teahouse on its terrace above the junction, in the Hamlet table's shape.
    public static readonly (string Name,Vector2 At,Vector3 Size,float Yaw,string Style) OldTeahouse=("Old Teahouse",new(12,-59.5f),new(8,4.03f,7),0,"teahouse");
    public Vector3 RelayPosition=>new(-6,Height(-6,-167),-167);
    // The station road's barrier and rockfall, models and bodies, and where the road runs out past them, the extension's way in.
    public readonly List<Node3D> Rockfall=new();
    // The signpost before the rockfall stays when it is cleared; only its "road closed" line goes.
    public const string StationSign="E · STATION",ClosedSign="E · STATION\nROCKFALL · ROAD CLOSED";
    public List<Label3D> RockfallSign=new();
    public void ClearRockfall(bool clear){foreach(var n in Rockfall){n.Visible=!clear;if(n is StaticBody3D body)body.CollisionLayer=clear?0u:1u;}foreach(var l in RockfallSign)l.Text=clear?StationSign:ClosedSign;}
    // The three patrols guard the junction, the teahouse terrace and the relay plaza. The scouts watch the forest valley, the quarry,
    // the switchback's hairpin and the high road. The drones hover over the switchback's foot, the forest valley and the high road,
    // each over 16 m from any patrol or scout, so Mio can take one on alone.
    public static readonly Vector2[] PatrolHomes={new(2,-30),new(21,-54),new(4,-160)},ScoutHomes={new(-10,-68),new(-32,-131),new(44,-36),new(28,-100)},
        DroneHomes={new(27,-27),new(-11,-50),new(2,-85),new(6,-107),new(26,-80)};
    private void RelayRoad()
    {
        // The relay road's own scatter, so nothing it places moves the countryside's or the valley's.
        var r=new RandomNumberGenerator{Seed=7207};var valley=Trails.Single(t=>t.Name=="Forest valley");
        float X()=>r.RandfRange(-60,60);float Z()=>r.RandfRange(RelayNorth+4,RelaySouth-4);
        // Keyaki and cedars along the brows above the trails' cliffs, each 3 m from the rest so no two trunks double up.
        var trees=new List<Vector2>();
        for(int i=0;i<200;i++)
        {
            float x=X(),z=Z(),s=r.RandfRange(.8f,1.4f);var (_,_,maze,edge)=Carve(x,z);
            // Chosen by the relay road's own trails, so the stream runs as before the maze; one that would stand on the maze's way in is
            // drawn and dropped.
            if(edge>3.5f&&edge<16&&trees.All(c=>c.DistanceTo(new(x,z))>3)){trees.Add(new(x,z));if(maze>3.5f)Tree(new(x,Height(x,z),z),s,"countryside",r);else for(int k=0;k<3;k++)r.Randf();}
        }
        // Cedars down the forest valley either side of its path, on its own floor and narrow enough that their boughs keep off its walls.
        for(int i=0;i<48;i++)
        {
            int k=r.RandiRange(0,valley.Points.Length-2);var a=valley.Points[k];var b=valley.Points[k+1];float t=r.Randf(),side=r.RandfRange(2.5f,3.6f)*(i%2*2-1),s=r.RandfRange(.8f,1.15f);
            var p=a.Lerp(b,t);var across=new Vector2(a.Z-b.Z,b.X-a.X).Normalized()*side;float x=p.X+across.X,z=p.Z+across.Y,path=Near(valley.Points,x,z).Gap;
            if(p.Z<-40&&path>2.4f&&RelayEdge(x,z)>=path-valley.Half-.01f&&trees.All(c=>c.DistanceTo(new(x,z))>3)){trees.Add(new(x,z));Tree(new(x,Height(x,z),z),s,"sugi",r);}
        }
        for(int i=0;i<420;i++)
        {
            float x=X(),z=Z(),s=r.RandfRange(.4f,1.3f),turn=r.Randf()*Mathf.Tau,edge=RelayEdge(x,z);
            if(edge>-1.2f&&edge<.4f&&RoadGap(x,z)>3.6f)Boulder("Relay boulder",x,z,s,turn);
        }
        bool Open(float x,float z)=>RelayEdge(x,z)<-.3f&&RoadGap(x,z)>3.4f;
        var grass=new List<(Vector3 P,float H)>();
        for(int i=0;i<6000;i++){float x=X(),z=Z(),h=r.RandfRange(.18f,.55f);if(Open(x,z))grass.Add((new(x,Height(x,z),z),h));}
        Grass("Relay road grass",grass,3,p=>Open(p.X,p.Y),5,.45f,new("3a5122"),new("8ea54f"),.1f,7211);
        for(int i=0;i<160;i++)
        {
            int k=r.RandiRange(0,valley.Points.Length-2);var a=valley.Points[k];var b=valley.Points[k+1];float t=r.Randf(),side=r.RandfRange(1.4f,3.2f)*(i%2*2-1);
            var p=a.Lerp(b,t);var across=new Vector2(a.Z-b.Z,b.X-a.X).Normalized()*side;float x=p.X+across.X,z=p.Z+across.Y;
            if(p.Z<-38)Sphere(this,"Valley wildflower",new(x,Height(x,z)+.16f,z),new(.11f,.11f,.11f),Mat(i%3==0?new("b8acd0"):new("e8d89b")));
        }
        // Lanterns up the switchback and along the high road, every 15 m on the right going north.
        foreach(var name in new[]{"Switchback","High road"})
        {
            var points=Trails.Single(t=>t.Name==name).Points;float next=name=="Switchback"?24:8,run=0;
            for(int i=0;i<points.Length-1;i++)
            {
                var a=new Vector2(points[i].X,points[i].Z);var b=new Vector2(points[i+1].X,points[i+1].Z);float length=a.DistanceTo(b);var d=(b-a)/length;
                for(;next<run+length;next+=15){var p=a+d*(next-run)+new Vector2(-d.Y,d.X)*2.9f;Lamp(new(p.X,Height(p.X,p.Y),p.Y));}
                run+=length;
            }
        }
        Signpost(new(-4,-21),"E · MANUAL RELAY\nN · OLD QUARRY");
        // The road east from the plaza beside the old line is the relay road extension's way in, toward the station: closed by a barrier and,
        // behind it, a rockfall slid down from the cutting's end, boulders resting on its talus, until the third morning (ClearRockfall).
        const float barrier=41;float road=-160;
        // Three Tripo barricades (assets/models/barricade.glb, 3 m long on its Z axis) across the road, each on the lowest ground under it.
        foreach(float z in new[]{road-3,road,road+3})
        {
            float ground=new[]{-1.5f,1.5f}.Min(dz=>Height(barrier,z+dz));
            var n=Model(this,"Station road barrier","barricade",new(barrier,ground,z),z==road?180:0,new(1.3f,1.3f,1.3f));
            var body=new StaticBody3D();n.AddChild(body);body.AddChild(new CollisionShape3D{Position=new(0,.65f,0),Shape=new BoxShape3D{Size=new(.4f,1.3f,3)}});
            Rockfall.Add(n);Rockfall.Add(body);
        }
        RockfallSign=Signpost(new(39,-158),ClosedSign,Mathf.Pi/2);
        foreach(var (x,z,size,turn) in new[]{(43.5f,-161.5f,1.3f,.4f),(44.5f,-158.2f,1.1f,1.9f),(46.5f,-160.4f,1.8f,.9f),(47.8f,-157.8f,1.4f,2.6f),(46,-163,1.2f,1.4f),(48.5f,-162,1.5f,.2f),
            (49.5f,-159.8f,2.4f,1.1f),(50.3f,-157.6f,2,2.2f),(50.4f,-162.6f,2.2f,.7f),(51.6f,-160.4f,2.6f,1.7f),(52.6f,-158.4f,1.8f,.3f),(52.8f,-162,2,2.9f),(54,-160.2f,2.2f,1.3f)})
            Boulder("Rockfall boulder",x,z,size,turn);
        Minka(OldTeahouse.Name,OldTeahouse.At,OldTeahouse.Size,OldTeahouse.Yaw,OldTeahouse.Style);
        TeahouseLadder();
        // The old quarry: cut blocks along its walls, the Tripo handcart left as its stone sled and, on a barrel at its far end, a remedy pouch.
        foreach(var (x,z,w,h,d,yaw) in new[]{(-38.5f,-127.5f,2.4f,1.6f,1.8f,.3f),(-42f,-140f,1.8f,1.2f,1.6f,-.4f),(-30f,-139f,2f,1f,1.4f,.9f),(-24f,-121f,1.6f,1.3f,1.2f,.2f),(-45f,-131.5f,1.2f,.9f,1.2f,1.1f)})
        {
            // Each rests on the lowest ground under its corners, sunk into the wall's foot where it leans on it rather than standing off it.
            float y=new[]{-.5f,.5f}.SelectMany(a=>new[]{-.5f,.5f},(a,b)=>Height(x+a*w,z+b*d)).Min();
            Box(this,"Quarry block",new(x,y+h/2-.1f,z),new(w,h,d),stone,true).Rotation=new(0,yaw,0);
        }
        Handcart("Quarry sled",new(-33,-138),90);
        Pouch(new(-40,-137),"Remedy pouch","Pouch barrel");
        // Two resonance cells, standing lit on the ground: one behind the terrace's patrol, one at the forest valley's eastern bend.
        foreach(var (x,z) in new[]{(19.5f,-62f),(16f,-96f)})Cell(new(x,z));
        // The Tripo cabinet on its plinth (assets/models/relay.glb) turned so its door faces the plaza, the dead screen lit behind its
        // window, and the mast behind it on the plinth.
        var relay=new Node3D {Name="Manual relay",Position=RelayPosition};AddChild(relay);
        Model(relay,"Relay cabinet","relay",Vector3.Zero,-90,new(1.3f,1.3f,1.3f));
        var cabinet=new StaticBody3D();relay.AddChild(cabinet);
        cabinet.AddChild(new CollisionShape3D{Position=new(0,.18f,0),Shape=new BoxShape3D{Size=new(1.87f,.36f,1.89f)}});
        cabinet.AddChild(new CollisionShape3D{Position=new(0,1.4f,-.02f),Shape=new BoxShape3D{Size=new(1.24f,2.1f,1.1f)}});
        Box(relay,"Offline terminal",new(0,1.71f,.43f),new(.36f,.32f,.04f),Mat(Jade,.8f));
        Cylinder(relay,"Antenna mast",new(0,3.73f,-.72f),.09f,6.75f,Mat(new("829b91")));
        for(int i=0;i<3;i++)Box(relay,"Antenna crossbar",new(0,5.5f+i*.55f,-.72f),new(3-i*.6f,.06f,.06f),Mat(new("829b91")));
        Label(relay,"MANUAL RELAY",new(0,3.15f,0),Jade,30);
        // The broken rail viaduct over the plaza, a reminder of the line the first chapter will reopen: its deck 10 m over the plaza, a
        // pillar wherever the ground lies under it, standing on the lowest ground under its footprint, and its span over the road missing.
        const float Viaduct=-168,deck=13;
        for(int i=0;i<9;i++)
        {
            float x=-38.5f+i*9,ground=new[]{-1.6f,1.6f}.SelectMany(dx=>new[]{-1.6f,1.6f},(dx,dz)=>Height(x+dx,Viaduct+dz)).Min();
            // The Tripo pier (assets/models/viaduct_pier.glb, 11 m, 3.7 m across its footing) slimmed under the deck and shortened to reach
            // just below the plaza, or sunk into the hillside where the ground stands higher.
            if(ground<deck-1.5f)
            {
                float tall=Mathf.Clamp((deck-.2f-ground)/11,.8f,1)*11;
                var pier=Model(this,"Viaduct pillar","viaduct_pier",new(x,deck-.5f-tall,Viaduct),0,new(1.3f,1.3f,1.3f));pier.Scale=new(.85f,tall/11,.85f);
                var body=new StaticBody3D{Position=new(x,deck-.5f-tall/2,Viaduct)};AddChild(body);body.AddChild(new CollisionShape3D{Shape=new BoxShape3D{Size=new(2.6f,tall,2.6f)}});
            }
            // The Tripo deck span (assets/models/viaduct_span.glb, 11.8 m by 4.6 m, rails on its X axis) fitted to the 9 m bay, its
            // ballast and parapets 1.4 m deep over the piers.
            if(i!=4&&i<8)Model(this,"Viaduct span","viaduct_span",new(x+4.5f,deck-.5f,Viaduct),0,new(1.3f,1.3f,1.3f)).Scale=new(9/11.84f,1,3.6f/4.62f);
        }
    }
    // The Tripo boulder (assets/models/boulder.glb, 1.58 m across), size metres across. Its flat base rests on the lowest ground under
    // its footprint, sunk a little, so no air shows under it on a slope. Scatter boulders stay walk-through, as their spheres were.
    private void Boulder(string name,float x,float z,float size,float turn,bool solid=true)
    {
        float h=size/2,ground=new[]{-h,h}.SelectMany(dx=>new[]{-h,h},(dx,dz)=>Height(x+dx,z+dz)).Min();
        var n=Model(this,name,"boulder",new(x,ground-size*.05f,z),Mathf.RadToDeg(turn),new(1.05f,1.05f,1.05f));n.Scale=Vector3.One*size/1.58f;
        ActorVisual.Descendants(n).OfType<MeshInstance3D>().Single().VisibilityRangeEnd=80;if(name=="Rockfall boulder")Rockfall.Add(n);
        if(!solid)return;
        var body=new StaticBody3D{Position=n.Position+Vector3.Up*size*.35f};AddChild(body);body.AddChild(new CollisionShape3D{Shape=new CylinderShape3D{Radius=size*.42f,Height=size*.7f}});
        if(name=="Rockfall boulder")Rockfall.Add(body);
    }
    // The teahouse's ladder, drawn up on its terrace's brow above the junction until Mio lowers it from there: a steep ship's ladder,
    // under 40 degrees so she walks it.
    private void TeahouseLadder()
    {
        float z=-52;while(Height(4,z+.1f)>7.4f)z+=.1f;
        var top=new Vector3(4,Height(4,z),z);var foot=new Vector3(4,0,z+7.6f);foot.Y=Height(foot.X,foot.Z);
        // Drawn up, it lies back over the terrace on a trestle, so it is lowered from beside its hinge and swings up over it and down.
        const float back=.3f,rest=4;var on=top+new Vector3(0,rest*Mathf.Sin(back)-.17f,-rest*Mathf.Cos(back));float ground=Height(on.X,on.Z);
        foreach(float x in new[]{-.95f,.95f})Box(this,"Ladder trestle leg",new(on.X+x,(on.Y+ground)/2,on.Z),new(.12f,on.Y-ground,.12f),wood,true);
        Box(this,"Ladder trestle bar",new(on.X,on.Y-.06f,on.Z),new(2.1f,.12f,.14f),wood);
        Stair("Teahouse ladder","ladder",top,foot,back-Mathf.Pi,top+new Vector3(1.8f,0,-1));
    }
    // A ladder or stair let down from a brow at `top` to `foot`, worked at `from` beside its hinge, standing at `closed` about it until then.
    // Hinged just under the brow and with no end standing proud of it, so she steps on and off it at either end without a ledge.
    private void Stair(string name,string kind,Vector3 top,Vector3 foot,float closed,Vector3 from)
    {
        var d=foot-top;float length=d.Length();
        var part=new AnimatableBody3D{Name=name,Position=top+Vector3.Down*.1f,CollisionMask=0};AddChild(part);
        part.AddChild(new CollisionShape3D{Position=new(0,-.08f,length/2+.1f),Shape=new BoxShape3D{Size=new(1.5f,.16f,length+.2f)}});
        // The Tripo ladder (7.84 m, hinge brackets at its head) fitted to the walk box: its rung tops 2 cm above it and its rails to the box's sides.
        if(kind=="ladder")Model(part,"Ladder","ladder",new(0,-.15f,length/2),-90,new(1.3f,1.3f,1.3f)).Scale=new(length/7.84f,.8f,1.15f);
        else
        {
            // The Tripo stair: its treads' line runs 12.4 m from (0, 6, -6.25) at its hinged head down to (0, 0, 4.6), 29 degrees, so turned
            // that line lies along the walk box, scaled to its length, its treads 2 cm above it and narrowed to 2 m over its handrail.
            var model=Model(part,"Stair","bell_stair",Vector3.Zero,0,new(1.3f,1.3f,1.3f));float s=length/12.4f;var turn=new Basis(Vector3.Right,Mathf.DegToRad(-28.95f));
            model.Basis=turn*Basis.FromScale(new(.42f,s,s));model.Position=Vector3.Up*.02f-turn*new Vector3(0,6*s,-6.25f*s);
        }
        var stair=new Part(name,kind,part,from,foot,closed,Mathf.Atan2(-d.Y,new Vector2(d.X,d.Z).Length()),Mathf.Atan2(d.X,d.Z));stair.Swing(stair.Closed);Parts.Add(stair);
    }
    // A remedy pouch on a barrel's lid, and a resonance cell standing lit on the ground, each one of the Pickups.
    private void Pouch(Vector2 at,string name,string barrelName)
    {
        var pouch=new Node3D{Name=name,Position=new(at.X,Height(at.X,at.Y),at.Y)};AddChild(pouch);
        Barrel(this,barrelName,pouch.Position,.6f);
        var barrel=new StaticBody3D{Position=pouch.Position+new Vector3(0,.42f,0)};AddChild(barrel);barrel.AddChild(new CollisionShape3D{Shape=new CylinderShape3D{Radius=.32f,Height=.84f}});
        pouch.AddChild(Combined("Pouch",Mat(new("c9a25a"),.25f),(new(0,.96f,0),new(.34f,.24f,.26f)),(new(0,1.12f,0),new(.14f,.1f,.12f))));Pickups.Add(new("pouch",pouch));
    }
    private void Cell(Vector2 at)
    {
        var cell=new Node3D{Name="Resonance cell",Position=new(at.X,Height(at.X,at.Y),at.Y)};AddChild(cell);
        cell.AddChild(Combined("Cell",Mat(Jade,.9f),(new(0,.14f,0),new(.13f,.28f,.13f)),(new(0,.31f,0),new(.07f,.06f,.07f))));Pickups.Add(new("cell",cell));
    }
    // Boxes baked into one mesh that Merge() leaves alone, for parts that move or vanish.
    private static MeshInstance3D Combined(string name,Material material,params (Vector3 At,Vector3 Size)[] boxes)
    {
        var st=new SurfaceTool();st.Begin(Godot.Mesh.PrimitiveType.Triangles);
        foreach(var (at,size) in boxes)st.AppendFrom(new BoxMesh{Size=size},0,new Transform3D(Basis.Identity,at));
        return new MeshInstance3D{Name=name,Mesh=st.Commit(),MaterialOverride=material};
    }
}
