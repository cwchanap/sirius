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
        ("Station road",3.5f,2,new Vector3[]{new(12,3,-160.5f),new(30,3.6f,-161),new(46,4.4f,-159.5f)}),
        ("Forest valley",6,1.1f,new Vector3[]{new(3,1.2f,-24),new(-6,1.4f,-34),new(-14,2,-58),new(-4,2.6f,-80),new(16,3,-96),new(2,3.4f,-112),new(-14,3.8f,-122),new(-28,4.2f,-128)}),
        ("Old quarry",9,0,new Vector3[]{new(-28,4.2f,-128),new(-36,4.4f,-134)}),
    };
    private static readonly int Reaches=Trails.Sum(t=>t.Points.Length-1);
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
    // Each point takes the level of the trail reach whose floor it lies nearest outside of, blended over about half a metre where two
    // floors meet so that they meet at a cliff, and further into the mountain, so its ridges carry no crease; reach by reach, not trail
    // by trail, or the spur inside a bend would step where its nearest point jumps from one reach to the next. Beyond a floor's edge the
    // rock rises 5-7 m over 2.5 m, steeper than Mio's 50 degree floor limit over all but its foot and brow, then on into ridges.
    // The river's bank south of RelaySouth+2 and Cedar Pass north of RelayNorth+2 count as floors too, at their own ground's level there,
    // so the mountain rises from them in the same cliffs.
    private static float RelayGround(float x,float z)
    {
        int n=0;Span<float> g=stackalloc float[Reaches+2],l=stackalloc float[Reaches+2];
        foreach(var (_,half,_,points) in Trails)for(int i=0;i<points.Length-1;i++){var (gap,level)=Near(points[i],points[i+1],x,z);g[n]=gap-half;l[n++]=level;}
        g[n]=RelaySouth+2-z;l[n]=BaseHeight(x,Math.Max(z,RelaySouth+2));g[n+1]=z-RelayNorth-2;l[n+1]=BaseHeight(x,Math.Min(z,RelayNorth+2));
        float edge=float.MaxValue;foreach(float e in g)edge=Math.Min(edge,e);
        float soft=.3f+.3f*Math.Max(0,edge),weights=0,sum=0;
        for(int i=0;i<n+2;i++){float w=Mathf.Exp(-(g[i]-edge)/soft);weights+=w;sum+=w*l[i];}
        float cliff=6+1.2f*Mathf.Sin(x*.13f+z*.07f)*Mathf.Sin(x*.05f-z*.11f);
        return sum/weights+cliff*Mathf.SmoothStep(.3f,2.8f,edge)+Mathf.SmoothStep(4,40,edge)*(9+4*Mathf.Sin(x*.06f)*Mathf.Sin(z*.045f));
    }
    // How far (x, z) lies from the middle of the painted road, less the paint's half width over 2 m: terrain.gdshader paints the road
    // from vertex UV2.y, fading it out between 2 and 3.3. Hoshikawa's street and the pass road follow RoadX(); the relay road's trails
    // take over under the stone bridge's north abutment and hand back to RoadX() under the viaduct's broken arch.
    public static float RoadGap(float x,float z)
    {
        if(z>-13||z<RelayNorth+2)return Math.Abs(x-RoadX(z));
        float gap=z<RelayNorth+12?Math.Abs(x-RoadX(z)):9;
        foreach(var (_,_,paint,points) in Trails)if(paint>0)gap=Math.Min(gap,Near(points,x,z).Gap-paint+2);
        return gap;
    }
    // How far (x, z) lies outside the nearest floor, the river's bank and Cedar Pass included: under 0 on a floor, over 2.8 on the
    // mountain above its cliffs.
    public static float RelayEdge(float x,float z)
    {
        float edge=Math.Min(RelaySouth+2-z,z-RelayNorth-2);
        foreach(var (_,half,_,points) in Trails)edge=Math.Min(edge,Near(points,x,z).Gap-half);
        return edge;
    }
    // A shortcut opens once, from its far side: Mio works it at From, and Part swings about its own X axis from its Closed angle to its
    // Open one. Game keeps which are open through deaths, until the prologue restarts.
    public record Shortcut(string Name,AnimatableBody3D Part,Vector3 From,float Closed,float Open){public void Swing(float angle)=>Part.Quaternion=new(Vector3.Right,angle);}
    public readonly List<Shortcut> Shortcuts=new();
    // Remedy pouches lying off the road; Game hides each one Mio takes until the prologue restarts.
    public readonly List<Node3D> Pouches=new();
    // The ruined teahouse on its terrace above the junction, in the Hamlet table's shape.
    public static readonly (string Name,Vector2 At,Vector3 Size,float Yaw,string Style) OldTeahouse=("Old Teahouse",new(12,-59.5f),new(8,4.03f,7),0,"teahouse");
    public Vector3 RelayPosition=>new(-6,Height(-6,-167),-167);
    // The three patrols guard the junction, the teahouse terrace and the relay plaza. The scouts watch the forest valley, the quarry,
    // the switchback's hairpin and the high road.
    public static readonly Vector2[] PatrolHomes={new(2,-30),new(21,-54),new(4,-160)},ScoutHomes={new(-10,-68),new(-32,-131),new(44,-36),new(28,-100)};
    private void RelayRoad()
    {
        // The relay road's own scatter, so nothing it places moves the countryside's or the valley's.
        var r=new RandomNumberGenerator{Seed=7207};var valley=Trails.Single(t=>t.Name=="Forest valley");
        float X()=>r.RandfRange(-60,60);float Z()=>r.RandfRange(RelayNorth+4,RelaySouth-4);
        // Keyaki and cedars along the brows above the trails' cliffs.
        for(int i=0;i<200;i++)
        {
            float x=X(),z=Z(),s=r.RandfRange(.8f,1.4f),edge=RelayEdge(x,z);
            if(edge>3.5f&&edge<16)Tree(new(x,Height(x,z),z),s,"countryside",r);
        }
        // Cedars down the forest valley either side of its path, on its own floor and narrow enough that their boughs keep off its walls.
        var cedars=new List<Vector2>();
        for(int i=0;i<48;i++)
        {
            int k=r.RandiRange(0,valley.Points.Length-2);var a=valley.Points[k];var b=valley.Points[k+1];float t=r.Randf(),side=r.RandfRange(2.5f,3.6f)*(i%2*2-1),s=r.RandfRange(.8f,1.15f);
            var p=a.Lerp(b,t);var across=new Vector2(a.Z-b.Z,b.X-a.X).Normalized()*side;float x=p.X+across.X,z=p.Z+across.Y,path=Near(valley.Points,x,z).Gap;
            if(p.Z<-40&&path>2.4f&&RelayEdge(x,z)>=path-valley.Half-.01f&&cedars.All(c=>c.DistanceTo(new(x,z))>3)){cedars.Add(new(x,z));Tree(new(x,Height(x,z),z),s,"sugi",r);}
        }
        for(int i=0;i<420;i++)
        {
            float x=X(),z=Z(),s=r.RandfRange(.4f,1.3f),turn=r.Randf()*Mathf.Tau,edge=RelayEdge(x,z);
            if(edge>-1.2f&&edge<.4f&&RoadGap(x,z)>3.6f)Sphere(this,"Relay boulder",new(x,Height(x,z)+s*.3f,z),new(s,s*.65f,s*.8f),stone).Rotation=new(0,turn,0);
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
        // The road east from the plaza beside the old line is the relay road extension's way in, toward the station: closed for the
        // prologue by a barrier and the rockfall behind it at the end of its cutting.
        const float barrier=41;float road=-160;
        foreach(float z in new[]{road-4,road,road+4})Box(this,"Station road barrier post",new(barrier,Height(barrier,z)+.75f,z),new(.18f,1.5f,.18f),wood,true);
        foreach(float y in new[]{.6f,1.25f})Box(this,"Station road barrier rail",new(barrier,Height(barrier,road)+y,road),new(.1f,.14f,8.6f),wood,true);
        Signpost(new(39,-158),"E · STATION\nROCKFALL · ROAD CLOSED",Mathf.Pi/2);
        foreach(var (x,z,size,turn) in new[]{(43.5f,-161.5f,1.3f,.4f),(44.5f,-158.2f,1.1f,1.9f),(46.5f,-160.4f,1.8f,.9f),(47.8f,-157.8f,1.4f,2.6f),(46,-163,1.2f,1.4f),(48.5f,-162,1.5f,.2f),
            (49.5f,-159.8f,2.4f,1.1f),(50.3f,-157.6f,2,2.2f),(50.4f,-162.6f,2.2f,.7f),(51.6f,-160.4f,2.6f,1.7f),(52.6f,-158.4f,1.8f,.3f),(52.8f,-162,2,2.9f),(54,-160.2f,2.2f,1.3f)})
            Sphere(this,"Rockfall boulder",new(x,Height(x,z)+size*.35f,z),new(size,size*.75f,size*.9f),stone).Rotation=new(0,turn,0);
        Minka(OldTeahouse.Name,OldTeahouse.At,OldTeahouse.Size,OldTeahouse.Yaw,OldTeahouse.Style);
        TeahouseLadder();
        // The old quarry: cut blocks along its walls, a timber sled and, on a crate at its far end, a remedy pouch.
        foreach(var (x,z,w,h,d,yaw) in new[]{(-38.5f,-127.5f,2.4f,1.6f,1.8f,.3f),(-42f,-140f,1.8f,1.2f,1.6f,-.4f),(-30f,-139f,2f,1f,1.4f,.9f),(-24f,-121f,1.6f,1.3f,1.2f,.2f),(-45f,-131.5f,1.2f,.9f,1.2f,1.1f)})
        {
            // Each rests on the lowest ground under its corners, sunk into the wall's foot where it leans on it rather than standing off it.
            float y=new[]{-.5f,.5f}.SelectMany(a=>new[]{-.5f,.5f},(a,b)=>Height(x+a*w,z+b*d)).Min();
            Box(this,"Quarry block",new(x,y+h/2-.1f,z),new(w,h,d),stone,true).Rotation=new(0,yaw,0);
        }
        foreach(float x in new[]{-.5f,.5f})Box(this,"Quarry sled runner",new(-33+x,Height(-33,-138)+.1f,-138),new(.12f,.2f,2.6f),wood);
        Box(this,"Quarry sled deck",new(-33,Height(-33,-138)+.25f,-138),new(1.3f,.1f,2),wood,true);
        var pouch=new Node3D{Name="Remedy pouch",Position=new(-40,Height(-40,-137),-137)};AddChild(pouch);
        Box(this,"Pouch crate",pouch.Position+new Vector3(0,.3f,0),new(.8f,.6f,.6f),wood,true);
        pouch.AddChild(Combined("Pouch",Mat(new("c9a25a"),.25f),(new(0,.72f,0),new(.34f,.24f,.26f)),(new(0,.88f,0),new(.14f,.1f,.12f))));Pouches.Add(pouch);
        var relay=new Node3D {Name="Manual relay",Position=RelayPosition};AddChild(relay);
        Box(relay,"Relay platform",new(0,.15f,0),new(5,.3f,4),stone,true);
        Box(relay,"Relay cabinet",new(0,1.3f,0),new(1.5f,2.3f,.8f),Mat(new("607e76")),true);
        Box(relay,"Relay face",new(0,1.55f,.42f),new(1.2f,.8f,.04f),Mat(Ink));
        Box(relay,"Offline terminal",new(0,1.55f,.45f),new(.8f,.46f,.025f),Mat(Jade,.8f));
        Cylinder(relay,"Antenna mast",new(0,4.1f,-.4f),.09f,6,Mat(new("829b91")));
        for(int i=0;i<3;i++)Box(relay,"Antenna crossbar",new(0,5.5f+i*.55f,-.4f),new(3-i*.6f,.06f,.06f),Mat(new("829b91")));
        Label(relay,"MANUAL RELAY",new(0,3.15f,0),Jade,30);
        // The broken rail viaduct over the plaza, a reminder of the line the first chapter will reopen: its deck 10 m over the plaza, a
        // pillar wherever the ground lies under it, standing on the lowest ground under its footprint, and its span over the road missing.
        const float Viaduct=-168,deck=13;
        for(int i=0;i<9;i++)
        {
            float x=-38.5f+i*9,ground=new[]{-1f,1f}.SelectMany(dx=>new[]{-1.25f,1.25f},(dx,dz)=>Height(x+dx,Viaduct+dz)).Min();
            if(ground<deck-1.5f)Box(this,"Viaduct pillar",new(x,(ground-.5f+deck-.5f)/2,Viaduct),new(2,deck-ground,2.5f),stone);
            if(i!=4&&i<8)Box(this,"Viaduct span",new(x+4.5f,deck,Viaduct),new(9,1,3.6f),stone);
        }
    }
    // The teahouse's ladder, drawn up on its terrace's brow above the junction until Mio lowers it from there: a steep ship's ladder,
    // under 40 degrees so she walks it.
    private void TeahouseLadder()
    {
        float z=-52;while(Height(4,z+.1f)>7.4f)z+=.1f;
        var top=new Vector3(4,Height(4,z),z);var foot=new Vector3(4,0,z+7.6f);foot.Y=Height(foot.X,foot.Z);var d=foot-top;float length=d.Length();
        // Hinged just under the brow and with no end standing proud of it, so she steps on and off it at either end without a ledge.
        var part=new AnimatableBody3D{Name="Teahouse ladder",Position=top+Vector3.Down*.1f,CollisionMask=0};AddChild(part);
        part.AddChild(new CollisionShape3D{Position=new(0,-.08f,length/2+.1f),Shape=new BoxShape3D{Size=new(1.5f,.16f,length+.2f)}});
        var boxes=new List<(Vector3,Vector3)>{(new(-.7f,-.05f,length/2),new(.12f,.24f,length)),(new(.7f,-.05f,length/2),new(.12f,.24f,length))};
        for(float t=.3f;t<length;t+=.32f)boxes.Add((new(0,.02f,t),new(1.3f,.05f,.2f)));
        part.AddChild(Combined("Ladder",wood,boxes.ToArray()));
        // Drawn up, it lies back over the terrace on a trestle, so it is lowered from beside its hinge and swings up over it and down.
        const float back=.3f,rest=4;var on=part.Position+new Vector3(0,rest*Mathf.Sin(back)-.07f,-rest*Mathf.Cos(back));float ground=Height(on.X,on.Z);
        foreach(float x in new[]{-.95f,.95f})Box(this,"Ladder trestle leg",new(on.X+x,(on.Y+ground)/2,on.Z),new(.12f,on.Y-ground,.12f),wood,true);
        Box(this,"Ladder trestle bar",new(on.X,on.Y-.06f,on.Z),new(2.1f,.12f,.14f),wood);
        var shortcut=new Shortcut("Teahouse ladder",part,top+new Vector3(1.8f,0,-1),back-Mathf.Pi,Mathf.Atan2(-d.Y,d.Z));
        shortcut.Swing(shortcut.Closed);Shortcuts.Add(shortcut);
    }
    // Boxes baked into one mesh that Merge() leaves alone, for parts that move or vanish.
    private static MeshInstance3D Combined(string name,Material material,params (Vector3 At,Vector3 Size)[] boxes)
    {
        var st=new SurfaceTool();st.Begin(Godot.Mesh.PrimitiveType.Triangles);
        foreach(var (at,size) in boxes)st.AppendFrom(new BoxMesh{Size=size},0,new Transform3D(Basis.Identity,at));
        return new MeshInstance3D{Name=name,Mesh=st.Commit(),MaterialOverride=material};
    }
}
