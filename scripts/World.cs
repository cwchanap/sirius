using Godot;
namespace Hoshikawa;

public partial class World : Node3D
{
    public readonly List<Node3D> Buildings = new();
    public readonly List<(string Name, string Role, Node3D Body)> Residents = new();
    // Residents with their own Tripo GLB: grounded model height (also the capsule) and name label height. The rest share villager.glb.
    public static readonly Dictionary<string, (string Asset, float Height, float Label)> TripoResidents = new()
    {
        ["Chiyo"]=("chiyo",1.58f,1.9f), ["KOMA-7"]=("koma7",1.25f,1.55f), ["Sora"]=("sora",1.78f,2.1f), ["Nao"]=("nao",1.64f,1.96f), ["Toma"]=("toma",1.74f,2.06f),
    };
    public const int NorthEdge = -200, SouthEdge = 80, HalfWidth = 64;
    public static Vector3 KomorebiCenter => new(0, Height(0,-154), -154);
    public static float RoadX(float z)
    {
        // Keep the terrain shader's road curve in sync with this placement guide.
        float original = Mathf.Sin(z*.068f)*2.4f;
        float pass = 10*Mathf.Sin((-z-72)*.045f);
        return Mathf.Lerp(original, pass, Mathf.SmoothStep(0,12,-z-72)) * (1-Mathf.SmoothStep(0,12,-z-134));
    }
    public static readonly (string Name,Vector2 At,Vector3 Size,float Yaw,string Style)[] Hamlet={
        ("Kisaragi Workshop",new(-10.37f,6.63f),new(9.2f,3.61f,7),75,"workshop"),("Seed & Grain",new(12.5f,6),new(6.2f,3.24f,3.67f),-95,"kura"),
        ("Village Assembly",new(-12.8f,21.78f),new(10.82f,3.6f,7.4f),82,"hall"),("The Copper Kettle",new(12,24),new(8,4.03f,7),-80,"teahouse"),
        ("Hoshikawa Clinic",new(-11.05f,37.54f),new(10,3.62f,5.68f),98,"clinic"),("Mori Farmhouse",new(11.81f,42.27f),new(10.9f,2.9f,7.1f),-70,"farmhouse")};
    // Komorebi reuses the Hoshikawa styles and their sizes. Its ground is level within |x| < 29 and z -182…-132, so it needs no pads;
    // the hall faces down the road from its north end.
    public static readonly (string Name,Vector2 At,Vector3 Size,float Yaw,string Style)[] KomorebiHouses={
        ("Komorebi Lodge",new(-12,-141),new(10.9f,2.9f,7.1f),90,"farmhouse"),("Mountain Clinic",new(12,-146),new(10,3.62f,5.68f),-90,"clinic"),
        ("Weavers House",new(-12.5f,-162),new(9.2f,3.61f,7),90,"workshop"),("Orchard Store",new(13,-161.2f),new(6.2f,3.24f,3.67f),-90,"kura"),
        ("Komorebi Meeting Hall",new(0,-177),new(10.82f,3.6f,7.4f),0,"hall")};
    public static readonly Vector2 ShrineCenter=new(-1,63);
    public static readonly (Vector2 From,Vector2 To)[] Footpaths={(new(0,15),new(17.2f,15)),(new(21.4f,15),new(34,15)),(new(34,15),new(34,20))};
    public static bool OnFootpath(Vector2 p,float margin)=>Footpaths.Any(f=>Geometry2D.GetClosestPointToSegment(p,f.From,f.To).DistanceTo(p)<1.2f+margin);
    private static readonly float[] PadLevel=Hamlet.Select(h=>HamletGround(h.At.X,h.At.Y,BaseHeight(h.At.X,h.At.Y),false)).ToArray();
    // The four rice paddies, terraced: Height() levels each 6 cm under the lowest ground it covers, and RiceFields() raises the aze between them.
    public static readonly Rect2[] Paddies={new(24.4f,21.2f,6.2f,8.4f),new(31.4f,21.2f,6.2f,8.4f),new(24.4f,30.4f,6.2f,8.4f),new(31.4f,30.4f,6.2f,8.4f)};
    private static readonly float[] PaddyLevel=Paddies.Select(p=>Enumerable.Range(0,64).Min(i=>
        {float x=p.Position.X+p.Size.X*(i%8)/7,z=p.Position.Y+p.Size.Y*(i/8)/7;return HamletGround(x,z,BaseHeight(x,z),true);})-.06f).ToArray();
    // The Hoshikawa runs at sea level through the valley, an estuary from its falls to the sea; the Cedar River runs higher, in its ravine.
    public const float ShrineLevel=2.4f,WaterLevel=-.38f,CedarLevel=.7f;
    // The brook's surface drifts toward its mouth at this many m/s; the water wheel's rim turns at the same speed.
    public const float BrookFlow=.45f,WheelRadius=1.3f;
    // The water wheel's axle, in the brook beside the sluice, low enough that its rim dips .3 m under the water.
    public static readonly Vector3 WheelAxle=new(19.5f,WaterLevel-.3f+WheelRadius,36.8f);
    // The scarecrow's pole, in paddy 1.
    public static readonly Vector2 Scarecrow=new(33.6f,25.8f);
    // The boat's mooring on the river, off the village bank's low gravel shore, and the washing steps' centre on its higher bank at x 10,
    // both clear of the bridge and the brook's mouth; reeds and river stones keep off both.
    public static readonly Vector2 Mooring=new(-13,-5.6f),Kawabata=new(10,-4.4f);
    public static bool Landing(Vector2 p,float margin)=>(Math.Abs(p.X-Mooring.X)<2.8f+margin||Math.Abs(p.X-Kawabata.X)<1.8f+margin)&&p.Y>-7.5f-margin&&p.Y<-2+margin;
    // Roofs stop the camera's spring arm on this layer alone; the player and robots only collide with layer 1.
    public const int CameraLayer=8;
    public static void CameraStop(Node3D n,Shape3D shape){var b=new StaticBody3D{CollisionLayer=CameraLayer,CollisionMask=0};n.AddChild(b);b.AddChild(new CollisionShape3D{Shape=shape});}
    // Height(), the water strip, the footbridge and the minimap all follow this one brook line.
    public static float BrookX(float z)=>19.3f+.6f*Mathf.Sin(z*.16f);
    // Wetness, 0..1, that terrain.gdshader paints as damp earth and gravel: rising from .35 m above the river, brook, Cedar River, sea and gorge waterlines to them, and half wet across the paddy floors.
    // Beyond the play area the band reaches `band` up instead, which Ground() raises on steep rock so it is never thinner than a cell.
    public static float Shore(float x,float z,float band=.35f)
    {
        if(!InPlay(x,z))return WaterOutside(x,z) is float level?1-Mathf.SmoothStep(.05f,band,Height(x,z)-level):0;
        if(Paddies.Any(p=>p.HasPoint(new(x,z))))return .5f;
        bool cedar=z<-60,near=cedar?Math.Abs(z+111)<7:Math.Abs(z+8)<6||(z>-6&&z<57.5f&&Math.Abs(x-BrookX(z))<3.2f);
        return near?1-Mathf.SmoothStep(.05f,.35f,Height(x,z)-(cedar?CedarLevel:WaterLevel)):0;
    }
    // The rendered terrain between its 1 m vertices: Terrain() splits each cell on the (x+1,z)-(x,z+1) diagonal.
    public static float MeshHeight(float x,float z)
    {
        int x0=Mathf.FloorToInt(x),z0=Mathf.FloorToInt(z);float fx=x-x0,fz=z-z0;
        return fx+fz<=1?Height(x0,z0)+fx*(Height(x0+1,z0)-Height(x0,z0))+fz*(Height(x0,z0+1)-Height(x0,z0))
            :Height(x0+1,z0+1)+(1-fx)*(Height(x0,z0+1)-Height(x0+1,z0+1))+(1-fz)*(Height(x0+1,z0)-Height(x0+1,z0+1));
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
        Environment(); Terrain(); Village(); Countryside(); NorthernValley(); Komorebi(); Merge();
    }
    // The static cache would otherwise hold its materials past the engine's leak check at quit.
    public override void _ExitTree() => looks.Clear();
    // Each static box, sphere and cylinder would cost a draw call, plus another in the sun's shadow pass, so their geometry is baked
    // into one mesh per material and 32 m cell. The originals stay, hidden, for their colliders and IntegrationCheck's lookups.
    private void Merge()
    {
        var groups=new Dictionary<(Material,GeometryInstance3D.ShadowCastingSetting,float,float),List<MeshInstance3D>>();
        foreach(var n in FindChildren("*","MeshInstance3D",true,false).Cast<MeshInstance3D>())
        {
            bool plain=true;for(var p=n.GetParent();p!=this;p=p.GetParent())plain&=p.GetType()==typeof(Node3D);
            // Transparent surfaces sort per instance; merged, they would blend in mesh order instead.
            if(!plain||!(n.Mesh is PrimitiveMesh||propMeshes.Contains(n.Mesh))||n.MaterialOverride==null||n.Mesh is PlaneMesh||n.MaterialOverride is BaseMaterial3D{Transparency:not BaseMaterial3D.TransparencyEnum.Disabled}||n.FindChildren("*","VisualInstance3D",true,false).Count>0)continue;
            var cell=(n.GlobalPosition/32).Floor();var key=(n.MaterialOverride,n.CastShadow,cell.X,cell.Z);
            if(!groups.TryGetValue(key,out var list))groups[key]=list=new();
            list.Add(n);
        }
        foreach(var list in groups.Values)
        {
            var v=new List<Vector3>();var normals=new List<Vector3>();var uv=new List<Vector2>();var index=new List<int>();
            foreach(var n in list)
            {
                // Normals take the inverse transpose: the spheres are scaled unevenly.
                var a=n.Mesh.SurfaceGetArrays(0);var x=GlobalTransform.AffineInverse()*n.GlobalTransform;var nx=x.Basis.Inverse().Transposed();int first=v.Count;
                v.AddRange(a[(int)Godot.Mesh.ArrayType.Vertex].AsVector3Array().Select(p=>x*p));normals.AddRange(a[(int)Godot.Mesh.ArrayType.Normal].AsVector3Array().Select(d=>(nx*d).Normalized()));
                uv.AddRange(a[(int)Godot.Mesh.ArrayType.TexUV].AsVector2Array());index.AddRange(a[(int)Godot.Mesh.ArrayType.Index].AsInt32Array().Select(i=>first+i));
                n.Visible=false;
            }
            var arrays=new Godot.Collections.Array();arrays.Resize((int)Godot.Mesh.ArrayType.Max);
            arrays[(int)Godot.Mesh.ArrayType.Vertex]=v.ToArray();arrays[(int)Godot.Mesh.ArrayType.Normal]=normals.ToArray();arrays[(int)Godot.Mesh.ArrayType.TexUV]=uv.ToArray();arrays[(int)Godot.Mesh.ArrayType.Index]=index.ToArray();
            var mesh=new ArrayMesh();mesh.AddSurfaceFromArrays(Godot.Mesh.PrimitiveType.Triangles,arrays);
            Mesh(this,$"Merged statics {GetChildCount()}",mesh,Vector3.Zero,list[0].MaterialOverride).CastShadow=list[0].CastShadow;
        }
    }
    public static float Height(float x, float z) => InPlay(x, z) ? Inside(x, z) : Outside(x, z);
    private static float Inside(float x, float z) => z > -9 ? PaddyGround(x, z, HamletGround(x, z, BaseHeight(x, z), true)) : BaseHeight(x, z);
    // Each paddy floor is flat to 30 cm inside its edge and back on the ground 5 cm outside it, under the aze.
    private static float PaddyGround(float x,float z,float y)
    {
        if(x<23.5f||x>38.5f||z<20.3f||z>39.7f)return y;
        for(int i=0;i<Paddies.Length;i++)
        {
            var p=Paddies[i];float d=Math.Max(Math.Max(p.Position.X-x,x-p.End.X),Math.Max(p.Position.Y-z,z-p.End.Y));
            y=Mathf.Lerp(y,PaddyLevel[i],1-Mathf.SmoothStep(-.3f,.05f,d));
        }
        return y;
    }
    private static float BaseHeight(float x, float z)
    {
        float rural = Mathf.SmoothStep(0, 45, -z);
        float y = (.38f * Mathf.Sin(x * .13f) * Mathf.Cos(z * .1f) + 2.7f * rural);
        y += 3.4f * Mathf.Exp(-((x + 22) * (x + 22) + (z + 42) * (z + 42)) / 280f);
        y -= 2.1f * Mathf.Exp(-(z + 8) * (z + 8) / 8f);
        // A low village-side bank: where the swells trough, the ground sat at the water line and gave the river sheet no shore to end in.
        y += .3f * Mathf.Exp(-(z + 4) * (z + 4) / 4f);
        // The village flattening fades in over 3 m; switching it on at z=0 left a 0.38 m step across the terrain.
        if (Math.Abs(x) < 30) y *= Mathf.Lerp(1, Mathf.SmoothStep(0, 12, Math.Abs(x) - 18), Mathf.SmoothStep(-3, 0, z));
        // The stone bridge's earth approaches, level with its deck to the terrain mesh's z -13 and -3 rows; between them the river runs
        // under the deck, and the abutments hide the one-metre slopes down to the natural banks.
        if (Math.Abs(x) < 4 && z > -16 && z < 0)
            y = Mathf.Lerp(y, .82f, (1 - Mathf.SmoothStep(3, 4, Math.Abs(x))) * Mathf.SmoothStep(0, 2, Math.Min(z + 16, -z))
                * Math.Max(1 - Mathf.SmoothStep(-13, -12.6f, z), Mathf.SmoothStep(-3.4f, -3, z)));
        if (z < -78)
        {
            y += Mathf.SmoothStep(0,18,-z-78)*(1.5f+1.1f*Mathf.Sin(x*.055f)*Mathf.Sin((z+75)*.06f));
            y += 2.8f*Mathf.Exp(-((x+29)*(x+29)+(z+116)*(z+116))/460);
            y -= 4.1f*Mathf.Exp(-(z+111)*(z+111)/17);
            // The river has cut its channel through the hill: its bed stays 55 cm under the water all the way across, in a slot that
            // leaves the water sheet's edges, 3.75 m out, in the banks.
            y = Math.Min(y, Mathf.Lerp(y, CedarLevel-.55f, 1-Mathf.SmoothStep(2,3.6f,Math.Abs(z+111))));
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
    // Hoshikawa landforms, in order: swells, west terrace, square, level house pads, brook channel, shrine mound.
    private static float HamletGround(float x, float z, float y, bool pads)
    {
        y+=.35f*Mathf.Sin(x*.21f+.6f)*Mathf.Sin(z*.17f)*Mathf.SmoothStep(3.5f,6,Math.Abs(x-RoadX(z)))*Mathf.SmoothStep(-2,4,z)*(1-Mathf.SmoothStep(15,18,x));
        y+=1.3f*Mathf.SmoothStep(-19.2f,-20.8f,x)*Mathf.SmoothStep(2,6,z)*(1-Mathf.SmoothStep(46,52,z));
        y=Mathf.Lerp(y,0,1-Mathf.SmoothStep(7,10,new Vector2(x,z-21).Length()));
        // A sharpened weighted mean of pad levels: continuous where aprons overlap, yet a footprint's own
        // pad dominates (w^4) so a neighbour's apron cannot tilt it; picking the strongest pad left a 0.48 m step.
        float weight=0,sum=0,level=0;
        if(pads)for(int i=0;i<Hamlet.Length;i++)
        {
            var (_,at,size,yaw,_)=Hamlet[i];var l=new Vector2(x-at.X,z-at.Y).Rotated(Mathf.DegToRad(yaw));
            float w=1-Mathf.SmoothStep(0,2.2f,Math.Max(Math.Abs(l.X)-(size.X/2+1.6f),Math.Abs(l.Y)-(size.Z/2+1.6f))),k=w*w*w*w;
            weight=Math.Max(weight,w);sum+=k;level+=k*PadLevel[i];
        }
        if(sum>0)y=Mathf.Lerp(y,level/sum,weight);
        // The channel only ever cuts down, so it also runs into the river dip south of the square without a seam; a 2.3 m bank keeps the 1 m mesh under 40 degrees.
        y=Math.Min(y,Mathf.Lerp(y,-.95f,(1-Mathf.SmoothStep(.6f,2.3f,Math.Abs(x-BrookX(z))))*(1-Mathf.SmoothStep(54,60,z))));
        // The washing steps' notch: from their top end the bank is cut to a plane under their treads, fading out under their walls.
        float down=Kawabata.Y+1.2f-z;
        y=Math.Min(y,Mathf.Lerp(y,.35f-.4f*down,(1-Mathf.SmoothStep(.6f,.95f,Math.Abs(x-Kawabata.X)))*Mathf.SmoothStep(-.3f,0,down)*(1-Mathf.SmoothStep(2.4f,2.7f,down))));
        return Mathf.Lerp(y,ShrineLevel,1-Mathf.SmoothStep(4.5f,14,new Vector2(x,z).DistanceTo(ShrineCenter)));
    }
    // One material per look, so Merge() can batch by reference: never change a material Mat() returns.
    private static readonly Dictionary<(Color, float, bool), StandardMaterial3D> looks = new();
    public static StandardMaterial3D Mat(Color c, float emission = 0, bool twoSided = false)
    {
        if (!looks.TryGetValue((c, emission, twoSided), out var m))
            looks[(c, emission, twoSided)] = m = new() { AlbedoColor = c, Roughness = .85f, EmissionEnabled = emission > 0,
                Emission = c, EmissionEnergyMultiplier = emission, CullMode = twoSided ? BaseMaterial3D.CullModeEnum.Disabled : BaseMaterial3D.CullModeEnum.Back };
        return m;
    }
    private ShaderMaterial Atlas(Vector2 cell, Vector2 repeats, string texture = "res://assets/art/material-atlas.png")
    {
        var m = new ShaderMaterial { Shader = GD.Load<Shader>("res://assets/shaders/atlas.gdshader") };
        m.SetShaderParameter("atlas", GD.Load<Texture2D>(texture));
        m.SetShaderParameter("cell", cell); m.SetShaderParameter("repeats", repeats); return m;
    }
    public static MeshInstance3D Mesh(Node3D parent, string name, Mesh mesh, Vector3 pos, Material material)
    {
        var n = new MeshInstance3D { Name = name, Mesh = mesh, Position = pos, MaterialOverride = material };
        parent.AddChild(n); return n;
    }
    // Blender props (tools/build_village_props.py) carry material names only. Each surface becomes its own single-surface
    // mesh under one shared material, so Merge() bakes the props into the same per-cell meshes as the primitives.
    private readonly Dictionary<string,(ArrayMesh Mesh,Material Material)[]> props=new();
    private readonly HashSet<Mesh> propMeshes=new();
    private readonly Dictionary<string,Material> propMaterials=new();
    private MeshInstance3D[] Prop(Node3D parent,string name,string model,Vector3 pos,float yaw=0,Vector3? scale=null)
    {
        if(!props.TryGetValue(model,out var parts))
        {
            var scene=GD.Load<PackedScene>($"res://assets/models/props/{model}.glb").Instantiate<Node3D>();var source=ActorVisual.Descendants(scene).OfType<MeshInstance3D>().Single().Mesh;
            props[model]=parts=Enumerable.Range(0,source.GetSurfaceCount()).Select(i=>
            {
                var part=new ArrayMesh{ResourceName="prop "+model};part.AddSurfaceFromArrays(Godot.Mesh.PrimitiveType.Triangles,source.SurfaceGetArrays(i));propMeshes.Add(part);
                return (part,PropMaterial(source.SurfaceGetMaterial(i)?.ResourceName??""));
            }).ToArray();
            scene.Free();
        }
        return parts.Select(p=>{var n=Mesh(parent,name,p.Mesh,pos,p.Material);n.Rotation=new(0,yaw,0);n.Scale=scale??Vector3.One;return n;}).ToArray();
    }
    private Material PropMaterial(string name)
    {
        if(!propMaterials.TryGetValue(name,out var m))
            propMaterials[name]=m=name switch{"cedar"=>VillageAtlas(new(1,0),Vector2.One,new(1.55f,1.5f,1.45f)),"paper"=>Mat(new("f8d48c"),.35f),"stone"=>stone,
                "straw"=>Mat(new("d6bd78")),"leaf"=>Mat(new("8eaa63")),"leaf_dark"=>Mat(new("728f60")),"root"=>Mat(new("ebe5d3")),_=>TreeMaterial(name)};
        return m;
    }
    // Tripo props (assets/models/NAME.glb) are placed whole, unmerged. Their albedo comes out darker than the village atlas,
    // so one tinted copy of the model's material, shared by its instances, lifts it; the tint is the per-model brightness knob.
    private readonly Dictionary<string,Material> modelMaterials=new();
    private Node3D Model(Node3D parent,string name,string model,Vector3 pos,float yaw,Color tint)
    {
        var n=new Node3D{Name=name,Position=pos,RotationDegrees=new(0,yaw,0)};parent.AddChild(n);
        var scene=GD.Load<PackedScene>($"res://assets/models/{model}.glb").Instantiate<Node3D>();n.AddChild(scene);
        var mesh=ActorVisual.Descendants(scene).OfType<MeshInstance3D>().Single();
        if(!modelMaterials.TryGetValue(model,out var m)){var copy=(BaseMaterial3D)mesh.Mesh.SurfaceGetMaterial(0).Duplicate();copy.AlbedoColor=tint;modelMaterials[model]=m=copy;}
        mesh.MaterialOverride=m;return n;
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
        // The Mobile renderer has no SSAO, SDFGI or volumetric fog; depth fog, sky ambient, glow and adjustments all run there.
        var skyMaterial = new ProceduralSkyMaterial
        {
            SkyTopColor = new("4d83b0"), SkyHorizonColor = new("e6dcc4"),
            GroundBottomColor = new("4b5440"), GroundHorizonColor = new("dacfb2"), SkyCurve = .16f, SunAngleMax = 24
        };
        AddChild(new WorldEnvironment { Environment = new Godot.Environment
        {
            BackgroundMode = Godot.Environment.BGMode.Sky, Sky = new Sky { SkyMaterial = skyMaterial },
            AmbientLightSource = Godot.Environment.AmbientSource.Sky, AmbientLightColor = new("d9ccb0"), AmbientLightSkyContribution = .65f, AmbientLightEnergy = .6f,
            ReflectedLightSource = Godot.Environment.ReflectionSource.Sky, TonemapMode = Godot.Environment.ToneMapper.Aces,
            FogEnabled = true, FogMode = Godot.Environment.FogModeEnum.Depth, FogLightColor = new("e9dcc0"), FogDensity = .7f,
            FogDepthBegin = 25, FogDepthEnd = 250, FogDepthCurve = 1.2f, FogAerialPerspective = .4f, FogSunScatter = .08f, FogSkyAffect = .2f,
            GlowEnabled = true, GlowIntensity = .2f, GlowBloom = .03f,
            AdjustmentEnabled = true, AdjustmentContrast = 1.08f, AdjustmentSaturation = 1.1f
        }});
        AddChild(new DirectionalLight3D { RotationDegrees = new(-34, -52, 0), LightColor = new("ffdfb0"),
            LightEnergy = 1.2f, ShadowEnabled = true, DirectionalShadowMaxDistance = 100, ShadowBias = .06f });
        // The occupied city, a distant story landmark on the far eastern ridge, half lost in the haze from Cedar Pass.
        for (int i=0;i<9;i++){float x=150+i*3;Box(this,"Machine city silhouette",new(x,Height(x,-120)+6+i%3*3,-120),new(2,18+i%3*6,3),Mat(new("728e8c")));}
    }
    private void Terrain()
    {
        var material = new ShaderMaterial { Shader = GD.Load<Shader>("res://assets/shaders/terrain.gdshader") };
        material.SetShaderParameter("atlas",GD.Load<Texture2D>("res://assets/art/material-atlas.png"));
        // The 1 m terrain over the play area and its margin, with its collider, in 64 m chunks that culling can drop; then the 4 m mountains and
        // seabed round it to Far, stitched to its edge.
        var ground = new StaticBody3D { Name = "Terrain collision" }; AddChild(ground);
        for(int z=Fine.Position.Y;z<Fine.End.Y;z+=64)for(int x=Fine.Position.X;x<Fine.End.X;x+=64)
        {
            var mesh=Ground(new(x,z,Math.Min(64,Fine.End.X-x),Math.Min(64,Fine.End.Y-z)),1);
            Mesh(this,$"Rolling terrain {x} {z}",mesh,Vector3.Zero,material);
            ground.AddChild(new CollisionShape3D { Shape = mesh.CreateTrimeshShape() });
        }
        for(int z=Far.Position.Y;z<Far.End.Y;z+=64)for(int x=Far.Position.X;x<Far.End.X;x+=64)
            if(Ground(new(x,z,Math.Min(64,Far.End.X-x),Math.Min(64,Far.End.Y-z)),4,true) is ArrayMesh mesh)Mesh(this,$"Mountains {x} {z}",mesh,Vector3.Zero,material);
        var water = new ShaderMaterial { Shader = GD.Load<Shader>("res://assets/shaders/water.gdshader") };water.SetShaderParameter("flow",new Vector2(-.35f,0));
        // 1 m cells, on the terrain's own columns, so the sheet's ends meet the gorge's and the sea's water vertex for vertex.
        Mesh(this,"Hoshikawa river",new PlaneMesh { Size = new(2*HalfWidth,8), SubdivideWidth = 2*HalfWidth-1 },new(0,WaterLevel,-8),water);
        var bridge=new Node3D{Name="Stone Bridge",Position=new(0,0,-8)};AddChild(bridge);
        TimberBridge(bridge,.82f,5,1.8f,new[]{-1.2f,1.2f});
        foreach(float side in new[]{-1f,1f})
        {
            float bottom=Height(0,-8+side*3.4f)-.6f;
            Box(bridge,$"Stone bridge abutment {side}",new(0,(.73f+bottom)/2,side*4.325f),new(6.6f,.73f-bottom,1.85f),stone,true);
        }
        Wilds();
    }
    // The side a coarse cell at `p` shares with the 1 m ground, if any, as its ends and the corners across the cell from them.
    public static (Vector2I A,Vector2I B,Vector2I FarA,Vector2I FarB)? FineSide(Vector2I p,int step)
    {
        Vector2I r=new(step,0),d=new(0,step);
        foreach(var (a,b,fa,fb,beyond) in new[]{(p,p+d,p+r,p+r+d,p-r),(p+r,p+r+d,p,p+d,p+r),(p,p+r,p+d,p+r+d,p-d),(p+d,p+r+d,p,p+r,p+d)})
            if(!Fine.HasPoint(p)&&Fine.HasPoint(beyond))return (a,b,fa,fb);
        return null;
    }
    // A terrain grid over `area` at `step` metres, split on the (x+step,z)-(x,z+step) diagonal like the water grids. Vertex red is
    // terrain.gdshader's dryness; green 0 outside the play area keeps the road's paint off the mountains and the beach. With `skipFine`,
    // the 4 m grid leaves out the 1 m ground and fans each cell along its edge to that ground's own vertices, so the two meet without a crack.
    private static ArrayMesh? Ground(Rect2I area,int step,bool skipFine=false)
    {
        var at=new Dictionary<Vector2I,int>();var v=new List<Vector3>();var n=new List<Vector3>();var c=new List<Color>();var uv=new List<Vector2>();var index=new List<int>();
        bool Rim(Vector2I p)=>p.X>=Fine.Position.X&&p.X<=Fine.End.X&&p.Y>=Fine.Position.Y&&p.Y<=Fine.End.Y;
        int V(Vector2I p)
        {
            if(at.TryGetValue(p,out int k))return k;
            float x=p.X,z=p.Y,e=step==1||Rim(p)?.1f:1;var normal=new Vector3(Height(x-e,z)-Height(x+e,z),2*e,Height(x,z-e)-Height(x,z+e)).Normalized();
            at[p]=k=v.Count;v.Add(new(x,Height(x,z),z));n.Add(normal);uv.Add(new(x*.15f,z*.15f));
            c.Add(new(1-Shore(x,z,Math.Max(.35f,1.2f*new Vector2(normal.X,normal.Z).Length()/normal.Y)),InPlay(x,z)?1:0,1));return k;
        }
        // Wound clockwise seen from above, the terrain's front.
        void T(Vector2I a,Vector2I b,Vector2I d){if((b.X-a.X)*(d.Y-a.Y)-(b.Y-a.Y)*(d.X-a.X)<0)(b,d)=(d,b);index.AddRange(new[]{V(a),V(b),V(d)});}
        for(int z=area.Position.Y;z<area.End.Y;z+=step)for(int x=area.Position.X;x<area.End.X;x+=step)
        {
            Vector2I p=new(x,z),r=new(step,0),d=new(0,step);
            if(skipFine&&Fine.HasPoint(p))continue;
            if(skipFine&&FineSide(p,step) is var (a,b,fa,fb))
            {
                var u=(b-a)/step;var mid=a+u*(step/2);T(fa,mid,fb);
                for(var q=a;q!=mid;q+=u)T(fa,q,q+u);
                for(var q=mid;q!=b;q+=u)T(fb,q,q+u);
            }
            else{T(p,p+r,p+d);T(p+r,p+r+d,p+d);}
        }
        if(index.Count==0)return null;
        var arrays=new Godot.Collections.Array();arrays.Resize((int)Godot.Mesh.ArrayType.Max);
        arrays[(int)Godot.Mesh.ArrayType.Vertex]=v.ToArray();arrays[(int)Godot.Mesh.ArrayType.Normal]=n.ToArray();arrays[(int)Godot.Mesh.ArrayType.Color]=c.ToArray();
        arrays[(int)Godot.Mesh.ArrayType.TexUV]=uv.ToArray();arrays[(int)Godot.Mesh.ArrayType.Index]=index.ToArray();
        var mesh=new ArrayMesh();mesh.AddSurfaceFromArrays(Godot.Mesh.PrimitiveType.Triangles,arrays);return mesh;
    }
    private void Village()
    {
        foreach(var (name,at,size,yaw,style) in Hamlet) Minka(name,at,size,yaw,style);
        float Face(float x,float z)=>Mathf.Atan2(RoadX(z)-x,0);
        Resident("Ren", "ENGINEER",new(-5.2f,0,11),0,Face(-5.2f,11));
        Resident("Chiyo", "VILLAGE ELDER",new(-6,0,20),1,Face(-6,20));
        Resident("Aki", "MEDIC",new(-5.5f,0,37.5f),2,Face(-5.5f,37.5f));
        Resident("Hana", "FARMER",new(6,0,37.5f),3,Face(6,37.5f));
        Resident("Sora", "COURIER",new(6.8f,0,22),4,Face(6.8f,22));
        Resident("KOMA-7", "OFFLINE GARDENER",new(8.3f,0,11.5f),5,Face(8.3f,11.5f));
        Well("Village well",new(-3.2f,24));
        RiceFields();
        Fence(new(-24,0,0),new(-24,0,42));
        Fence(new(23,0,1),new(23,0,13));Fence(new(23,0,17),new(23,0,42));
        foreach(var (from,to) in Footpaths)Footpath(from,to,1.2f);
        Brook();WaterWheel();Reeds();RiverStones();Landings();Terrace();Shrine();Dressing();
        Label(this,"OLD RELAY ROAD  ↑",new(4,2.6f,-.3f),Gold,27);
    }
    private void Resident(string name,string role,Vector3 p,int variant,float? face=null)
    {
        bool custom=TripoResidents.TryGetValue(name,out var model);
        p.Y=Height(p.X,p.Z); var n=new Node3D {Name=name,Position=p,Rotation=new(0,face??(name=="Chiyo"||variant%2==0?1.1f:-1.1f),0)}; AddChild(n);
        var body=GD.Load<PackedScene>($"res://assets/models/{(custom?model.Asset:"villager")}.glb").Instantiate<Node3D>();
        n.AddChild(body); body.Scale=Vector3.One*(custom?1:1.03f+variant*.02f);
        var visual=new ActorVisual();n.AddChild(visual);visual.Bind(body);visual.Play("idle");
        Color[] colors={new("466b76"),new("876c88"),new("d5d9c4"),new("a17b47"),new("6c8c65"),new("77bfb2")};
        foreach(var m in ActorVisual.Descendants(body).OfType<MeshInstance3D>())
        {
            if(!custom&&(m.Name.ToString().Contains("coat",StringComparison.OrdinalIgnoreCase)||m.Name.ToString().Contains("armor",StringComparison.OrdinalIgnoreCase))) m.MaterialOverride=Mat(colors[variant]);
        }
        Label(n,name,new(0,custom?model.Label:2.35f,0),variant==0?Gold:new("ecedda"),29);
        var collision=new StaticBody3D {CollisionLayer=1};n.AddChild(collision);
        float height=custom?model.Height:1.8f;
        collision.AddChild(new CollisionShape3D { Position=new(0,height/2,0),Shape=new CapsuleShape3D { Radius=.32f,Height=height } });
        Residents.Add((name,role,n));
    }
    // A timber bridge along its node's Z from -half to half: a plank every half metre with its top at deck, on three log stringers and a log cap
    // across each bent's pair of stone piers, with a post every 2 m and a top and knee rail each side. One slab carries walkers at the deck and
    // each top rail fences them in, as the flat bridges' boxes did.
    private void TimberBridge(Node3D bridge,float deck,float half,float rail,float[] bents)
    {
        var o=bridge.Position;var r=new RandomNumberGenerator{Seed=4441};
        for(int i=0;i<=half*4;i++)Prop(bridge,$"Bridge plank {i}","plank",new(r.RandfRange(-.05f,.05f),deck-r.RandfRange(0,.012f),i*.5f-half),i%2*Mathf.Pi+r.RandfRange(-.01f,.01f));
        foreach(float x in new[]{-2.2f,0,2.2f})Prop(bridge,$"Bridge stringer {x}","field_rail",new(x,deck-.21f,0),0,new(2,2,half+.4f));
        foreach(float z in bents)
        {
            Prop(bridge,$"Bridge pier cap {z}","field_rail",new(0,deck-.45f,z),Mathf.Pi/2,new(2,2,1.4f));
            foreach(float x in new[]{-2f,2f}){float top=deck-.57f,bottom=Height(o.X+x,o.Z+z)-.4f;Box(bridge,$"Stone bridge pier {bridge.GetChildCount()}",new(x,(top+bottom)/2,z),new(.8f,top-bottom,.9f),stone,true);}
        }
        foreach(float x in new[]{-2.9f,2.9f})
        {
            for(float z=-half;z<=half;z+=2)Prop(bridge,$"Bridge post {bridge.GetChildCount()}","field_post",new(x,deck,z),z,new(1.2f,(rail+.1f-deck)/1.31f,1.2f));
            foreach(float y in new[]{rail,deck+.45f})Prop(bridge,$"{(y==rail?"Bridge rail":"Bridge knee rail")} {x}","field_rail",new(x,y,0),0,new(1,1,half+.25f));
            Solid(o+new Vector3(x,rail,0),Vector3.Zero,new(.12f,.12f,2*half+.5f));
        }
        Solid(o+new Vector3(0,deck-.1f,0),Vector3.Zero,new(6,.2f,2*half+.46f));
    }
    private void Solid(Vector3 p,Vector3 turn,Vector3 size){var body=new StaticBody3D{Position=p,Rotation=turn,CollisionMask=0};AddChild(body);body.AddChild(new CollisionShape3D{Shape=new BoxShape3D{Size=size}});}
    private void Lamp(Vector3 p)=>Prop(this,"Lantern pole","lamp",p);
    // Weathered posts and split rails from tools/build_village_props.py. A rail is its 2 m model stretched to the span, so the colliders are bodies of their own.
    private void Fence(Vector3 a,Vector3 b)
    {
        int count=Math.Max(1,(int)a.DistanceTo(b)/2);
        for(int i=0;i<=count;i++)
        {var p=a.Lerp(b,(float)i/count);p.Y=Height(p.X,p.Z);Prop(this,"Fence post","field_post",p,i*1.7f);Solid(p+new Vector3(0,.65f,0),Vector3.Zero,new(.16f,1.3f,.16f));}
        // Rails run post to post, so they climb a ramp with the ground instead of floating level over it.
        for(int i=0;i<count;i++)
        {
            Vector3 p0=a.Lerp(b,(float)i/count),p1=a.Lerp(b,(float)(i+1)/count);p0.Y=Height(p0.X,p0.Z);p1.Y=Height(p1.X,p1.Z);
            var turn=new Vector3(-Mathf.Atan2(p1.Y-p0.Y,new Vector2(p1.X-p0.X,p1.Z-p0.Z).Length()),Mathf.Atan2(b.X-a.X,b.Z-a.Z),0);float length=p0.DistanceTo(p1)+.1f;
            foreach(float y in new[]{.48f,1f})
            {
                foreach(var rail in Prop(this,$"Fence rail {GetChildCount()}","field_rail",(p0+p1)/2+new Vector3(0,y,0),0,new(1,1,length/2)))rail.Rotation=turn;
                Solid((p0+p1)/2+new Vector3(0,y,0),turn,new(.12f,.12f,length));
            }
        }
    }
    private void Countryside()
    {
        for(int i=0;i<140;i++)
        {
            float x=rng.RandfRange(-59,59),z=rng.RandfRange(-75,71);
            if(Math.Abs(x)<9|| (z>0&&Math.Abs(x)<40)||Math.Abs(z+8)<5||new Vector2(x-1,z+61).Length()<8)continue;
            Tree(new(x,Height(x,z),z),rng.RandfRange(.8f,1.5f),"countryside");
        }
        for(int i=0;i<150;i++)
        {
            float x=rng.RandfRange(-60,60),z=rng.RandfRange(-75,65);
            if(Math.Abs(x)<5||(z>0&&Math.Abs(x)<39))continue;
            float s=rng.RandfRange(.3f,1.5f);var tilt=new Vector3(rng.Randf(),rng.Randf()*6,0);
            // Skipped only after its draws, so the rest of the seeded scatter keeps its place. The window opens at Height()'s z>-9
            // hamlet split because the channel cuts on through the river bank; the one boulder it removes sits there, at z -4.2.
            if(z>-9&&Math.Abs(x-BrookX(z))<2.3f+s)continue;
            Sphere(this,"Weathered boulder",new(x,Height(x,z)+s*.3f,z),new(s,s*.6f,s*.75f),stone).Rotation=tilt;
        }
        var meadow=new List<(Vector3 P,float H)>();
        bool Meadow(float x,float z)=>!(Math.Abs(x-Mathf.Sin(z*.068f)*2.4f)<3.6f||(z>0&&Math.Abs(x)<40)||Math.Abs(z+8)<4);
        for(int i=0;i<5000;i++)
        {
            float x=rng.RandfRange(-60,60),z=rng.RandfRange(-75,70);
            if(!Meadow(x,z))continue;
            meadow.Add((new(x,Height(x,z),z),rng.RandfRange(.18f,.6f)));
        }
        Grass("Meadow grass",meadow,3,p=>Meadow(p.X,p.Y),5,.45f,new("3a5122"),new("8ea54f"),.08f,5003);
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
        var water = new ShaderMaterial { Shader=GD.Load<Shader>("res://assets/shaders/water.gdshader") };water.SetShaderParameter("flow",new Vector2(-.35f,0));
        Mesh(this,"Cedar River",new PlaneMesh { Size=new(2*HalfWidth,7.5f),SubdivideWidth=2*HalfWidth-1 },new(0,CedarLevel,-111),water);
        var bridge=new Node3D {Name="Cedar Bridge",Position=new(RoadX(-111),0,-111)};AddChild(bridge);
        TimberBridge(bridge,4.1f,8,5.2f,new[]{-6f,0,6f});
        Label(bridge,"CEDAR RIVER",new(0,6.6f,0),Gold,29);
        var shelter=new Node3D {Name="Cedar Pass Shelter",Position=new(-12,Height(-12,-91),-91)};AddChild(shelter);
        Box(shelter,"Shelter floor",new(0,-.12f,0),new(6,.3f,4.5f),stone,true);
        foreach(float x in new[]{-2.6f,2.6f})foreach(float z in new[]{-1.8f,1.8f})
            Box(shelter,"Shelter pillar",new(x,1.6f,z),new(.2f,3.2f,.2f),wood,true);
        foreach(float side in new[]{-1f,1f})
        {var r=Box(shelter,"Shelter tiled roof",new(side*1.65f,3.5f,0),new(3.7f,.2f,5.5f),roof);r.Rotation=new(0,0,-side*.38f);CameraStop(r,new BoxShape3D{Size=((BoxMesh)r.Mesh).Size});}
        Box(shelter,"Resting bench",new(0,.6f,-1.4f),new(4,.2f,.6f),wood,true);
        Box(shelter,"Bench back",new(0,1.1f,-1.65f),new(4,.55f,.12f),wood);
        foreach(float x in new[]{-1.5f,1.5f})Box(shelter,"Bench support",new(x,.25f,-1.4f),new(.18f,.5f,.5f),wood);
        for(int i=0;i<3;i++)Prop(shelter,"Rain barrel","barrel",new(3.4f,0,-1+i*1.1f),i*2,Vector3.One*1.2f);
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
        Signpost(new(RoadX(-80)+4,-80),"N · KOMOREBI\nS · HOSHIKAWA");
        Signpost(new(RoadX(-123)-4,-123),"N · KOMOREBI\nE · OVERLOOK");
        for(int i=0;i<180;i++)
        {
            float x=rng.RandfRange(-59,59),z=rng.RandfRange(-193,-80);
            if(NorthClear(x,z)||new Vector2(x,z).DistanceTo(Geometry2D.GetClosestPointToSegment(new(x,z),new(29,-124),new(2,-106)))<7)continue;
            Tree(new(x,Height(x,z),z),rng.RandfRange(.75f,1.35f),"valley");
        }
        for(int i=0;i<70;i++)
        {
            float x=rng.RandfRange(-59,59),z=rng.RandfRange(-192,-80);
            if(NorthClear(x,z))continue;
            float s=rng.RandfRange(.35f,1.3f);
            Sphere(this,"Pass boulder",new(x,Height(x,z)+s*.3f,z),new(s,s*.7f,s),stone);
        }
        var grass=new List<(Vector3 P,float H)>();
        for(int i=0;i<6500;i++)
        {
            float x=rng.RandfRange(-60,60),z=rng.RandfRange(-193,-80);
            if(NorthClear(x,z))continue;
            grass.Add((new(x,Height(x,z),z),rng.RandfRange(.18f,.55f)));
        }
        Grass("Cedar Pass meadow",grass,3,p=>!NorthClear(p.X,p.Y),5,.45f,new("3d5124"),new("98a75a"),.12f,6511);
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

    private void GroundStrip(string name,Vector2 a,Vector2 b,float halfWidth,Material material,float lift=.045f)
    {
        int steps=Math.Max(1,(int)a.DistanceTo(b)*2);
        int across=Math.Max(1,(int)Math.Ceiling(halfWidth*2));
        Vector2 side=new(-(b-a).Normalized().Y,(b-a).Normalized().X);
        var st=new SurfaceTool();st.Begin(Godot.Mesh.PrimitiveType.Triangles);
        void V(Vector2 p){st.SetNormal(Vector3.Up);st.SetUV(p*.22f);st.AddVertex(new(p.X,Height(p.X,p.Y)+lift,p.Y));}
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
    private void Signpost(Vector2 p,string text)
    {
        var sign=new Node3D {Name="Valley wayfinder",Position=new(p.X,Height(p.X,p.Y),p.Y)};AddChild(sign);
        Box(sign,"Sign post",new(0,1.3f,0),new(.17f,2.6f,.17f),wood);
        Box(sign,"Direction boards",new(0,2.15f,0),new(3.5f,.9f,.18f),wood);
        foreach(float side in new[]{-1f,1f})
        {
            var lettering=Label(sign,text,new(0,2.17f,side*.12f),Gold,29);
            lettering.Billboard=BaseMaterial3D.BillboardModeEnum.Disabled;
            lettering.Rotation=new(0,side<0?Mathf.Pi:0,0);
        }
    }
    private void Komorebi()
    {
        foreach(var (name,at,size,yaw,style) in KomorebiHouses) Minka(name,at,size,yaw,style);
        float Face(float x,float z)=>Mathf.Atan2(RoadX(z)-x,0);
        Resident("Nao","LODGE KEEPER",new(-5.6f,0,-137.8f),0,Face(-5.6f,-137.8f));
        Resident("Emi","MOUNTAIN MEDIC",new(6,0,-145),2,Face(6,-145));
        Resident("Toma","ORCHARD KEEPER",new(16.2f,0,-156.7f),3,0);
        Resident("Yui","PASS COURIER",new(-6,0,-160.2f),4,Face(-6,-160.2f));
        // Door paths: the lodge's and the clinic's from the road, and one across it from the weavers' door to the store's.
        Footpath(new(0,-139.8f),new(-7.3f,-139.8f),1.3f);Footpath(new(0,-146),new(7.7f,-146),1.3f);
        Footpath(new(-7.7f,-161.2f),new(10.4f,-161.2f),1.3f);Footpath(new(0,-154),new(22,-154),1.3f);
        Well("Komorebi well",new(-4.8f,-149.6f));NoticeBoard("Komorebi notice board",new(5.6f,-151.6f),180);
        Bench(new(-6.2f,-156.5f),90);Bench(new(5.8f,-157.5f),-90);
        StoneLantern(this,new(-3.9f,Height(-3.9f,-170.5f),-170.5f));StoneLantern(this,new(3.9f,Height(3.9f,-170.5f),-170.5f));
        foreach(var p in new[]{new Vector2(-4.5f,-136),new Vector2(5,-143),new Vector2(4.4f,-149.4f),new Vector2(5.4f,-164.2f),new Vector2(-6,-167),new Vector2(20,-152)})
            Lamp(new(p.X,Height(p.X,p.Y),p.Y));
        // Komorebi's own scatter (courtyard shrubs, persimmons) draws from its own generator, so rng's stream through the orchard is unchanged.
        var r=new RandomNumberGenerator{Seed=5101};
        foreach(var p in new[]{new Vector2(-20,-149),new Vector2(10.5f,-170.5f),new Vector2(-18,-176)})
        {
            Tree(new(p.X,Height(p.X,p.Y),p.Y),.72f);
            for(int i=0;i<3;i++)
            {
                float a=i*2.1f+r.Randf(),x=p.X+Mathf.Sin(a)*1.9f,z=p.Y+Mathf.Cos(a)*1.9f;
                Tree(new(x,Height(x,z),z),r.RandfRange(.75f,.95f),"shrub",r);
            }
        }
        foreach(float z in new[]{-134f,-166f})
        {
            foreach(float x in new[]{-4.5f,4.5f}) Box(this,"Festival mast",new(x,Height(x,z)+2.4f,z),new(.15f,4.8f,.15f),wood);
            Box(this,"Bunting rope",new(0,Height(0,z)+4.5f,z),new(9,.035f,.035f),wood);
            for(int i=0;i<9;i++)
                Box(this,"Cloth pennant",new(-3.8f+i*.95f,Height(0,z)+4.15f,z),new(.55f,.65f,.025f),Mat(new(i%2==0?"ba795d":"7ba89c")));
        }
        Signpost(new(5,-135),"KOMOREBI\nLODGE · CLINIC · ORCHARDS");
        // Persimmons hang on the crown's lower outside, on the foliage cards' outer corners.
        var fruit=Mat(new("e07a2c"));
        for(int row=0;row<3;row++)for(int col=0;col<3;col++)
        {
            float x=25+col*6,z=-146-row*9;
            var tree=Tree(new(x,Height(x,z),z),.75f,"kaki");
            var leaves=ActorVisual.Descendants(tree).OfType<MeshInstance3D>().SelectMany(m=>Enumerable.Range(0,m.Mesh.GetSurfaceCount())
                .Where(i=>m.Mesh.SurfaceGetMaterial(i)?.ResourceName=="foliage").SelectMany(i=>m.Mesh.SurfaceGetArrays(i)[(int)Godot.Mesh.ArrayType.Vertex].AsVector3Array().Select(v=>m.GlobalTransform*v))).ToArray();
            float reach=leaves.Max(v=>new Vector2(v.X-x,v.Z-z).Length()),low=leaves.Min(v=>v.Y),high=leaves.Max(v=>v.Y);
            var outer=leaves.Where(v=>new Vector2(v.X-x,v.Z-z).Length()>reach*.6f&&v.Y<low+(high-low)*.55f).ToArray();
            for(int i=0;i<14;i++)Sphere(this,$"Orchard persimmon {GetChildCount()}",outer[r.RandiRange(0,outer.Length-1)]+Vector3.Down*.1f,Vector3.One*.11f,fruit);
        }
        // The packing station between the store and the orchard fence: Toma's handcart, barrels and a hasa rack along the fence.
        Handcart("Orchard handcart",new(19,-161),90);
        foreach(var p in new Vector2[]{new(17.4f,-164),new(17.6f,-164.9f),new(18.3f,-164.4f)})Prop(this,"Orchard barrel","barrel",new(p.X,Height(p.X,p.Y),p.Y),p.X*3);
        Hasa(new(20.3f,-167),90);
        // Hoshigaki: strings of persimmons drying under the store's front eave, either side of its sign.
        var store=Buildings.Single(n=>n.Name=="Orchard Store");float front=TripoHouses["kura"].Walls.End.Y+.35f,eave=KomorebiHouses.Single(h=>h.Name=="Orchard Store").Size.Y;
        var dried=Mat(new("b8582a"));var cord=Mat(new("3f3026"));
        foreach(float x in new[]{-2.5f,-2.1f,-1.7f,1.7f,2.1f,2.5f})
        {
            Box(store,"Hoshigaki cord",new(x,(1.9f+eave)/2,front),new(.015f,eave-1.9f,.015f),cord);
            for(int i=0;i<6;i++)Sphere(store,"Hoshigaki",new(x,2+i*.17f,front),new(.07f,.08f,.07f),dried);
        }
        Footpath(new(0,-153),new(-12,-151.5f),1.2f);Footpath(new(-12,-151.5f),new(-38.5f,-151.5f),1.2f);
        Footpath(new(-32.5f,-142),new(-32.5f,-161),.8f);
        Footpath(new(22,-154),new(24,-151),1.2f);Footpath(new(24,-151),new(34,-151),1.2f);
        Footpath(new(28,-141),new(28,-167),.8f);
        for(int field=0;field<2;field++)foreach(float z in new[]{-146f,-157f})
        {
            float x=-29-field*7;
            GroundStrip($"Komorebi garden bed {field}-{(int)-z}",new(x,z-4),new(x,z+4),2.5f,Mat(new("716348")));
            // Cabbages in the x -29 beds, daikon rows in the x -36 beds, standing on the soil strip's 4.5 cm lift.
            for(int row=0;row<(field==0?4:6);row++)for(int col=0;col<4;col++)
            {
                float px=x-1.8f+col*1.2f,pz=field==0?z-2.7f+row*1.8f:z-3.25f+row*1.3f;
                Prop(this,field==0?"Cabbage":"Daikon",field==0?"cabbage":"daikon",new(px,Height(px,pz)+.045f,pz),row*2.3f+col*1.1f);
            }
        }
        // The scarecrow stands in the middle of the nearer cabbage bed, between its four central cabbages, facing the lane.
        Kakashi("Komorebi scarecrow",new(-29,-157),0);
        Fence(new(-24,0,-139),new(-24,0,-149.5f));Fence(new(-24,0,-153.5f),new(-24,0,-171));
        Fence(new(22,0,-139),new(22,0,-152));Fence(new(22,0,-156),new(22,0,-173));
        Fence(new(-24,0,-171),new(-20,0,-171));Fence(new(10,0,-173),new(22,0,-173));
    }
    private ShaderMaterial? grassMaterial;
    // Grass and rice tufts in 32 m chunks, each around its own origin so culling and the visibility range drop what is out of sight.
    // Each scattered tuft gathers per-1 neighbours where `open` allows. Neighbours, colours and blades come from `look`, seeded
    // per field, so a field never draws from a scatter stream.
    private void Grass(string name,List<(Vector3 P,float H)> seeds,int per,Func<Vector2,bool> open,int blades,float lean,Color root,Color tip,float dry,ulong seed)
    {
        grassMaterial??=new ShaderMaterial{Shader=GD.Load<Shader>("res://assets/shaders/grass.gdshader")};
        var look=new RandomNumberGenerator{Seed=seed};var field=new Node3D{Name=name};AddChild(field);
        var tufts=new List<(Vector3 P,float H)>(seeds);
        foreach(var (p,h) in seeds)for(int i=1;i<per;i++)
        {
            var q=new Vector2(p.X,p.Z)+Vector2.FromAngle(look.Randf()*Mathf.Tau)*look.RandfRange(.4f,.9f);float k=look.RandfRange(.6f,1.1f);
            if(open(q))tufts.Add((new(q.X,Height(q.X,q.Y),q.Y),h*k));
        }
        foreach(var chunk in tufts.GroupBy(t=>(Mathf.FloorToInt(t.P.X/32),Mathf.FloorToInt(t.P.Z/32))))
        {
            var origin=new Vector3(chunk.Key.Item1*32+16,chunk.Average(t=>t.P.Y),chunk.Key.Item2*32+16);
            var st=new SurfaceTool();st.Begin(Godot.Mesh.PrimitiveType.Triangles);
            foreach(var (p,h) in chunk)
            {
                float shade=look.RandfRange(.82f,1.1f);bool straw=look.Randf()<dry;
                var top=(straw?new Color("c4b47c"):tip.Lerp(new("bfc46c"),look.Randf()*.55f))*shade;
                Tuft(st,look,p-origin,h,blades,lean,(root*shade).SrgbToLinear(),top.SrgbToLinear());
            }
            st.Index();var m=Mesh(field,$"{name} {chunk.Key}",st.Commit(),origin,grassMaterial);
            m.CastShadow=GeometryInstance3D.ShadowCastingSetting.Off;m.VisibilityRangeEnd=90;m.VisibilityRangeEndMargin=8;
        }
    }
    // Arching blades, each a tapered quad and a tip; COLOR.a is grass.gdshader's sway weight.
    private static void Tuft(SurfaceTool st,RandomNumberGenerator look,Vector3 p,float h,int blades,float lean,Color root,Color tip)
    {
        var mid=root.Lerp(tip,.55f);
        for(int i=0;i<blades;i++)
        {
            float a=look.Randf()*Mathf.Tau,bh=h*look.RandfRange(.55f,1.05f),w=look.RandfRange(.022f,.04f),l=lean*look.RandfRange(.4f,1.3f);
            var dir=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));var side=new Vector3(-dir.Z,0,dir.X)*w;
            var b=p+dir*look.RandfRange(0,.05f);var m=b+dir*l*bh*.3f+Vector3.Up*bh*.55f;var t=b+dir*l*bh+Vector3.Up*bh*(1-.35f*l);
            var n=(Vector3.Up+dir*.35f).Normalized();
            void V(Vector3 v,Color c,float sway){st.SetNormal(n);st.SetColor(new(c.R,c.G,c.B,sway));st.AddVertex(v);}
            V(b-side,root,0);V(b+side,root,0);V(m+side*.6f,mid,.3f);
            V(b-side,root,0);V(m+side*.6f,mid,.3f);V(m-side*.6f,mid,.3f);
            V(m-side*.6f,mid,.3f);V(m+side*.6f,mid,.3f);V(t,tip,1);
        }
    }
    private static readonly Dictionary<string,(int Variants,float Radius,float Height)> TreeTrunks=new()
        {["keyaki"]=(3,.32f,3.2f),["sugi"]=(3,.34f,3.5f),["sakura"]=(2,.28f,2.2f),["bamboo"]=(1,.75f,3),["shrub"]=(2,.45f,1),["kaki"]=(2,.26f,1.6f)};
    private readonly Dictionary<string,PackedScene> treeScenes=new();
    private readonly Dictionary<string,Material> treeMaterials=new();
    // Exactly three draws from `r` (variant, tint, yaw) keep the seeded scatter streams stable whatever species is chosen.
    private Node3D Tree(Vector3 p,float scale,string species="keyaki",RandomNumberGenerator? r=null)
    {
        r??=rng;float a=r.Randf(),b=r.Randf(),c=r.Randf();
        species=species switch{"countryside"=>a<.7f?"keyaki":"sugi","valley"=>a<.75f?"sugi":"keyaki",_=>species};
        var spec=TreeTrunks[species];string file=$"{species}_{(char)('a'+(int)(a*997)%spec.Variants)}";
        if(!treeScenes.TryGetValue(file,out var scene))treeScenes[file]=scene=GD.Load<PackedScene>($"res://assets/models/props/{file}.glb");
        var t=scene.Instantiate<Node3D>();t.Name=$"{species} {GetChildCount()}";t.Position=p;t.Scale=Vector3.One*scale;t.Rotation=new(0,c*Mathf.Tau,0);AddChild(t);
        var tint=new Color(.9f+b*.2f,.92f+b*.14f,.88f+b*.1f);
        foreach(var m in ActorVisual.Descendants(t).OfType<MeshInstance3D>())
        {
            for(int i=0;i<m.Mesh.GetSurfaceCount();i++)m.SetSurfaceOverrideMaterial(i,TreeMaterial(m.Mesh.SurfaceGetMaterial(i)?.ResourceName??""));
            m.SetInstanceShaderParameter("tint",tint);
        }
        var body=new StaticBody3D();t.AddChild(body);
        body.AddChild(new CollisionShape3D {Position=new(0,spec.Height/2,0),Shape=new CylinderShape3D {Radius=spec.Radius,Height=spec.Height}});
        return t;
    }
    private Material TreeMaterial(string name)
    {
        if(treeMaterials.TryGetValue(name,out var m))return m;
        if(name=="foliage")
        {
            var f=new ShaderMaterial {Shader=GD.Load<Shader>("res://assets/shaders/foliage.gdshader")};
            f.SetShaderParameter("atlas",GD.Load<Texture2D>("res://assets/art/foliage-atlas.png"));m=f;
        }
        else m=new StandardMaterial3D {AlbedoTexture=GD.Load<Texture2D>($"res://assets/art/bark-{name.Replace("bark_","")}.png"),Roughness=.95f};
        return treeMaterials[name]=m;
    }
}
