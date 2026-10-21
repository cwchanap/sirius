using Godot;
namespace Hoshikawa;

// Hoshikawa's landforms and landmarks; every placement reads World.Height().
public partial class World
{
    private void Brook()
    {
        var st=new SurfaceTool();st.Begin(Godot.Mesh.PrimitiveType.Triangles);
        // Half-width 2.1 buries the strip's edges under the 1 m mesh chords of the bank instead of leaving them hovering over it.
        void V(float x,float z){st.SetNormal(Vector3.Up);st.SetUV(new(x*.2f,z*.2f));st.AddVertex(new(x,WaterLevel,z));}
        // Starts flush with the river sheet's south edge (z -4): overlapping it z-fights, a gap shows as a dark seam.
        // The shader's wave is per vertex, so the mouth row reuses the sheet's own edge vertices; any other spacing opens a step at the joint.
        var river=(PlaneMesh)GetNode<MeshInstance3D>("Hoshikawa river").Mesh;float cell=river.Size.X/(river.SubdivideWidth+1),l=BrookX(-3)-2.1f,r=BrookX(-3)+2.1f;
        float edge=Mathf.Floor((BrookX(-4)-2.1f+river.Size.X/2)/cell)*cell-river.Size.X/2;
        for(;edge<BrookX(-4)+2.1f;edge+=cell){V(l,-3);V(edge,-4);V(edge+cell,-4);}
        V(l,-3);V(edge,-4);V(r,-3);
        for(float z=-3;z<56;z++)
        {
            float a=BrookX(z),b=BrookX(z+1);
            V(a-2.1f,z);V(b-2.1f,z+1);V(a+2.1f,z);V(a+2.1f,z);V(b-2.1f,z+1);V(b+2.1f,z+1);
        }
        var water=new ShaderMaterial{Shader=GD.Load<Shader>("res://assets/shaders/water.gdshader")};water.SetShaderParameter("flow",new Vector2(0,-BrookFlow));
        Mesh(this,"Hoshikawa brook",st.Commit(),Vector3.Zero,water);
        for(int i=0;i<6;i++)
        {
            float z=55.6f+i%3*.7f,x=BrookX(z)+(i-2.5f)*.55f,s=.35f+.12f*(i%3);
            Sphere(this,"Spring stone",new(x,Height(x,z)+s*.2f,z),new(s,s*.5f,s*.8f),stone).Rotation=new(0,i*1.3f,0);
        }
        for(float z=0;z<54;z+=4.5f)
        {
            if(Math.Abs(z-WheelAxle.Z)<1.5f)continue;
            float x=BrookX(z)+(((int)(z/4.5f))%2==0?1.95f:-1.95f),s=.3f+.15f*Mathf.Abs(Mathf.Sin(z));
            Sphere(this,$"Bank stone {GetChildCount()}",new(x,Height(x,z)+s*.15f,z),new(s,s*.55f,s*.8f),stone).Rotation=new(0,z,0);
        }
        // Pebbles along the waterline and a few half-sunk rocks midstream, clear of the footbridge, the climb-out bank at z 33 and the water wheel; their own stream.
        var pebbles=new RandomNumberGenerator{Seed=4410};
        for(float z=-1;z<54;z+=2.4f)
        {
            float side=pebbles.Randf()<.5f?-1:1,x=BrookX(z)+side*pebbles.RandfRange(1.3f,1.7f),at=z+pebbles.RandfRange(-.6f,.6f);
            if(Math.Abs(at-15)>2&&Math.Abs(at-WheelAxle.Z)>1.6f)Prop(this,$"River pebbles {GetChildCount()}","pebbles",new(x,Height(x,at),at),pebbles.Randf()*Mathf.Tau);
        }
        foreach(float z in new[]{4.5f,9,21,26.5f,40,46})
        {float x=BrookX(z)+(z%3<1.5f?-.9f:.9f);Prop(this,$"Half-sunk rock {GetChildCount()}","river_rock",new(x,WaterLevel-.08f,z),z,Vector3.One*(.8f+.03f*(z%4)));}
        // Planks are tilted to the arch tangent: stepped flat boxes would leave ledges the capsule cannot climb.
        float xa=BrookX(15)-2.1f,xb=BrookX(15)+2.1f;const int planks=9;
        float Deck(float t)=>Mathf.Lerp(Height(xa,15),Height(xb,15),t)+.28f*Mathf.Sin(Mathf.Pi*t);
        var bridge=new Node3D{Name="Brook footbridge"};AddChild(bridge);
        for(int i=0;i<planks;i++)
        {
            float t0=(float)i/planks,t1=(float)(i+1)/planks,x0=Mathf.Lerp(xa,xb,t0),x1=Mathf.Lerp(xa,xb,t1);
            float angle=Mathf.Atan2(Deck(t1)-Deck(t0),x1-x0),mid=(Deck(t0)+Deck(t1))/2;
            Box(bridge,"Footbridge plank",new((x0+x1)/2,mid-.06f,15),new((x1-x0)/Mathf.Cos(angle)+.03f,.12f,1.6f),wood,true).Rotation=new(0,0,angle);
            foreach(float side in new[]{-.85f,.85f})
            {
                Box(bridge,"Footbridge rail",new((x0+x1)/2,mid+.55f,15+side),new((x1-x0)/Mathf.Cos(angle)+.02f,.08f,.08f),wood,true).Rotation=new(0,0,angle);
                if(i%4==0)Box(bridge,"Footbridge post",new(x0,Deck(t0)+.25f,15+side),new(.1f,.62f,.1f),wood);
            }
        }
        foreach(float side in new[]{-.85f,.85f})Box(bridge,"Footbridge post",new(xb,Deck(1)+.25f,15+side),new(.1f,.62f,.1f),wood);
    }
    private Node3D wheel=null!,boat=null!;private float clock;
    // The rotation that lays a model's +Z along d.
    private static Vector3 Along(Vector3 d)=>new(-Mathf.Atan2(d.Y,new Vector2(d.X,d.Z).Length()),Mathf.Atan2(d.X,d.Z),0);
    // The moored boat floats with its bottom 15 cm under the water, bow upstream (the river flows toward +X).
    public static Vector3 BoatRest=>new(Mooring.X,WaterLevel-.15f,Mooring.Y);
    public override void _Process(double delta)
    {
        wheel.RotateX((float)delta*BrookFlow/WheelRadius);clock+=(float)delta;
        // A slow swell on the boat: 2.5 cm of heave, 1.5 degrees of roll and half a degree of pitch, on unrelated periods.
        boat.Position=BoatRest+Vector3.Up*.025f*Mathf.Sin(clock*1.3f);boat.Rotation=new(.026f*Mathf.Sin(clock*.83f),Mathf.Pi,.009f*Mathf.Sin(clock*1.1f+1));
    }
    // An undershot wheel in the brook beside the sluice, turned by the current (its bottom moves with the water, toward -Z).
    // Crossed timber legs cradle its axle near both ends, on the brook bed and at the far waterline, and a board trough carries
    // what its buckets lift over the sluice board into the channel.
    private void WaterWheel()
    {
        wheel=Model(this,"Water wheel","waterwheel",WheelAxle,0,new(1.3f,1.3f,1.3f));wheel.GetChild<Node3D>(0).Position=new(0,-WheelRadius,0);
        var body=new StaticBody3D{Name="Water wheel collision",Position=WheelAxle,RotationDegrees=new(0,0,90),CollisionMask=0};AddChild(body);
        body.AddChild(new CollisionShape3D{Shape=new CylinderShape3D{Radius=WheelRadius,Height=.75f}});
        void Timber(string name,string model,Vector3 a,Vector3 b,Vector3 size)
        {
            var d=b-a;var turn=Along(d);
            foreach(var n in Prop(this,$"{name} {GetChildCount()}",model,(a+b)/2,0,new(1,1,d.Length()/2)))n.Rotation=turn;
            Solid((a+b)/2+Basis.FromEuler(turn)*new Vector3(0,model=="trough"?size.Y/2:0,0),turn,size with{Z=d.Length()});
        }
        foreach(float x in new[]{WheelAxle.X-1.1f,WheelAxle.X+1.1f})foreach(float side in new[]{-1f,1f})
        {
            var foot=new Vector3(x,0,WheelAxle.Z+side*.85f);foot.Y=Height(foot.X,foot.Z)-.1f;var cradle=new Vector3(x,WheelAxle.Y-.1f,WheelAxle.Z);
            Timber("Water wheel leg","field_rail",foot,cradle+(cradle-foot).Normalized()*.3f,new(.12f,.12f,0));
        }
        float z=WheelAxle.Z+.25f;var top=new Vector3(WheelAxle.X+.55f,WheelAxle.Y+WheelRadius-.35f,z);var spout=new Vector3(22.45f,Height(22.45f,z)+.8f,z);
        Timber("Water wheel trough","trough",top,spout,new(.32f,.19f,0));
        foreach(float x in new[]{20.95f,21.6f})Timber("Water wheel trough post","field_rail",new(x,Height(x,z)-.1f,z),new(x,Mathf.Lerp(top.Y,spout.Y,(x-top.X)/(spout.X-top.X)),z),new(.12f,.12f,0));
    }
    // Reeds and rushes along the brook and both river banks, clear of the stone bridge, the footbridge, the footpaths and the sluice; their own stream.
    private void Reeds()
    {
        var r=new RandomNumberGenerator{Seed=4421};var reeds=new List<(Vector3 P,float H)>();
        void Clump(Vector2 at){if(r.Randf()<.4f)return;for(int i=3+r.RandiRange(0,2);i>0;i--){var q=at+Vector2.FromAngle(r.Randf()*Mathf.Tau)*r.RandfRange(0,.3f);float h=r.RandfRange(.8f,1.35f);if(!Landing(q,.5f))reeds.Add((new(q.X,Height(q.X,q.Y),q.Y),h));}}
        for(float z=-1.5f;z<53;z+=.9f)foreach(int side in new[]{-1,1})
            if(Math.Abs(z-15)>2&&(side<0||Math.Abs(z-36.8f)>1.4f))Clump(new(BrookX(z)+side*r.RandfRange(1.4f,1.9f),z));
        // The river's waterline; the village bank skips the brook's mouth.
        for(float x=-60;x<60;x+=1.1f)foreach(int side in new[]{-1,1})
        {
            if(Math.Abs(x)<4.5f||(side>0&&Math.Abs(x-BrookX(-4))<2.8f))continue;
            float z=Waterline(x,-8,WaterLevel,side);
            if(!float.IsNaN(z))Clump(new(x,z-side*.15f));
        }
        Grass("River reeds",reeds,1,_=>true,9,.12f,new("3d5226"),new("9aa45a"),.25f,4425);
        // The Cedar River's, from their own seed, clear of the Cedar Bridge's embankments.
        r=new RandomNumberGenerator{Seed=4451};reeds=new();
        for(float x=-60;x<60;x+=1.1f)foreach(int side in new[]{-1,1})
        {
            float z=Waterline(x,-111,.7f,side);
            if(!float.IsNaN(z)&&Math.Abs(x-RoadX(-111))>6)Clump(new(x,z-side*.15f));
        }
        Grass("Cedar River reeds",reeds,1,_=>true,9,.12f,new("3d5226"),new("9aa45a"),.25f,4455);
    }
    // Where the bank rises 3 cm above a river's level, searched outward from mid-stream; NaN where the bed is dry (the Cedar River's, x -56 to -2,
    // runs over the hill there) or it stays under for 6 m.
    private static float Waterline(float x,float mid,float level,int side)
    {
        float z=mid;while(Math.Abs(z-mid)<6&&Height(x,z)<level+.03f)z+=side*.1f;
        return z!=mid&&Math.Abs(z-mid)<6?z:float.NaN;
    }
    // Two Tripo models on the river's village bank: a boat moored by its bow to a field post at the water's edge, and stone washing steps
    // down the notch Height() cuts for them. Stairs are a ramp to the capsule, so a box under the treads' nosings carries walkers
    // between boxes for the top step and the slab, with the side walls solid.
    private void Landings()
    {
        boat=Model(this,"Moored boat","boat",BoatRest,180,new(1.3f,1.3f,1.3f));
        var hull=new StaticBody3D{Name="Boat collision",CollisionMask=0};boat.AddChild(hull);
        hull.AddChild(new CollisionShape3D{Position=new(0,.3f,0),Shape=new BoxShape3D{Size=new(3.6f,.6f,1.15f)}});
        var post=new Vector3(Mooring.X-2.6f,0,-4.55f);post.Y=Height(post.X,post.Z);
        Prop(this,"Mooring post","field_post",post,.7f);Solid(post+Vector3.Up*.55f,Vector3.Zero,new(.15f,1.3f,.15f));
        var knot=post+Vector3.Up*.7f;var bow=new Vector3(Mooring.X-1.85f,BoatRest.Y+.85f,Mooring.Y);var sag=(knot+bow)/2-Vector3.Up*.12f;
        foreach(var (a,b) in new[]{(knot,sag),(sag,bow)})
        {
            var d=b-a;var turn=Along(d);
            foreach(var n in Prop(this,$"Mooring rope {GetChildCount()}","field_rail",(a+b)/2,0,new(.22f,.22f,d.Length()/2)))n.Rotation=turn;
        }
        // The model's slab top is 0.195 m over its base: set 8 cm over the water, its four treads land at 0.50, 0.30, 0.11 and -0.09 m.
        float x=Kawabata.X,top=Kawabata.Y+1.19f;
        Model(this,"Washing steps","kawabata",new(x,WaterLevel+.08f-.195f,Kawabata.Y),90,new(1.3f,1.3f,1.3f));
        void Slope(Vector3 a,Vector3 b,float width,float depth)
        {
            var d=b-a;var turn=Along(d);
            Solid((a+b)/2-Basis.FromEuler(turn)*Vector3.Up*depth/2,turn,new(width,depth,d.Length()));
        }
        Solid(new(x,.35f,top-.295f),Vector3.Zero,new(1.2f,.3f,.59f));
        Slope(new(x,.5f,top-.59f),new(x,WaterLevel+.08f,top-1.79f),1.2f,.3f);
        Solid(new(x,WaterLevel-.07f,top-2.09f),Vector3.Zero,new(1.7f,.3f,.6f));
        foreach(float side in new[]{-.75f,.75f})Slope(new(x+side,.72f,top-.2f),new(x+side,.13f,top-2.2f),.3f,.9f);
    }
    // Pebbles just above both rivers' waterlines and the odd half-sunk rock just below, clear of the bridges, the brook's mouth and the landings.
    private void RiverStones()
    {
        var r=new RandomNumberGenerator{Seed=4461};
        foreach(var (mid,level,bridge) in new[]{(-8f,WaterLevel,0f),(-111f,.7f,RoadX(-111))})
            for(float x=-58;x<58;x+=2.9f)foreach(int side in new[]{-1,1})
            {
                float at=x+r.RandfRange(-.8f,.8f),z=Waterline(at,mid,level,side),pick=r.Randf();
                if(float.IsNaN(z)||Math.Abs(at-bridge)<5.5f||(mid>-9&&side>0&&Math.Abs(at-BrookX(-4))<3))continue;
                if(pick<.35f){float turn=r.Randf()*Mathf.Tau;if(!Landing(new(at,z),.8f))Prop(this,$"River pebbles {GetChildCount()}","pebbles",new(at,Height(at,z+side*.35f),z+side*.35f),turn);}
                else if(pick>.88f&&!Landing(new(at,z),.8f))Prop(this,$"Half-sunk rock {GetChildCount()}","river_rock",new(at,level-.08f,z-side*.5f),at,Vector3.One*(.8f+.4f*(pick-.88f)/.12f));
            }
    }
    // Hoshikawa's paddies, levelled by Height(): aze around and between them, still water over the wet mud floors, rice in jittered hills
    // of two or three tufts, an irrigation channel from the brook behind a sluice board, and a hasa rack of drying straw.
    private void RiceFields()
    {
        // The aze: one .25 m heightfield over the terrain mesh, each crest .25 m above the higher ground .75 m either side, sinking 2 cm under
        // the terrain mesh where its profile runs out. The channel's banks are aze too, meeting the x 24 one's ends across its gap.
        var aze=new (Vector2 A,Vector2 B)[]{(new(24,20.8f),new(24,35.9f)),(new(24,37.7f),new(24,39.2f)),(new(31,20.8f),new(31,39.2f)),(new(38,20.8f),new(38,39.2f)),
            (new(24,20.8f),new(38,20.8f)),(new(24,30),new(38,30)),(new(24,39.2f),new(38,39.2f)),(new(22.1f,35.9f),new(24,35.9f)),(new(22.1f,37.7f),new(24,37.7f))};
        float Levee(float x,float z)
        {
            var p=new Vector2(x,z);float ground=MeshHeight(x,z)-.02f,y=ground;
            foreach(var (a,b) in aze)
            {
                var c=Geometry2D.GetClosestPointToSegment(p,a,b);var n=(b-a).Normalized().Orthogonal()*.75f;
                y=Math.Max(y,Mathf.Lerp(ground,Math.Max(Height(c.X+n.X,c.Y+n.Y),Height(c.X-n.X,c.Y-n.Y))+.25f,1-Mathf.SmoothStep(.2f,.75f,c.DistanceTo(p))));
            }
            return y;
        }
        const float s=.25f,x0=21.3f,z0=20;const int w=70,h=80;var y=new float[w+1,h+1];
        for(int i=0;i<=w;i++)for(int j=0;j<=h;j++)y[i,j]=Levee(x0+i*s,z0+j*s);
        var st=new SurfaceTool();st.Begin(Godot.Mesh.PrimitiveType.Triangles);
        // Vertex red is terrain.gdshader's dryness, so each paddy's inner foot reads wet up to just above its water.
        void V(int i,int j)
        {
            float x=x0+i*s,z=z0+j*s;int k=Array.FindIndex(Paddies,p=>p.HasPoint(new(x,z)));
            st.SetNormal(new Vector3(y[Math.Max(i-1,0),j]-y[Math.Min(i+1,w),j],2*s,y[i,Math.Max(j-1,0)]-y[i,Math.Min(j+1,h)]).Normalized());
            st.SetColor(new(k<0?1:Mathf.SmoothStep(PaddyLevel[k]+.05f,PaddyLevel[k]+.2f,y[i,j]),1,1));st.AddVertex(new(x,y[i,j],z));
        }
        bool Raised(int i,int j)=>y[i,j]>MeshHeight(x0+i*s,z0+j*s)-.015f;
        for(int i=0;i<w;i++)for(int j=0;j<h;j++)if(Raised(i,j)||Raised(i+1,j)||Raised(i,j+1)||Raised(i+1,j+1)){V(i,j);V(i+1,j);V(i,j+1);V(i+1,j);V(i+1,j+1);V(i,j+1);}
        st.Index();var mesh=st.Commit();
        var earth=new ShaderMaterial{Shader=GD.Load<Shader>("res://assets/shaders/terrain.gdshader")};earth.SetShaderParameter("atlas",GD.Load<Texture2D>("res://assets/art/material-atlas.png"));earth.SetShaderParameter("rocks",0f);
        Mesh(this,"Aze levees",mesh,Vector3.Zero,earth);
        var body=new StaticBody3D{Name="Aze collision",CollisionMask=0};AddChild(body);body.AddChild(new CollisionShape3D{Shape=mesh.CreateTrimeshShape()});
        var still=new ShaderMaterial{Shader=GD.Load<Shader>("res://assets/shaders/paddy.gdshader")};
        for(int i=0;i<Paddies.Length;i++)Mesh(this,$"Rice paddy water {i}",new PlaneMesh{Size=Paddies[i].Size},new(Paddies[i].GetCenter().X,PaddyLevel[i]+.08f,Paddies[i].GetCenter().Y),still);
        var plant=new RandomNumberGenerator{Seed=2213};var green=new List<(Vector3 P,float H)>();var pale=new List<(Vector3 P,float H)>();
        foreach(var p in Paddies)for(float x=p.Position.X+.45f;x<p.End.X-.35f;x+=.55f)for(float z=p.Position.Y+.45f;z<p.End.Y-.35f;z+=.62f)
        {
            var hill=new Vector2(x+plant.RandfRange(-.06f,.06f),z+plant.RandfRange(-.06f,.06f));var field=plant.Randf()<.3f?pale:green;float tall=plant.RandfRange(.55f,.7f);
            // The scarecrow's ring is left unplanted after the draws, so every other hill keeps its place.
            for(int i=plant.Randf()<.5f?3:2;i>0;i--){var q=hill+Vector2.FromAngle(plant.Randf()*Mathf.Tau)*plant.RandfRange(.03f,.08f);if(hill.DistanceTo(Scarecrow)>.6f)field.Add((new(q.X,Height(q.X,q.Y),q.Y),tall*plant.RandfRange(.85f,1.1f)));}
        }
        Grass("Rice",green,1,_=>true,8,.2f,new("3d5a2a"),new("93b052"),0,2207);
        Grass("Rice, paler hills",pale,1,_=>true,8,.2f,new("4a6230"),new("b3c062"),0,2219);
        var tuft=new RandomNumberGenerator{Seed=4431};var crest=new List<(Vector3 P,float H)>();
        foreach(var (a,b) in aze)for(float t=0;t<a.DistanceTo(b);t+=.4f)foreach(float side in new[]{-.28f,.28f})
        {
            var q=a.MoveToward(b,t)+(b-a).Normalized().Orthogonal()*side+new Vector2(tuft.RandfRange(-.1f,.1f),tuft.RandfRange(-.1f,.1f));
            crest.Add((new(q.X,Levee(q.X,q.Y),q.Y),tuft.RandfRange(.18f,.38f)));
        }
        Grass("Aze grass",crest,1,_=>true,6,.4f,new("3b5121"),new("8ea851"),.06f,4433);
        // The channel runs level from a sluice board on the brook bank into paddy 2, between its banks and through the x 24 aze.
        float level=Math.Max(PaddyLevel[2]+.08f,Enumerable.Range(0,10).Max(i=>MeshHeight(22+i*.25f,36.8f))+.04f);
        var ditch=new ShaderMaterial{Shader=still.Shader};ditch.SetShaderParameter("size",new Vector2(2.4f,.9f));
        Mesh(this,"Irrigation channel water",new PlaneMesh{Size=new(2.4f,.9f)},new(23.1f,level,36.8f),ditch);
        Prop(this,"Sluice board","sluice",new(21.95f,Height(21.95f,36.8f),36.8f));
        var crow=Model(this,"Scarecrow","scarecrow",new(Scarecrow.X,Height(Scarecrow.X,Scarecrow.Y),Scarecrow.Y),90,new(1.3f,1.3f,1.3f));
        var pole=new StaticBody3D();crow.AddChild(pole);pole.AddChild(new CollisionShape3D{Position=new(0,.85f,0),Shape=new CylinderShape3D{Radius=.06f,Height=1.7f}});
        var rack=new Vector3(30,Height(30,40.7f),40.7f);Prop(this,"Hasa drying rack","hasa",rack);
        var frame=new StaticBody3D{Position=rack+new Vector3(0,.8f,0),CollisionMask=0};AddChild(frame);frame.AddChild(new CollisionShape3D{Shape=new BoxShape3D{Size=new(5.6f,1.6f,.5f)}});
    }
    private void Terrace()
    {
        for(float z=3;z<51;z+=2)
        {
            float bottom=Height(-19.2f,z+1)-.3f,top=Height(-20.9f,z+1)+.25f;
            if(top-bottom<.35f)continue;
            Box(this,"Ishigaki terrace wall",new(-19.95f,(top+bottom)/2,z+1),new(.55f,top-bottom,2.02f),stone,true);
        }
    }
    private void Shrine()
    {
        Torii(new(RoadX(52),Height(RoadX(52),52),52),0);
        var red=Mat(new("b54a32"));
        for(int i=0;i<12;i++)
        {
            float z=53.2f+i*.46f,x=Mathf.Lerp(RoadX(53),ShrineCenter.X,(z-53)/5.5f);
            Box(this,"Shrine approach step",new(x,Height(x,z)+.1f-.2f,z),new(2.6f,.4f,.5f),stone);
        }
        StoneLantern(this,new(-2.6f,Height(-2.6f,57.5f),57.5f));StoneLantern(this,new(.6f,Height(.6f,57.5f),57.5f));
        var c=ShrineCenter+new Vector2(0,2.2f);
        var h=new Node3D{Name="Hoshikawa shrine",Position=new(c.X,Height(c.X,c.Y),c.Y),RotationDegrees=new(0,180,0)};AddChild(h);
        var timber=Mat(new("7a5a3c"));var straw=Mat(new("d9c48a"));var copper=Mat(new("5f7f6f"));
        Box(h,"Shrine stone base",new(0,.3f,0),new(3.8f,.6f,3.4f),stone,true);
        Box(h,"Shrine hall",new(0,1.55f,0),new(3,1.9f,2.6f),timber,true);
        foreach(float x in new[]{-1.5f,1.5f})foreach(float z in new[]{-1.3f,1.3f})Box(h,"Shrine pillar",new(x,1.55f,z),new(.16f,1.9f,.16f),red);
        Box(h,"Shrine lintel",new(0,2.4f,1.32f),new(3.1f,.14f,.1f),red);
        foreach(float x in new[]{-.55f,.55f})Box(h,"Shrine lattice door",new(x,1.45f,1.33f),new(1,1.5f,.06f),Mat(new("e9dfc4"),.05f));
        Roof(h,"Shrine roof",3,2.6f,1.1f,.6f,"gable",copper,.15f).Position=new(0,2.5f,0);
        foreach(float x in new[]{-1.75f,1.75f})foreach(float s in new[]{-1f,1f})
            Box(h,"Chigi",new(x,3.75f,s*.18f),new(.08f,.1f,.95f),copper).Rotation=new(s*.9f,0,0);
        foreach(float x in new[]{-.8f,0,.8f})Cylinder(h,"Katsuogi",new(x,3.72f,0),.08f,.7f,timber).Rotation=new(Mathf.Pi/2,0,0);
        for(int i=0;i<3;i++)Box(h,"Shrine stair",new(0,.15f*(3-i)-.07f,1.85f+i*.3f),new(1.4f,.15f*(3-i)+.14f,.3f),stone,true);
        Box(h,"Offering box",new(0,.85f,1.55f),new(1,.5f,.35f),timber);
        Cylinder(h,"Shimenawa rope",new(0,2.55f,1.5f),.075f,3,straw).Rotation=new(0,0,Mathf.Pi/2);
        foreach(float x in new[]{-1.05f,-.35f,.35f,1.05f})Box(h,"Shide paper",new(x,2.3f,1.5f),new(.1f,.34f,.02f),Mat(new("f4f1e8"),.1f)).Rotation=new(0,0,.12f);
        Sphere(h,"Shrine bell",new(0,2.3f,1.62f),Vector3.One*.13f,Mat(Gold));
        Cylinder(h,"Bell rope",new(0,1.55f,1.62f),.03f,1.4f,red);
    }
    private void Torii(Vector3 p,float yaw)
    {
        var t=new Node3D{Name="Shrine torii",Position=p,RotationDegrees=new(0,yaw,0)};AddChild(t);
        var red=Mat(new("b54a32"));var black=Mat(new("2b2724"));
        foreach(float s in new[]{-2f,2f})
        {
            var g=t.ToGlobal(new(s,0,0));float b=Height(g.X,g.Z)-p.Y;
            Cylinder(t,"Torii pillar",new(s,(b-.2f+4.3f)/2,0),.16f,4.5f-b,red,.14f);
            Cylinder(t,"Torii footing",new(s,b+.12f,0),.24f,.34f,black);
            var body=new StaticBody3D{Position=new(s,2,0)};t.AddChild(body);body.AddChild(new CollisionShape3D{Shape=new CylinderShape3D{Radius=.18f,Height=4.4f}});
        }
        Box(t,"Torii nuki",new(0,3.35f,0),new(5,.22f,.16f),red);
        Box(t,"Torii gakuzuka",new(0,3.75f,0),new(.18f,.6f,.14f),red);
        Box(t,"Torii plaque",new(0,3.78f,.1f),new(.42f,.5f,.06f),black);
        Box(t,"Torii shimaki",new(0,4.15f,0),new(4.8f,.2f,.3f),red);
        Box(t,"Torii kasagi",new(0,4.37f,0),new(4.6f,.24f,.36f),black);
        foreach(float s in new[]{-1f,1f})Box(t,"Torii kasagi tip",new(s*2.65f,4.45f,0),new(.9f,.24f,.36f),black).Rotation=new(0,0,s*.2f);
    }
    public void StoneLantern(Node3D parent,Vector3 p)
    {
        var n=Model(parent,"Stone lantern","lantern",p,0,new(1.2f,1.2f,1.2f));
        var body=new StaticBody3D();n.AddChild(body);body.AddChild(new CollisionShape3D{Position=new(0,.875f,0),Shape=new CylinderShape3D{Radius=.33f,Height=1.75f}});
    }
    private readonly RandomNumberGenerator villageRng=new(){Seed=3301};
    private static bool OnPad(float x,float z)=>Hamlet.Any(h=>{var l=new Vector2(x-h.At.X,z-h.At.Y).Rotated(Mathf.DegToRad(h.Yaw));return Math.Abs(l.X)<h.Size.X/2+1.6f&&Math.Abs(l.Y)<h.Size.Z/2+1.6f;});
    // Everything here draws from villageRng, so the countryside's seeded scatter never shifts when the village changes.
    private void Dressing()
    {
        var r=villageRng;var boards=VillageAtlas(new(1,0),new(3,2));var soil=Mat(new("5d4a36"));
        var keep=Residents.Where(x=>x.Body.Position.Z>0).SelectMany(x=>new[]{new Vector2(x.Body.Position.X,x.Body.Position.Z),new Vector2(x.Body.Position.X,x.Body.Position.Z+2)}).ToArray();
        bool Clear(Vector2 p,float room)=>keep.All(k=>k.DistanceTo(p)>room)&&Math.Abs(p.X-RoadX(p.Y))>3.6f&&!OnFootpath(p,.4f);
        Vector3 Ground(Vector2 p,float lift=0)=>new(p.X,Height(p.X,p.Y)+lift,p.Y);
        StoneLantern(this,Ground(new(-6.5f,17.5f)));StoneLantern(this,Ground(new(-6.5f,29)));
        Bench(new(-5.4f,27.3f),90);Bench(new(5.2f,26.5f),-90);
        var board=Model(this,"Notice board","notice",Ground(new(6.4f,17.2f)),180,new(1.3f,1.3f,1.3f));
        var boardBody=new StaticBody3D();board.AddChild(boardBody);
        foreach(float z in new[]{-.98f,.98f})boardBody.AddChild(new CollisionShape3D{Position=new(0,1.1f,z),Shape=new BoxShape3D{Size=new(.24f,2.2f,.18f)}});
        // The notices are painted into the model now; their fifteen draws stay so every later village placement keeps its seed.
        for(int i=0;i<15;i++)r.Randf();
        int side=-1;
        foreach(float z in new[]{2f,10,19,29,37,45}){var p=new Vector2(RoadX(z)+side*3.9f,z);if(keep.All(k=>k.DistanceTo(p)>1.5f))Lamp(Ground(p));side=-side;}
        foreach(var p in new Vector2[]{new(-7,27.5f),new(2.4f,63.2f)})Tree(Ground(p),r.RandfRange(.8f,.95f),"sakura",r);
        foreach(var p in new Vector2[]{new(-22.6f,19),new(-29.5f,25),new(-23,38.5f),new(-30,44),new(16.8f,10.6f),new(23.5f,47),new(26,53),new(-14,50)})
            Tree(Ground(p),r.RandfRange(.75f,.95f),"keyaki",r);
        foreach(var p in new Vector2[]{new(-18.2f,6.4f),new(-18.7f,10.4f),new(-22.3f,12.6f)})Tree(Ground(p),r.RandfRange(.85f,1.05f),"bamboo",r);
        for(int i=0;i<7;i++)
        {
            float a=Mathf.DegToRad(22+i*22.5f),d=r.RandfRange(8.2f,10.5f);
            Tree(Ground(ShrineCenter+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*d),r.RandfRange(.8f,1),"sugi",r);
        }
        foreach(var (name,at,size,yaw,style) in Hamlet)
        {
            var b=Buildings.Single(n=>n.Name==name);float dz=size.Z/2;var (_,walls,sign)=TripoHouses[style];
            Vector2 World2(float x,float z){var g=b.ToGlobal(new(x,0,z));return new(g.X,g.Z);}
            foreach(float s in new[]{-1f,1f}){var p=World2(s*(size.X/2+.7f),dz+.7f);if(Clear(p,1.3f))Tree(Ground(p),r.RandfRange(.8f,1.15f),"shrub",r);}
            var door=World2(sign.X,walls.End.Y+(style is "hall" or "clinic"?2.5f:1.3f));
            var road=new Vector2(RoadX(door.Y)+Math.Sign(door.X-RoadX(door.Y))*2.9f,door.Y);
            for(float t=0;t<=door.DistanceTo(road);t+=.8f)
            {var p=door.MoveToward(road,t)+new Vector2(r.RandfRange(-.1f,.1f),r.RandfRange(-.1f,.1f));Cylinder(this,"Stepping stone",Ground(p,.02f),r.RandfRange(.24f,.32f),.08f,stone);}
            // Hall pots flank its porch steps; the clinic's stand on the engawa deck either side of its steps, the deck's top .83 m up.
            if(style=="hall")foreach(float s in new[]{-2.4f,2.4f})Pot(Ground(World2(s,walls.End.Y+1.6f)));
            if(style=="clinic")foreach(float s in new[]{-1.4f,1.4f})Pot(b.ToGlobal(new(s,.83f,walls.End.Y+.45f)));
        }
        // KOMA-7's kitchen garden and the farmhouse vegetable plot.
        GroundStrip("Kitchen garden soil",new(10.2f,11.55f),new(14.8f,11.55f),1.15f,soil);
        for(int i=0;i<6;i++)
        {
            float x=10.6f+i*.8f;
            Box(this,"Tomato stake",Ground(new(x,12.2f),.55f),new(.04f,1.1f,.04f),wood);
            Sphere(this,"Tomato vine",Ground(new(x,12.2f),.55f),new(.22f,.4f,.22f),Mat(new("5f8a45")));
            for(int k=0;k<2;k++)Sphere(this,"Ripe tomato",Ground(new(x+(k-.5f)*.18f,12.1f),.55f+k*.22f),Vector3.One*.07f,Mat(new("c9432f")));
            Sphere(this,"Cabbage",Ground(new(x,10.9f),.12f),new(.26f,.18f,.26f),Mat(new("89a95e")));
        }
        BambooFence(new(9.9f,13),new(15.1f,13));BambooFence(new(15.1f,13),new(15.1f,10.1f));
        GroundStrip("Vegetable plot soil",new(11.8f,35),new(15.2f,35),.8f,soil);
        for(int i=0;i<9;i++)foreach(float z in new[]{34.6f,35.4f})Sphere(this,"Leafy greens",Ground(new(12.1f+i*.37f,z),.1f),new(.16f,.13f,.16f),Mat(new(i%3==0?"6f9a4a":"93b35f")));
        // Household props.
        var line=new Node3D{Name="Laundry line",Position=Ground(new(-13,31.4f)),RotationDegrees=new(0,-4,0)};AddChild(line);
        foreach(float x in new[]{-1.6f,1.6f})Box(line,"Laundry post",new(x,.95f,0),new(.1f,1.9f,.1f),wood);
        Cylinder(line,"Laundry pole",new(0,1.85f,0),.03f,3.4f,Mat(new("a9a266"))).Rotation=new(0,0,Mathf.Pi/2);
        for(int i=0;i<4;i++)Box(line,"Drying cloth",new(-1.1f+i*.72f,1.45f,0),new(.52f,.75f,.02f),Mat(new(i%2==0?"eeeadf":"3b5379")));
        var wood2=Mat(new("7a5a3c"));
        for(int row=0;row<3;row++)for(int i=0;i<5-row;i++)Cylinder(this,"Firewood",Ground(new(13.95f+i*.24f+row*.12f,49.37f),.12f+row*.21f),.11f,1,wood2).Rotation=new(Mathf.Pi/2,0,0);
        Box(this,"Woodpile roof",Ground(new(14.45f,49.37f),.95f),new(1.7f,.06f,1.3f),boards).Rotation=new(.2f,0,0);
        foreach(var p in new Vector2[]{new(16.05f,49.97f),new(16.65f,48.97f)})Cylinder(this,"Straw roll",Ground(p,.45f),.45f,.9f,Mat(new("d6bd78"))).Rotation=new(0,0,Mathf.Pi/2);
        var cart=Model(this,"Handcart","cart",Ground(new(12.9f,1.6f)),160,new(1.3f,1.3f,1.3f));
        var cartBody=new StaticBody3D();cart.AddChild(cartBody);
        cartBody.AddChild(new CollisionShape3D{Position=new(-.72f,.5f,0),Shape=new BoxShape3D{Size=new(1.24f,1,1.42f)}});cartBody.AddChild(new CollisionShape3D{Position=new(.61f,.5f,0),Shape=new BoxShape3D{Size=new(1.42f,.4f,1)}});
        foreach(var p in new Vector2[]{new(-12.4f,13.6f),new(-13.2f,13.9f),new(-12.8f,14.6f)})Prop(this,"Stored barrel","barrel",Ground(p),p.X*3);
        // Grass tufts on the village lawns, clear of the road, square, pads, footpaths, brook and shrine mound; blades reach .35 m from their root.
        var tufts=new List<(Vector3 P,float H)>();
        bool Lawn(float x,float z)=>!(Math.Abs(x-RoadX(z))<3.8f||new Vector2(x,z-21).Length()<8||(Math.Abs(x-BrookX(z))<2.6f&&z<58)||new Vector2(x,z).DistanceTo(ShrineCenter)<10||OnPad(x,z)||OnFootpath(new(x,z),.35f)||(x>21&&x<38.9f&&z>20&&z<40.1f));
        for(int i=0;i<2500;i++)
        {
            float x=r.RandfRange(-39,39),z=r.RandfRange(0,72),h=r.RandfRange(.16f,.5f);
            if(Lawn(x,z))tufts.Add((Ground(new(x,z)),h));
        }
        Grass("Village grass",tufts,3,p=>Lawn(p.X,p.Y),7,.45f,new("3b5121"),new("8ea851"),.05f,3307);
    }
    private void Bench(Vector2 p,float yaw)
    {
        var n=new Node3D{Name="Village bench",Position=new(p.X,Height(p.X,p.Y),p.Y),RotationDegrees=new(0,yaw,0)};AddChild(n);
        Prop(n,"Bench seat","bench",Vector3.Zero);
        var body=new StaticBody3D{CollisionMask=0};n.AddChild(body);body.AddChild(new CollisionShape3D{Position=new(0,.45f,0),Shape=new BoxShape3D{Size=new(1.6f,.08f,.42f)}});
    }
    private void Pot(Vector3 p)
    {
        Cylinder(this,$"Plant pot {GetChildCount()}",p+new Vector3(0,.16f,0),.2f,.32f,Mat(new("9c5a3c")),.16f);
        Sphere(this,"Potted plant",p+new Vector3(0,.48f,0),new(.28f,.24f,.28f),Mat(new("6f9a4a")));
    }
    private void BambooFence(Vector2 a,Vector2 b)
    {
        int n=Math.Max(1,(int)(a.DistanceTo(b)/1.2f));Vector3 Ground(Vector2 p)=>new(p.X,Height(p.X,p.Y),p.Y);
        for(int i=0;i<=n;i++){var p=a.Lerp(b,(float)i/n);Prop(this,"Bamboo fence post","fence_post",Ground(p),i);}
        // Each bay is modelled 1.2 m long and stretched to the post spacing.
        for(int i=0;i<n;i++)Prop(this,"Bamboo fence rail","fence_panel",Ground(a.Lerp(b,(i+.5f)/n)),Mathf.Atan2(b.X-a.X,b.Y-a.Y),new(1,1,a.DistanceTo(b)/n/1.2f));
    }
}
