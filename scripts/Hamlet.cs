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
        Mesh(this,"Hoshikawa brook",st.Commit(),Vector3.Zero,new ShaderMaterial{Shader=GD.Load<Shader>("res://assets/shaders/water.gdshader")});
        for(int i=0;i<6;i++)
        {
            float z=55.6f+i%3*.7f,x=BrookX(z)+(i-2.5f)*.55f,s=.35f+.12f*(i%3);
            Sphere(this,"Spring stone",new(x,Height(x,z)+s*.2f,z),new(s,s*.5f,s*.8f),stone).Rotation=new(0,i*1.3f,0);
        }
        for(float z=0;z<54;z+=4.5f)
        {
            float x=BrookX(z)+(((int)(z/4.5f))%2==0?1.95f:-1.95f),s=.3f+.15f*Mathf.Abs(Mathf.Sin(z));
            Sphere(this,"Bank stone",new(x,Height(x,z)+s*.15f,z),new(s,s*.55f,s*.8f),stone).Rotation=new(0,z,0);
        }
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
    public void StoneLantern(Node3D parent,Vector3 p,float scale=1)
    {
        var n=new Node3D{Name="Stone lantern",Position=p,Scale=Vector3.One*scale};parent.AddChild(n);
        Cylinder(n,"Lantern plinth",new(0,.12f,0),.32f,.24f,stone);
        Cylinder(n,"Lantern shaft",new(0,.6f,0),.11f,.75f,stone,.09f);
        Box(n,"Lantern platform",new(0,1.02f,0),new(.52f,.1f,.52f),stone);
        foreach(float x in new[]{-.18f,.18f})foreach(float z in new[]{-.18f,.18f})Box(n,"Lantern corner",new(x,1.23f,z),new(.07f,.32f,.07f),stone);
        Box(n,"Lantern light",new(0,1.23f,0),new(.26f,.24f,.26f),Mat(new("f5d9a0"),.6f));
        Mesh(n,"Lantern roof",new CylinderMesh{TopRadius=.05f,BottomRadius=.44f,Height=.26f,RadialSegments=4},new(0,1.52f,0),stone).Rotation=new(0,Mathf.Pi/4,0);
        Sphere(n,"Lantern finial",new(0,1.7f,0),Vector3.One*.07f,stone);
        var body=new StaticBody3D();n.AddChild(body);body.AddChild(new CollisionShape3D{Position=new(0,.85f,0),Shape=new CylinderShape3D{Radius=.3f,Height=1.7f}});
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
        var board=new Node3D{Name="Notice board",Position=Ground(new(6.4f,17.2f)),RotationDegrees=new(0,-90,0)};AddChild(board);
        foreach(float x in new[]{-.85f,.85f})Box(board,"Notice post",new(x,1.1f,0),new(.12f,2.2f,.12f),wood,true);
        Box(board,"Notice panel",new(0,1.45f,.02f),new(1.7f,1,.06f),boards);
        Box(board,"Notice roof",new(0,2.26f,0),new(2.1f,.08f,.55f),wood).Rotation=new(.12f,0,0);
        for(int i=0;i<5;i++)Box(board,"Posted notice",new(r.RandfRange(-.6f,.6f),r.RandfRange(1.2f,1.7f),.06f),new(.26f,.34f,.01f),Mat(new("f1ead6"),.05f)).Rotation=new(0,0,r.RandfRange(-.12f,.12f));
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
            var b=Buildings.Single(n=>n.Name==name);float dz=size.Z/2;
            Vector2 World2(float x,float z){var g=b.ToGlobal(new(x,0,z));return new(g.X,g.Z);}
            foreach(float s in new[]{-1f,1f}){var p=World2(s*(size.X/2+.7f),dz+.7f);if(Clear(p,1.3f))Tree(Ground(p),r.RandfRange(.8f,1.15f),"shrub",r);}
            var door=World2(DoorX(style,size.X),dz+(style=="hall"?2.1f:1.3f));
            var road=new Vector2(RoadX(door.Y)+Math.Sign(door.X-RoadX(door.Y))*2.9f,door.Y);
            for(float t=0;t<=door.DistanceTo(road);t+=.8f)
            {var p=door.MoveToward(road,t)+new Vector2(r.RandfRange(-.1f,.1f),r.RandfRange(-.1f,.1f));Cylinder(this,"Stepping stone",Ground(p,.02f),r.RandfRange(.24f,.32f),.08f,stone);}
            // Hall pots flank its wide porch step; the clinic's stand on the engawa deck, whose top is .53 m up.
            if(style=="hall")foreach(float s in new[]{-2.1f,2.1f})Pot(Ground(World2(s,dz+.9f)));
            if(style=="clinic")foreach(float s in new[]{-1.4f,1.4f})Pot(b.ToGlobal(new(s,.53f,dz+.45f)));
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
        for(int row=0;row<3;row++)for(int i=0;i<5-row;i++)Cylinder(this,"Firewood",Ground(new(13.2f+i*.24f+row*.12f,47.3f),.12f+row*.21f),.11f,1,wood2).Rotation=new(Mathf.Pi/2,0,0);
        Box(this,"Woodpile roof",Ground(new(13.7f,47.3f),.95f),new(1.7f,.06f,1.3f),boards).Rotation=new(.2f,0,0);
        foreach(var p in new Vector2[]{new(15.3f,47.9f),new(15.9f,46.9f)})Cylinder(this,"Straw roll",Ground(p,.45f),.45f,.9f,Mat(new("d6bd78"))).Rotation=new(0,0,Mathf.Pi/2);
        var cart=new Node3D{Name="Handcart",Position=Ground(new(12.9f,1.6f)),RotationDegrees=new(0,20,0)};AddChild(cart);
        Box(cart,"Cart bed",new(0,.62f,0),new(1,.1f,1.5f),boards);
        foreach(float s in new[]{-1f,1f}){Cylinder(cart,"Cart wheel",new(s*.58f,.42f,0),.42f,.07f,wood).Rotation=new(0,0,Mathf.Pi/2);Box(cart,"Cart handle",new(s*.4f,.66f,1.25f),new(.05f,.05f,1.2f),wood);}
        for(int i=0;i<2;i++)Sphere(cart,"Rice sack",new(0,.85f,-.35f+i*.62f),new(.36f,.24f,.28f),Mat(new("d9cfa8")));
        foreach(var p in new Vector2[]{new(-12.4f,13.6f),new(-13.2f,13.9f),new(-12.8f,14.6f)})Prop(this,"Stored barrel","barrel",Ground(p),p.X*3);
        // Grass tufts on the village lawns, clear of the road, square, pads, footpaths, brook and shrine mound; blades reach .35 m from their root.
        var tufts=new List<(Vector3 P,float H)>();
        bool Lawn(float x,float z)=>!(Math.Abs(x-RoadX(z))<3.8f||new Vector2(x,z-21).Length()<8||(Math.Abs(x-BrookX(z))<2.6f&&z<58)||new Vector2(x,z).DistanceTo(ShrineCenter)<10||OnPad(x,z)||OnFootpath(new(x,z),.35f)||(x>21&&x<38&&z>20&&z<40));
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
