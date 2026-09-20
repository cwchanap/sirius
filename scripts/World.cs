using Godot;
namespace Hoshikawa;

public partial class World : Node3D
{
    public readonly List<Node3D> Buildings = new();
    public readonly List<(string Name, string Role, string Line, Node3D Body)> Residents = new();
    public const int NorthEdge = -200, SouthEdge = 80, HalfWidth = 64;
    public static Vector3 KomorebiCenter => new(0, Height(0,-154), -154);
    public static float RoadX(float z)
    {
        // Keep the terrain shader's road curve in sync with this placement guide.
        float original = Mathf.Sin(z*.068f)*2.4f;
        float pass = 10*Mathf.Sin((-z-72)*.045f);
        return Mathf.Lerp(original, pass, Mathf.SmoothStep(0,12,-z-72)) * (1-Mathf.SmoothStep(0,12,-z-134));
    }
    public static string RegionAt(Vector3 p) => p.Z < -136 ? "Komorebi" : p.Z < -78 ? "Cedar Pass" : p.Z < -14 ? "Old Relay Road" : "Hoshikawa";
    public Vector3 RelayPosition => new(1, Height(1, -61), -61);
    public static readonly Color Jade = new("72e5cc"), Gold = new("eacb89"), Ink = new("152c33");
    private readonly RandomNumberGenerator rng = new() { Seed = 2189 };
    private Material stone = null!, roof = null!, wood = null!;
    public override void _Ready()
    {
        stone = Atlas(new(0, 1), new(2, 2)); roof = Atlas(new(1, 1), new(5, 3));
        wood = Mat(new("594735"));
        Environment(); Terrain(); Village(); Countryside(); NorthernValley(); Komorebi();
    }
    public static float Height(float x, float z)
    {
        float rural = Mathf.SmoothStep(0, 45, -z);
        float y = (.38f * Mathf.Sin(x * .13f) * Mathf.Cos(z * .1f) + 2.7f * rural);
        y += 3.4f * Mathf.Exp(-((x + 22) * (x + 22) + (z + 42) * (z + 42)) / 280f);
        y -= 2.1f * Mathf.Exp(-(z + 8) * (z + 8) / 8f);
        if (z > 0 && Math.Abs(x) < 30) y *= Mathf.SmoothStep(0, 12, Math.Abs(x) - 18);
        if (Math.Abs(x) < 4 && z > -16 && z < 0)
            y = Mathf.Lerp(y, .82f, (1 - Mathf.SmoothStep(3, 4, Math.Abs(x))) * Mathf.SmoothStep(0, 2, Math.Min(z + 16, -z)));
        if (z < -78)
        {
            y += Mathf.SmoothStep(0,18,-z-78)*(1.5f+1.1f*Mathf.Sin(x*.055f)*Mathf.Sin((z+75)*.06f));
            y += 2.8f*Mathf.Exp(-((x+29)*(x+29)+(z+116)*(z+116))/460);
            y -= 4.1f*Mathf.Exp(-(z+111)*(z+111)/17);
            float bank = Mathf.SmoothStep(5,8,Math.Abs(z+111))*(1-Mathf.SmoothStep(8,13,Math.Abs(z+111)));
            y = Mathf.Lerp(y,4.1f,bank*(1-Mathf.SmoothStep(2.6f,5,Math.Abs(x-RoadX(-111)))));
            float village = (1-Mathf.SmoothStep(29,42,Math.Abs(x)))*(1-Mathf.SmoothStep(25,36,Math.Abs(z+157)));
            y = Mathf.Lerp(y,4.5f,village);
            // Grade the whole footprint and its approaches, so a center-height placement cannot leave a ledge.
            float shelterEdge=Math.Max(Math.Abs(x+12)-3.5f,Math.Abs(z+91)-3);
            float lookoutEdge=Math.Max(Math.Abs(x-29)-3.5f,Math.Abs(z+126)-3);
            y=Mathf.Lerp(y,5.2f,1-Mathf.SmoothStep(0,3,Math.Min(shelterEdge,lookoutEdge)));
        }
        return y;
    }
    public static StandardMaterial3D Mat(Color c, float emission = 0) => new()
    {
        AlbedoColor = c, Roughness = .85f, EmissionEnabled = emission > 0,
        Emission = c, EmissionEnergyMultiplier = emission
    };
    private ShaderMaterial Atlas(Vector2 cell, Vector2 repeats)
    {
        var m = new ShaderMaterial { Shader = GD.Load<Shader>("res://assets/shaders/atlas.gdshader") };
        m.SetShaderParameter("atlas", GD.Load<Texture2D>("res://assets/art/material-atlas.png"));
        m.SetShaderParameter("cell", cell); m.SetShaderParameter("repeats", repeats); return m;
    }
    public static MeshInstance3D Mesh(Node3D parent, string name, Mesh mesh, Vector3 pos, Material material)
    {
        var n = new MeshInstance3D { Name = name, Mesh = mesh, Position = pos, MaterialOverride = material };
        parent.AddChild(n); return n;
    }
    public static MeshInstance3D Box(Node3D parent, string name, Vector3 pos, Vector3 size, Material material, bool solid = false)
    {
        var n = Mesh(parent, name, new BoxMesh { Size = size }, pos, material);
        if (solid)
        {
            var body = new StaticBody3D { CollisionLayer = 1, CollisionMask = 0 };
            n.AddChild(body); body.AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = size } });
        }
        return n;
    }
    public static MeshInstance3D Sphere(Node3D parent, string name, Vector3 pos, Vector3 scale, Material mat)
    {
        var n = Mesh(parent, name, new SphereMesh { Radius = 1, Height = 2, RadialSegments = 12, Rings = 6 }, pos, mat);
        n.Scale = scale; return n;
    }
    public static MeshInstance3D Cylinder(Node3D parent, string name, Vector3 pos, float radius, float height, Material mat, float top = -1)
        => Mesh(parent, name, new CylinderMesh { BottomRadius = radius, TopRadius = top < 0 ? radius : top, Height = height, RadialSegments = 12 }, pos, mat);
    public static Label3D Label(Node3D parent, string text, Vector3 pos, Color color, int size = 32)
    {
        var label = new Label3D { Text = text, Position = pos, FontSize = size, PixelSize = .007f,
            Modulate = color, OutlineModulate = new Color(.055f,.09f,.1f,.85f), OutlineSize = 6,
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled, NoDepthTest = false };
        parent.AddChild(label); return label;
    }
    private void Environment()
    {
        var skyMaterial = new ProceduralSkyMaterial
        {
            SkyTopColor = new("457b99"), SkyHorizonColor = new("c5ddd2"),
            GroundBottomColor = new("516653"), GroundHorizonColor = new("c5ddd2"), SkyCurve = .22f
        };
        AddChild(new WorldEnvironment { Environment = new Godot.Environment
        {
            BackgroundMode = Godot.Environment.BGMode.Sky, Sky = new Sky { SkyMaterial = skyMaterial },
            AmbientLightSource = Godot.Environment.AmbientSource.Color, AmbientLightColor = new("bfdbc8"), AmbientLightEnergy = .2f,
            ReflectedLightSource = Godot.Environment.ReflectionSource.Sky, TonemapMode = Godot.Environment.ToneMapper.Aces,
            FogEnabled = true, FogLightColor = new("a4c6ba"), FogDensity = .0027f, FogSkyAffect = .3f
        }});
        AddChild(new DirectionalLight3D { RotationDegrees = new(-48, -34, 0), LightColor = new("fff0c3"),
            LightEnergy = .65f, ShadowEnabled = true, DirectionalShadowMaxDistance = 100, ShadowBias = .06f });
        var ridges=new SurfaceTool();ridges.Begin(Godot.Mesh.PrimitiveType.Triangles);
        float Ridge(float x,float z)
        {
            float outer=Math.Max(0,Math.Max(Math.Abs(x)-HalfWidth,Math.Max(NorthEdge-z,z-SouthEdge)));
            float edge=Height(Mathf.Clamp(x,-HalfWidth,HalfWidth),Mathf.Clamp(z,NorthEdge,SouthEdge));
            return edge+Mathf.SmoothStep(0,60,outer)*(23+7*Mathf.Sin(x*.047f)+6*Mathf.Sin(z*.036f)+4*Mathf.Sin((x+z)*.061f));
        }
        void RidgeVertex(float x,float z)
        {
            float y=Ridge(x,z);
            ridges.SetNormal(new Vector3(Ridge(x-1,z)-Ridge(x+1,z),2,Ridge(x,z-1)-Ridge(x,z+1)).Normalized());
            ridges.AddVertex(new(x,y,z));
        }
        for(int z=NorthEdge-100;z<SouthEdge+100;z+=4)for(int x=-180;x<180;x+=4)
        {
            if(x>=-HalfWidth&&x<HalfWidth&&z>=NorthEdge&&z<SouthEdge)continue;
            RidgeVertex(x,z);RidgeVertex(x+4,z);RidgeVertex(x,z+4);
            RidgeVertex(x+4,z);RidgeVertex(x+4,z+4);RidgeVertex(x,z+4);
        }
        ridges.Index();Mesh(this,"Layered mountain landscape",ridges.Commit(),Vector3.Zero,Mat(new("567969")));
        // The occupied city remains a distant story landmark in this rural chapter.
        for (int i=0;i<9;i++) Box(this,"Machine city silhouette",new(65+i*3,12+i%3*3,-108),new(2,18+i%3*6,3),Mat(new("728e8c")));
    }
    private void Terrain()
    {
        var st = new SurfaceTool(); st.Begin(Godot.Mesh.PrimitiveType.Triangles);
        for (int z = NorthEdge; z < SouthEdge; z++) for (int x = -HalfWidth; x < HalfWidth; x++)
        {
            Add(x,z); Add(x+1,z); Add(x,z+1);
            Add(x+1,z); Add(x+1,z+1); Add(x,z+1);
        }
        void Add(float x,float z)
        {
            var n = new Vector3(Height(x-.1f,z)-Height(x+.1f,z),.2f,Height(x,z-.1f)-Height(x,z+.1f)).Normalized();
            st.SetNormal(n); st.SetUV(new(x*.15f,z*.15f)); st.AddVertex(new(x,Height(x,z),z));
        }
        st.Index(); var mesh = st.Commit();
        var material = new ShaderMaterial { Shader = GD.Load<Shader>("res://assets/shaders/terrain.gdshader") };
        material.SetShaderParameter("atlas",GD.Load<Texture2D>("res://assets/art/material-atlas.png"));
        Mesh(this,"Rolling terrain",mesh,Vector3.Zero,material);
        var ground = new StaticBody3D { Name = "Terrain collision" }; AddChild(ground);
        ground.AddChild(new CollisionShape3D { Shape = mesh.CreateTrimeshShape() });
        var water = new ShaderMaterial { Shader = GD.Load<Shader>("res://assets/shaders/water.gdshader") };
        Mesh(this,"Hoshikawa river",new PlaneMesh { Size = new(127,4.8f), SubdivideWidth = 100 },new(0,-.38f,-8),water);
        for(int i=0;i<21;i++)
            Box(this,"Bridge plank",new(0,.72f,-13+i*.5f),new(6,.2f,.46f),wood,true);
        foreach(float x in new[]{-2.9f,2.9f})
        {
            Box(this,"Bridge handrail",new(x,1.8f,-8),new(.12f,.12f,10.5f),wood,true);
            for(int i=0;i<6;i++) Box(this,"Bridge post",new(x,1.25f,-13+i*2),new(.17f,1.25f,.17f),wood);
            for(int i=0;i<3;i++) Box(this,"Stone bridge pier",new(x*.7f,-.35f,-12+i*4),new(.75f,2,.8f),stone);
        }
        // Physical map boundary, dressed by forest and ridge silhouettes.
        float middle=(NorthEdge+SouthEdge)*.5f, depth=SouthEdge-NorthEdge;
        foreach(var (p,s) in new[]{(new Vector3(-63,10,middle),new Vector3(1,30,depth)),(new Vector3(63,10,middle),new Vector3(1,30,depth)),
            (new Vector3(0,10,NorthEdge+1),new Vector3(128,30,1)),(new Vector3(0,10,SouthEdge-1),new Vector3(128,30,1))})
        {
            var b=new StaticBody3D { Name="Valley boundary", Position=p }; AddChild(b); b.AddChild(new CollisionShape3D { Shape=new BoxShape3D { Size=s } });
        }
    }
    private void Village()
    {
        House("Kisaragi Workshop",new(-12,0,18),new(8,4,7),new("b8c7b1"),true);
        House("The Copper Kettle",new(12,0,23),new(8,4.5f,8),new("edd9b0"),false);
        House("Hoshikawa Clinic",new(-14,0,36),new(7,3.5f,6),new("dbe1cc"),false);
        House("Mori Farmhouse",new(14,0,39),new(7,3.5f,6),new("dcc495"),false);
        House("Village Assembly",new(-14,0,3),new(9,5,7),new("ddd3b5"),false);
        House("Seed & Grain",new(13,0,5),new(6,4,6),new("c7bb95"),true);
        Resident("Ren", "ENGINEER", "A freight train is coming in three days. Zero says its cargo is people. Clear the three patrols beyond the bridge, then restart the manual relay. We need an escape route.",new(-5,0,21.3f),0);
        Resident("Chiyo", "VILLAGE ELDER", "The cities still shine at night. But their streets belong to machines now. Here, every lantern means someone is waiting for you.",new(-7,0,6),1);
        Resident("Aki", "MEDIC", "Hold your guard against a strike, or tap it just before impact to deflect. A broken posture leaves you exposed. Let me patch you up.",new(-8,0,34),2);
        Resident("Hana", "FARMER", "KOMA keeps our irrigation running without a network. Old machines are our neighbors. The amber-eyed patrols beyond the bridge are not.",new(8,0,38),3);
        Resident("Sora", "COURIER", "I saw three sentries on the old relay road. Watch for the red diamond: that sweeping attack cannot be blocked. Dodge through it or jump away.",new(7,0,25),4);
        Resident("KOMA-7", "OFFLINE GARDENER", "Morning diagnostic: tomatoes healthy. Humans worried. Zero is still in the city. This unit is KOMA. Different machine; equally excellent company.",new(8,0,9),5);
        var well=new Node3D {Name="Village well",Position=new(-3.2f,0,24)};AddChild(well);
        Cylinder(well,"Stone well",new(0,.45f,0),1,.9f,stone);
        Cylinder(well,"Well water",new(0,.92f,0),.78f,.025f,Mat(new("275863")));
        foreach(float x in new[]{-1.2f,1.2f}) Box(well,"Well post",new(x,1.6f,0),new(.13f,3,.13f),wood);
        Box(well,"Well beam",new(0,3.1f,0),new(2.6f,.15f,.15f),wood);
        Cylinder(well,"Well rope",new(0,2,0),.017f,2,Mat(new("c9b581")));
        var wellBody=new StaticBody3D();well.AddChild(wellBody);
        wellBody.AddChild(new CollisionShape3D {Position=new(0,.45f,0),Shape=new CylinderShape3D {Radius=1,Height=.9f}});
        for(int i=0;i<8;i++)
        {
            float z=1+i*6; Lamp(new(-4.2f,Height(-4.2f,z),z));
            if(i%2==0) Lamp(new(4.2f,Height(4.2f,z),z));
        }
        // Rice beds and a kitchen garden give the village a working rural life.
        for(int field=0;field<2;field++)
        {
            float x=25+field*9;
            GroundStrip("Terraced rice soil",new(x,21.5f),new(x,38.5f),3.5f,Mat(new("646346")));
            for(int row=0;row<8;row++) for(int col=0;col<5;col++)
            {
                float px=x-2.5f+col*1.2f,pz=23+row*2;
                var p=new Vector3(px,Height(px,pz)+.09f,pz);
                for(int b=0;b<3;b++)
                {
                    var blade=Box(this,"Rice",p+new Vector3((b-1)*.12f,.28f,0),new(.06f,.65f,.07f),Mat(new("96a857")));
                    blade.Rotation=new(0,0,(b-1)*.2f);
                }
            }
        }
        Fence(new(-24,0,0),new(-24,0,42));
        Fence(new(23,0,1),new(23,0,13));Fence(new(23,0,17),new(23,0,42));
        Footpath(new(0,15),new(34,15),1.2f);Footpath(new(34,15),new(34,20),1.2f);
        Label(this,"OLD RELAY ROAD  ↑",new(4,2.6f,-.3f),Gold,27);
    }
    private Node3D House(string name,Vector3 pos,Vector3 size,Color wall,bool workshop,float yaw=0,int barrels=3,float signProjection=.25f)
    {
        pos.Y=Height(pos.X,pos.Z); var house=new Node3D {Name=name,Position=pos,RotationDegrees=new(0,yaw,0)}; AddChild(house); Buildings.Add(house);
        float w=size.X,h=size.Y,d=size.Z;
        Box(house,"Mossy stone foundation",new(0,.25f,0),new(w+.3f,.5f,d+.3f),stone,true);
        Box(house,"Plaster walls",new(0,h/2+.3f,0),new(w,h,d),Mat(wall),true);
        float rise=2.2f; float slope=Mathf.Atan2(rise,w/2+.65f);
        for(int s=-1;s<=1;s+=2)
        {
            var r=Box(house,"Ceramic tile roof",new(s*(w/4+.32f),h+1.35f,0),new(Mathf.Sqrt(Mathf.Pow(w/2+.65f,2)+rise*rise),.24f,d+1.4f),roof);
            r.Rotation=new(0,0,-s*slope);
            Box(house,"Roof fascia",new(s*(w/2+.65f),h+.27f,0),new(.18f,.28f,d+1.5f),wood);
            for(int k=0;k<3;k++) Box(house,"Timber stud",new(s*(w/2+.035f),h/2+.3f,-d/2+k*d/2),new(.15f,h,.16f),wood);
        }
        Box(house,"Ridge cap",new(0,h+2.5f,0),new(.24f,.22f,d+1.6f),wood);
        // Triangular gables underneath the sloping roof.
        var st=new SurfaceTool();st.Begin(Godot.Mesh.PrimitiveType.Triangles);
        foreach(float s in new[]{-1f,1f})
        { st.SetNormal(new(0,0,s)); st.AddVertex(new(-w/2,h+.3f,s*d/2));st.AddVertex(new(0,h+2.5f,s*d/2));st.AddVertex(new(w/2,h+.3f,s*d/2)); }
        var gmat=Mat(wall);gmat.CullMode=BaseMaterial3D.CullModeEnum.Disabled;Mesh(house,"Gable plaster",st.Commit(),Vector3.Zero,gmat);
        for(int k=0;k<3;k++)
        {
            float x=-w/2+k*w/2;
            Box(house,"Front timber",new(x,h/2+.3f,d/2+.045f),new(.18f,h,.14f),wood);
        }
        foreach(float y in new[]{.6f,h+.25f}) Box(house,"Horizontal beam",new(0,y,d/2+.08f),new(w,.18f,.16f),wood);
        Box(house,"Recessed door",new(0,1.45f,d/2+.09f),new(1.2f,2.3f,.12f),wood);
        Box(house,"Door window",new(0,1.8f,d/2+.16f),new(.75f,.75f,.06f),Mat(new("bcceb1")));
        Sphere(house,"Brass door handle",new(.37f,1.2f,d/2+.19f),Vector3.One*.045f,Mat(Gold));
        for(int s=-1;s<=1;s+=2)
        {
            float x=s*w*.31f;
            Box(house,"Window frame",new(x,2.35f,d/2+.11f),new(1.6f,1.5f,.17f),wood);
            Box(house,"Window glass",new(x,2.35f,d/2+.21f),new(1.35f,1.24f,.04f),Mat(new("b7d9c0"),.12f));
            Box(house,"Window mullion",new(x,2.35f,d/2+.25f),new(.08f,1.3f,.06f),wood);
            Box(house,"Window crossbar",new(x,2.35f,d/2+.25f),new(1.4f,.08f,.06f),wood);
            Box(house,"Flower box",new(x,1.55f,d/2+.4f),new(1.7f,.3f,.55f),wood);
            for(int i=0;i<6;i++) Sphere(house,"Window flowers",new(x-.65f+i*.25f,1.84f,d/2+.4f),new(.13f,.15f,.13f),Mat(new(i%2==0?"d59c81":"dcd8a0")));
        }
        Box(house,"Entry step",new(0,.17f,d/2+.65f),new(2,.3f,1.2f),stone,true);
        Box(house,"Hanging shop sign",new(0,3.05f,d/2+signProjection),new(3.3f,.6f,.12f),wood);
        var sign=Label(house,name.ToUpperInvariant(),new(0,3.07f,d/2+signProjection+.1f),Gold,25); sign.Billboard=BaseMaterial3D.BillboardModeEnum.Disabled; sign.PixelSize=.0043f;
        Box(house,"Chimney",new(-w*.3f,h+1.9f,-d*.22f),new(.75f,2.8f,.8f),stone);
        Box(house,"Chimney cap",new(-w*.3f,h+3.3f,-d*.22f),new(1,.18f,1),wood);
        if(workshop)
        {
            Box(house,"Worktable",new(w/2+1.3f,1,d/4),new(2.2f,.25f,1.2f),wood,true);
            foreach(float x in new[]{w/2+.5f,w/2+2.1f})
            {
                var ground=house.ToGlobal(new(x,0,d/4));
                float bottom=Height(ground.X,ground.Z)-pos.Y-.02f;
                Box(house,"Worktable trestle",new(x,(bottom+.89f)/2,d/4),new(.15f,.89f-bottom,.8f),wood);
            }
            for(int i=0;i<3;i++) Cylinder(house,"Salvaged cell",new(w/2+1+i*.5f,1.4f,d/4),.17f,.6f,Mat(i==1?Jade:new("79918d")));
            Box(house,"Offline solar collector",new(w/3,h+1.6f,0),new(1.8f,.15f,2.3f),Mat(new("3b6974"))).Rotation=new(0,0,-slope);
        }
        for(int i=0;i<barrels;i++) Cylinder(house,"Stored barrels",new(-w/2-.7f,.6f,-d/3+i*.9f),.4f,1.1f,wood);
        return house;
    }
    private void Resident(string name,string role,string line,Vector3 p,int variant)
    {
        bool chiyo=name=="Chiyo",koma=name=="KOMA-7",custom=chiyo||koma;
        p.Y=Height(p.X,p.Z); var n=new Node3D {Name=name,Position=p,Rotation=new(0,chiyo||variant%2==0?1.1f:-1.1f,0)}; AddChild(n);
        var body=GD.Load<PackedScene>(chiyo?"res://assets/models/chiyo.glb":koma?"res://assets/models/koma7.glb":"res://assets/models/villager.glb").Instantiate<Node3D>();
        n.AddChild(body); body.Scale=Vector3.One*(custom?1:1.03f+variant*.02f);
        var visual=new ActorVisual();n.AddChild(visual);visual.Bind(body);visual.Play("idle");
        Color[] colors={new("466b76"),new("876c88"),new("d5d9c4"),new("a17b47"),new("6c8c65"),new("77bfb2")};
        foreach(var m in ActorVisual.Descendants(body).OfType<MeshInstance3D>())
        {
            if(!custom&&(m.Name.ToString().Contains("coat",StringComparison.OrdinalIgnoreCase)||m.Name.ToString().Contains("armor",StringComparison.OrdinalIgnoreCase))) m.MaterialOverride=Mat(colors[variant]);
        }
        Label(n,name,new(0,chiyo?1.9f:koma?1.55f:2.35f,0),variant==0?Gold:new("ecedda"),29);
        var collision=new StaticBody3D {CollisionLayer=1};n.AddChild(collision);
        float height=chiyo?1.58f:koma?1.25f:1.8f;
        collision.AddChild(new CollisionShape3D { Position=new(0,height/2,0),Shape=new CapsuleShape3D { Radius=.32f,Height=height } });
        Residents.Add((name,role,line,n));
    }
    private void Lamp(Vector3 p)
    {
        Box(this,"Lantern pole",p+new Vector3(0,1.5f,0),new(.13f,3,.13f),wood);
        Box(this,"Lantern frame",p+new Vector3(0,2.7f,0),new(.54f,.73f,.54f),wood);
        Box(this,"Paper lantern",p+new Vector3(0,2.7f,.02f),new(.43f,.55f,.56f),Mat(new("f8d48c"),.35f));
        Box(this,"Lantern cap",p+new Vector3(0,3.12f,0),new(.7f,.12f,.7f),wood);
    }
    private void Fence(Vector3 a,Vector3 b)
    {
        int count=Math.Max(1,(int)a.DistanceTo(b)/2);
        for(int i=0;i<=count;i++)
        {var p=a.Lerp(b,(float)i/count);p.Y=Height(p.X,p.Z);Box(this,"Fence post",p+new Vector3(0,.65f,0),new(.16f,1.3f,.16f),wood,true);}
        for(int i=0;i<count;i++)
        {
            var p=a.Lerp(b,(i+.5f)/count);p.Y=Height(p.X,p.Z);
            foreach(float y in new[]{.48f,1f})
            {
                var rail=Box(this,"Fence rail",p+new Vector3(0,y,0),new(.12f,.12f,a.DistanceTo(b)/count+.1f),wood,true);
                rail.Rotation=new(0,Mathf.Atan2(b.X-a.X,b.Z-a.Z),0);
            }
        }
    }
    private void Countryside()
    {
        for(int i=0;i<140;i++)
        {
            float x=rng.RandfRange(-59,59),z=rng.RandfRange(-75,71);
            if(Math.Abs(x)<9|| (z>0&&Math.Abs(x)<40)||Math.Abs(z+8)<5||new Vector2(x-1,z+61).Length()<8)continue;
            Tree(new(x,Height(x,z),z),rng.RandfRange(.8f,1.5f));
        }
        for(int i=0;i<150;i++)
        {
            float x=rng.RandfRange(-60,60),z=rng.RandfRange(-75,65);
            if(Math.Abs(x)<5||(z>0&&Math.Abs(x)<39))continue;
            float s=rng.RandfRange(.3f,1.5f);
            var rock=Sphere(this,"Weathered boulder",new(x,Height(x,z)+s*.3f,z),new(s,s*.6f,s*.75f),stone);
            rock.Rotation=new(rng.Randf(),rng.Randf()*6,0);
        }
        var st=new SurfaceTool();st.Begin(Godot.Mesh.PrimitiveType.Triangles);
        var gm=Mat(new("7d9d53"));gm.CullMode=BaseMaterial3D.CullModeEnum.Disabled;
        for(int i=0;i<5000;i++)
        {
            float x=rng.RandfRange(-60,60),z=rng.RandfRange(-75,70);
            if(Math.Abs(x-Mathf.Sin(z*.068f)*2.4f)<3.6f||(z>0&&Math.Abs(x)<40)||Math.Abs(z+8)<4)continue;
            var p=new Vector3(x,Height(x,z),z);float h=rng.RandfRange(.18f,.6f);
            st.SetNormal(Vector3.Up);st.AddVertex(p+new Vector3(-.09f,0,0));st.AddVertex(p+new Vector3(.04f,h,.05f));st.AddVertex(p+new Vector3(.09f,0,0));
        }
        Mesh(this,"Meadow grass",st.Commit(),Vector3.Zero,gm);
        for(int i=0;i<180;i++)
        {
            float x=rng.RandfRange(-24,24),z=rng.RandfRange(-65,-17);if(Math.Abs(x)<4)continue;
            Sphere(this,"Wildflower",new(x,Height(x,z)+.18f,z),new(.09f,.09f,.09f),Mat(i%3==0?new("b8acd0"):new("e8d89b")));
        }
        var relay=new Node3D {Name="Manual relay",Position=RelayPosition};AddChild(relay);
        Box(relay,"Relay platform",new(0,.15f,0),new(5,.3f,4),stone,true);
        Box(relay,"Relay cabinet",new(0,1.3f,0),new(1.5f,2.3f,.8f),Mat(new("607e76")),true);
        Box(relay,"Relay face",new(0,1.55f,.42f),new(1.2f,.8f,.04f),Mat(Ink));
        Box(relay,"Offline terminal",new(0,1.55f,.45f),new(.8f,.46f,.025f),Mat(Jade,.8f));
        Cylinder(relay,"Antenna mast",new(0,4.1f,-.4f),.09f,6,Mat(new("829b91")));
        for(int i=0;i<3;i++)Box(relay,"Antenna crossbar",new(0,5.5f+i*.55f,-.4f),new(3-i*.6f,.06f,.06f),Mat(new("829b91")));
        Label(relay,"MANUAL RELAY",new(0,3.15f,0),Jade,30);
        // Broken rail viaduct, a reminder of the route the first chapter will reopen.
        for(int i=0;i<9;i++)
        {
            float x=-38+i*9;Box(this,"Viaduct pillar",new(x,5,-73),new(2,10,2.5f),stone);
            if(i!=4)Box(this,"Viaduct span",new(x+3.8f,10,-73),new(9,1,3.6f),stone);
        }
        for(int i=0;i<5;i++) Lamp(new(4.5f,Height(4.5f,-19-i*8),-19-i*8));
    }
    private void NorthernValley()
    {
        var water = new ShaderMaterial { Shader=GD.Load<Shader>("res://assets/shaders/water.gdshader") };
        Mesh(this,"Cedar River",new PlaneMesh { Size=new(126,7.5f),SubdivideWidth=64 },new(0,.7f,-111),water);
        var bridge=new Node3D {Name="Cedar Bridge",Position=new(RoadX(-111),0,-111)};AddChild(bridge);
        for(int i=0;i<33;i++) Box(bridge,"Cedar bridge plank",new(0,4,-8+i*.5f),new(6,.2f,.47f),wood,true);
        foreach(float side in new[]{-1f,1f})
        {
            Box(bridge,"Bridge rail",new(side*2.9f,5.2f,0),new(.14f,.14f,16.5f),wood,true);
            for(int i=0;i<9;i++) Box(bridge,"Bridge post",new(side*2.9f,4.7f,-8+i*2),new(.18f,1.4f,.18f),wood);
            for(int i=0;i<3;i++) Box(bridge,"Masonry support",new(side*2.15f,2.2f,-6+i*6),new(.8f,3.6f,1.2f),stone);
        }
        Label(bridge,"CEDAR RIVER",new(0,6.6f,0),Gold,29);
        var shelter=new Node3D {Name="Cedar Pass Shelter",Position=new(-12,Height(-12,-91),-91)};AddChild(shelter);
        Box(shelter,"Shelter floor",new(0,-.12f,0),new(6,.3f,4.5f),stone,true);
        foreach(float x in new[]{-2.6f,2.6f})foreach(float z in new[]{-1.8f,1.8f})
            Box(shelter,"Shelter pillar",new(x,1.6f,z),new(.2f,3.2f,.2f),wood,true);
        foreach(float side in new[]{-1f,1f})
            Box(shelter,"Shelter tiled roof",new(side*1.65f,3.5f,0),new(3.7f,.2f,5.5f),roof).Rotation=new(0,0,-side*.38f);
        Box(shelter,"Resting bench",new(0,.6f,-1.4f),new(4,.2f,.6f),wood,true);
        Box(shelter,"Bench back",new(0,1.1f,-1.65f),new(4,.55f,.12f),wood);
        foreach(float x in new[]{-1.5f,1.5f})Box(shelter,"Bench support",new(x,.25f,-1.4f),new(.18f,.5f,.5f),wood);
        for(int i=0;i<3;i++)Cylinder(shelter,"Rain barrel",new(3.4f,.6f,-1+i*1.1f),.42f,1.2f,wood);
        Label(shelter,"CEDAR PASS SHELTER",new(0,3.1f,2),Gold,26);
        Footpath(new(RoadX(-89),-89),new(-12,-87),1.2f);
        Footpath(new(-12,-87),new(-12,-88.75f),1.2f);
        var lookout=new Node3D {Name="Valley Overlook",Position=new(29,Height(29,-126),-126)};AddChild(lookout);
        Box(lookout,"Overlook deck",new(0,-.12f,0),new(6,.3f,5),wood,true);
        for(int i=0;i<4;i++) Box(lookout,"Lookout post",new(-3+i*2,.6f,2.5f),new(.16f,1.2f,.16f),wood,true);
        Box(lookout,"Lookout rail",new(0,1.1f,2.5f),new(6.2f,.15f,.15f),wood,true);
        Box(lookout,"Lookout east rail",new(3,1.1f,0),new(.15f,.15f,5),wood,true);
        Box(lookout,"Lookout corner post",new(3,.6f,-2.5f),new(.16f,1.2f,.16f),wood,true);
        Box(lookout,"Lookout bench",new(1.9f,.5f,-.6f),new(.7f,.22f,2.6f),wood,true);
        Box(lookout,"Lookout bench back",new(2.2f,.9f,-.6f),new(.12f,.65f,2.6f),wood);
        foreach(float z in new[]{-1.5f,.3f}) Box(lookout,"Bench leg",new(1.9f,.24f,z),new(.5f,.48f,.16f),wood);
        Box(lookout,"Vista plaque post",new(-2, .65f,-2),new(.12f,1.3f,.12f),wood,true);
        Box(lookout,"Vista plaque",new(-2,1.25f,-2),new(1.8f,.5f,.1f),wood);
        var plaque=Label(lookout,"CEDAR RIVER\nTHE OFFLINE VALLEY",new(-2,1.25f,-1.93f),Gold,24);
        plaque.Billboard=BaseMaterial3D.BillboardModeEnum.Disabled;plaque.PixelSize=.0028f;
        Footpath(new(RoadX(-126),-126),new(26,-126),1.2f);
        foreach(int z in new[]{-79,-92,-101,-122,-132,-138})
        {float x=RoadX(z)+4.3f;Lamp(new(x,Height(x,z),z));}
        Signpost(new(RoadX(-80)+4,-80),"N · KOMOREBI","S · HOSHIKAWA");
        Signpost(new(RoadX(-123)-4,-123),"N · KOMOREBI","E · OVERLOOK");
        for(int i=0;i<180;i++)
        {
            float x=rng.RandfRange(-59,59),z=rng.RandfRange(-193,-80);
            if(NorthClear(x,z)||new Vector2(x,z).DistanceTo(Geometry2D.GetClosestPointToSegment(new(x,z),new(29,-124),new(2,-106)))<7)continue;
            Tree(new(x,Height(x,z),z),rng.RandfRange(.75f,1.35f));
        }
        for(int i=0;i<70;i++)
        {
            float x=rng.RandfRange(-59,59),z=rng.RandfRange(-192,-80);
            if(NorthClear(x,z))continue;
            float s=rng.RandfRange(.35f,1.3f);
            Sphere(this,"Pass boulder",new(x,Height(x,z)+s*.3f,z),new(s,s*.7f,s),stone);
        }
        var grass=new SurfaceTool();grass.Begin(Godot.Mesh.PrimitiveType.Triangles);
        for(int i=0;i<6500;i++)
        {
            float x=rng.RandfRange(-60,60),z=rng.RandfRange(-193,-80);
            if(NorthClear(x,z))continue;
            var p=new Vector3(x,Height(x,z),z);float h=rng.RandfRange(.18f,.55f);
            grass.SetNormal(Vector3.Up);grass.AddVertex(p+new Vector3(-.1f,0,0));grass.AddVertex(p+new Vector3(0,h,.05f));grass.AddVertex(p+new Vector3(.1f,0,0));
        }
        var gm=Mat(new("8b9d58"));gm.CullMode=BaseMaterial3D.CullModeEnum.Disabled;
        Mesh(this,"Cedar Pass meadow",grass.Commit(),Vector3.Zero,gm);
        for(int i=0;i<95;i++)
        {
            float z=rng.RandfRange(-135,-80),x=RoadX(z)+(i%2==0?1:-1)*rng.RandfRange(4,6.5f);
            if(Math.Abs(z+111)<10)continue;
            Sphere(this,"Pass wildflowers",new(x,Height(x,z)+.16f,z),new(.13f,.13f,.13f),Mat(new(i%3==0?"dab298":"dcd6a3")));
        }
    }
    private static bool NorthClear(float x,float z)
        => Math.Abs(x-RoadX(z))<8 || (z<-134&&Math.Abs(x)<43) || Math.Abs(z+111)<7
            || (Math.Abs(z+90)<6&&x>-20&&x<17) || (Math.Abs(z+125)<5&&x>0&&x<34);

    private void Footpath(Vector2 a,Vector2 b,float halfWidth)
        => GroundStrip("Connecting footpath",a,b,halfWidth,Atlas(new(1,0),Vector2.One));

    private void GroundStrip(string name,Vector2 a,Vector2 b,float halfWidth,Material material)
    {
        int steps=Math.Max(1,(int)a.DistanceTo(b)*2);
        int across=Math.Max(1,(int)Math.Ceiling(halfWidth*2));
        Vector2 side=new(-(b-a).Normalized().Y,(b-a).Normalized().X);
        var st=new SurfaceTool();st.Begin(Godot.Mesh.PrimitiveType.Triangles);
        void V(Vector2 p){st.SetNormal(Vector3.Up);st.SetUV(p*.22f);st.AddVertex(new(p.X,Height(p.X,p.Y)+.045f,p.Y));}
        for(int i=0;i<steps;i++)for(int j=0;j<across;j++)
        {
            var p=a.Lerp(b,(float)i/steps);var q=a.Lerp(b,(float)(i+1)/steps);
            var left=side*Mathf.Lerp(-halfWidth,halfWidth,(float)j/across);
            var right=side*Mathf.Lerp(-halfWidth,halfWidth,(float)(j+1)/across);
            V(p+left);V(q+left);V(p+right);
            V(p+right);V(q+left);V(q+right);
        }
        Mesh(this,name,st.Commit(),Vector3.Zero,material);
    }
    private void Signpost(Vector2 p,string north,string south)
    {
        var sign=new Node3D {Name="Valley wayfinder",Position=new(p.X,Height(p.X,p.Y),p.Y)};AddChild(sign);
        Box(sign,"Sign post",new(0,1.3f,0),new(.17f,2.6f,.17f),wood);
        Box(sign,"Direction boards",new(0,2.15f,0),new(3.5f,.9f,.18f),wood);
        foreach(float side in new[]{-1f,1f})
        {
            var lettering=Label(sign,north+"\n"+south,new(0,2.17f,side*.12f),Gold,29);
            lettering.Billboard=BaseMaterial3D.BillboardModeEnum.Disabled;
            lettering.Rotation=new(0,side<0?Mathf.Pi:0,0);
        }
    }
    private void Komorebi()
    {
        var lodge=House("Komorebi Lodge",new(-13,0,-143),new(8,4.1f,7),new("e6c89e"),false,yaw:55,barrels:1,signProjection:1.7f);
        var clinic=House("Mountain Clinic",new(12,0,-148),new(7,3.7f,7),new("d7ded0"),false,yaw:-65,barrels:0);
        House("Weavers House",new(-14,0,-163),new(8,4.3f,7),new("c8b6aa"),true,yaw:35,barrels:2);
        var store=House("Orchard Store",new(14,0,-164),new(8,3.7f,7),new("ddc591"),false,yaw:-25,barrels:2,signProjection:1.7f);
        House("Komorebi Meeting Hall",new(-3,0,-178),new(11,5,8),new("d8cfad"),false,barrels:0);
        // Give the services a recognizable frontage without duplicating the house kit.
        Box(lodge,"Lodge awning",new(0,2.9f,4.3f),new(4,.12f,1.7f),Mat(new("a8654f")));
        foreach(float x in new[]{-1.9f,1.9f})Box(lodge,"Awning support",new(x,1.5f,5),new(.12f,3,.12f),wood,true);
        Box(lodge,"Travel chest",new(3,.4f,4.3f),new(1.4f,.8f,.8f),wood,true);
        Box(clinic,"Medic marker post",new(-4.15f,1.55f,3.84f),new(.13f,3.1f,.13f),wood);
        Box(clinic,"Medic banner",new(-4.15f,2.2f,3.9f),new(.7f,1.6f,.05f),Mat(new("edf0d8")));
        Box(clinic,"Medic cross vertical",new(-4.15f,2.3f,3.94f),new(.14f,.65f,.04f),Mat(new("638d75")));
        Box(clinic,"Medic cross horizontal",new(-4.15f,2.3f,3.95f),new(.47f,.14f,.04f),Mat(new("638d75")));
        Box(store,"Produce awning",new(0,2.8f,4.3f),new(4.8f,.12f,1.7f),Mat(new("7c9874")));
        var packing=new Node3D {Name="Orchard packing station",Position=new(18,Height(18,-157),-157)};AddChild(packing);
        Box(packing,"Packing table",new(0,.85f,0),new(2,.16f,1),wood,true);
        foreach(float x in new[]{-.8f,.8f})Box(packing,"Table trestle",new(x,.4f,0),new(.15f,.8f,.8f),wood);
        for(int i=0;i<3;i++)
        {
            Box(packing,"Produce tray",new(-.65f+i*.65f,.98f,0),new(.55f,.14f,.7f),wood);
            for(int j=0;j<3;j++)Sphere(packing,"Harvested persimmon",new(-.65f+i*.65f,1.12f,-.22f+j*.22f),Vector3.One*.12f,Mat(new("d49950")));
        }
        Resident("Nao","LODGE KEEPER","Welcome to Komorebi, Mio. Travelers from Hoshikawa take the cedar bridge below the orchards. There is always a place by our stove when the mountain mist rolls in.",new(-8,0,-138),0);
        Resident("Emi","MOUNTAIN MEDIC","That pass is a long walk. Sit for a moment: I can treat your wounds and refill your remedies. The clinic keeps its doors open for both villages.",new(6,0,-145),2);
        Resident("Toma","ORCHARD KEEPER","These trees fed our grandparents before the city grid reached the valley. Now they feed our neighbors again. Follow the south lanterns to the bridge if you are returning to Hoshikawa.",new(16.2f,0,-156.7f),3);
        Resident("Yui","PASS COURIER","I carry letters between our villages. The rain shelter is south of the cedar bridge; the overlook is on its north bank. Ren will be waiting back in Hoshikawa if your relay work is finished.",new(-8.5f,0,-157),4);
        Footpath(new(0,-138),new(-7,-138),1.3f);Footpath(new(-7,-138),new(-8.6f,-140),1.3f);
        Footpath(new(0,-146),new(7,-146),1.3f);
        Footpath(new(0,-155),new(-10,-155),1.3f);Footpath(new(-10,-155),new(-11,-158),1.3f);
        Footpath(new(0,-154),new(22,-154),1.3f);Footpath(new(12,-154),new(11.7f,-159),1.3f);
        Footpath(new(0,-170),new(-3,-172.5f),1.5f);
        var square=new Node3D {Name="Komorebi Square",Position=KomorebiCenter};AddChild(square);
        Cylinder(square,"Village water basin",new(-4,.45f,3),1,.9f,stone);
        Cylinder(square,"Basin water",new(-4,.92f,3),.8f,.04f,Mat(new("527d81")));
        var wellBody=new StaticBody3D {Position=new(-4,.45f,3)};square.AddChild(wellBody);
        wellBody.AddChild(new CollisionShape3D {Shape=new CylinderShape3D {Radius=1,Height=.9f}});
        foreach(var p in new[]{new Vector3(-7,0,4.5f),new Vector3(-7,0,-5)})
        {
            Box(square,"Square bench",p+new Vector3(0,.5f,0),new(.7f,.22f,2.6f),wood,true);
            Box(square,"Bench back",p+new Vector3(-.3f,.9f,0),new(.12f,.6f,2.6f),wood);
            foreach(float z in new[]{-.9f,.9f})Box(square,"Bench leg",p+new Vector3(0,.24f,z),new(.5f,.48f,.16f),wood);
        }
        foreach(var p in new[]{new Vector2(-4.5f,-136),new Vector2(5,-143),new Vector2(-6,-148),new Vector2(6,-157),new Vector2(-6,-167),new Vector2(5,-171),new Vector2(20,-152)})
            Lamp(new(p.X,Height(p.X,p.Y),p.Y));
        foreach(var p in new[]{new Vector2(-20,-149),new Vector2(8,-171),new Vector2(-18,-176)})
        {
            Tree(new(p.X,Height(p.X,p.Y),p.Y),.72f);
            for(int i=0;i<9;i++)
            {
                float x=p.X+Mathf.Sin(i*2.4f)*1.6f,z=p.Y+Mathf.Cos(i*2.4f)*1.6f;
                Sphere(this,"Courtyard planting",new(x,Height(x,z)+.25f,z),new(.35f,.3f,.35f),Mat(new("879c68")));
            }
        }
        foreach(float z in new[]{-134f,-170f})
        {
            foreach(float x in new[]{-4.5f,4.5f}) Box(this,"Festival mast",new(x,Height(x,z)+2.4f,z),new(.15f,4.8f,.15f),wood);
            Box(this,"Bunting rope",new(0,Height(0,z)+4.5f,z),new(9,.035f,.035f),wood);
            for(int i=0;i<9;i++)
                Box(this,"Cloth pennant",new(-3.8f+i*.95f,Height(0,z)+4.15f,z),new(.55f,.65f,.025f),Mat(new(i%2==0?"ba795d":"7ba89c")));
        }
        Signpost(new(5,-135),"KOMOREBI","LODGE · CLINIC · ORCHARDS");
        for(int row=0;row<3;row++)for(int col=0;col<3;col++)
        {
            float x=25+col*6,z=-146-row*9;float y=Height(x,z);
            Tree(new(x,y,z),.72f);
            for(int fruit=0;fruit<7;fruit++)
            {
                var fruitPos=new Vector3(x+Mathf.Sin(fruit*2.4f)*1.25f,y+1.72f+(fruit%3)*.04f,z+.5f+Mathf.Cos(fruit*2.4f)*1.2f);
                Cylinder(this,"Persimmon stem",fruitPos+new Vector3(0,.31f,0),.02f,.4f,wood);
                Sphere(this,"Orchard persimmon",fruitPos,Vector3.One*.17f,Mat(new("d49950")));
            }
        }
        Footpath(new(0,-153),new(-12,-151.5f),1.2f);Footpath(new(-12,-151.5f),new(-38.5f,-151.5f),1.2f);
        Footpath(new(-32.5f,-142),new(-32.5f,-161),.8f);
        Footpath(new(22,-154),new(24,-151),1.2f);Footpath(new(24,-151),new(34,-151),1.2f);
        Footpath(new(28,-141),new(28,-167),.8f);
        for(int field=0;field<2;field++)foreach(float z in new[]{-146f,-157f})
        {
            float x=-29-field*7;
            GroundStrip($"Komorebi garden bed {field}-{(int)-z}",new(x,z-4),new(x,z+4),2.5f,Mat(new("716348")));
            for(int row=0;row<4;row++)for(int col=0;col<4;col++)
            {
                float px=x-1.8f+col*1.2f,pz=z-2.7f+row*1.8f;
                Sphere(this,"Leafy crop",new(px,Height(px,pz)+.22f,pz),new(.35f,.25f,.35f),Mat(new(field==0?"8eaa63":"728f60")));
            }
        }
        Fence(new(-24,0,-139),new(-24,0,-149.5f));Fence(new(-24,0,-153.5f),new(-24,0,-171));
        Fence(new(22,0,-139),new(22,0,-152));Fence(new(22,0,-156),new(22,0,-173));
        Fence(new(-24,0,-171),new(-20,0,-171));Fence(new(10,0,-173),new(22,0,-173));
    }
    private void Tree(Vector3 p,float scale)
    {
        var t=new Node3D {Position=p,Scale=Vector3.One*scale};AddChild(t);
        Cylinder(t,"Tree trunk",new(0,2,0),.25f,4,wood,.15f);
        var foliage=Mat(new Color(rng.RandfRange(.22f,.36f),rng.RandfRange(.38f,.52f),rng.RandfRange(.19f,.27f)));
        for(int j=0;j<4;j++) Sphere(t,"Layered canopy",new(Mathf.Sin(j*2)*.9f,3.6f+j*.65f,Mathf.Cos(j*2)*.7f),new(2.1f-j*.18f,1.5f,1.9f-j*.15f),foliage);
        var b=new StaticBody3D();t.AddChild(b);b.AddChild(new CollisionShape3D {Position=new(0,1.6f,0),Shape=new CylinderShape3D {Radius=.28f,Height=3.2f}});
    }
}
