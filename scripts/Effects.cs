using Godot;
namespace Hoshikawa;

// Combat effects. Each is built when it fires and frees itself; tweens and particles run on scaled time, so a hit-stop
// holds them mid-flight. Light goes through glow.gdshader, whose energy above 1 blooms under the valley's glow.
public partial class Game
{
    public Color TintColor;
    public float TintTime,TintLength=1;
    private static Shader? glow;
    private static Texture2D? soft;
    private static Gradient? cool,brighten,settle;
    private static Curve? shrink,swell,push;
    // A white disc fading to nothing at its rim, shared by every flash and dust puff.
    public static Texture2D Soft=>soft??=new GradientTexture2D {Width=64,Height=64,Fill=GradientTexture2D.FillEnum.Radial,FillFrom=new(.5f,.5f),FillTo=new(.5f,0),
        Gradient=Ramp((0,new(1,1,1,1)),(.3f,new(1,1,1,.45f)),(1,new(1,1,1,0)))};
    // Particle colour over life: sparks cool from white to nothing, motes brighten as they gather, dust hangs and thins.
    public static Gradient Cool=>cool??=Ramp((0,new(1,1,1)),(.5f,new(.7f,.7f,.7f)),(1,new(0,0,0)));
    private static Gradient Brighten=>brighten??=Ramp((0,new(0,0,0)),(.35f,new(1,1,1)),(1,new(1,1,1)));
    private static Gradient Settle=>settle??=Ramp((0,new(1,1,1,0)),(.1f,new(1,1,1,.5f)),(1,new(1,1,1,0)));
    private static Curve Shrink=>shrink??=Graph((0,1),(1,.2f));
    private static Curve Swell=>swell??=Graph((0,.3f),(1,1));
    // Dust gets its push in the first eighth of its life and coasts out on it.
    private static Curve Push=>push??=Graph((0,1),(.12f,0),(1,0));
    public static Gradient Ramp(params (float At,Color Color)[] stops)=>new() {Offsets=stops.Select(s=>s.At).ToArray(),Colors=stops.Select(s=>s.Color).ToArray()};
    private static Curve Graph(params (float X,float Y)[] points){var c=new Curve();foreach(var (x,y) in points)c.AddPoint(new(x,y));return c;}
    public static ShaderMaterial Fx(Color tint,float energy,Texture2D? mask=null,bool billboard=false,float height=0,float rim=0)
    {
        var m=new ShaderMaterial {Shader=glow??=GD.Load<Shader>("res://assets/shaders/glow.gdshader")};m.SetShaderParameter("tint",tint);m.SetShaderParameter("energy",energy);m.SetShaderParameter("billboard",billboard);
        m.SetShaderParameter("height",height);m.SetShaderParameter("rim",rim);if(mask!=null)m.SetShaderParameter("mask",mask);return m;
    }
    private void Expire(Node node,double seconds)=>GetTree().CreateTimer(seconds).Timeout+=node.QueueFree;
    private MeshInstance3D Glow(Node3D parent,string name,Mesh mesh,Vector3 p,ShaderMaterial m)
    {var n=Hoshikawa.World.Mesh(parent,name,mesh,p,m);n.CastShadow=GeometryInstance3D.ShadowCastingSetting.Off;return n;}
    // Grows a node to a scale while its light fades out, then frees it (or the node it hangs from).
    private void Fade(Node3D node,ShaderMaterial m,Vector3 scale,double seconds,Node? root=null)
    {
        var t=CreateTween().SetParallel();t.TweenProperty(node,"scale",scale,seconds).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
        // Through the setter: headless, the dummy renderer lists no uniforms, so the parameter is no property to tween.
        t.TweenMethod(Callable.From<float>(f=>m.SetShaderParameter("fade",f)),1f,0f,seconds).SetEase(Tween.EaseType.In);t.Chain().TweenCallback(Callable.From((root??node).QueueFree));
    }
    // A soft glow that swells and fades where something strikes or bursts.
    public void Flash(Vector3 p,Color c,float size,float seconds,float energy=3)
    {var m=Fx(c,energy,Soft,true);var q=Glow(this,"Flash",new QuadMesh(),p,m);q.Scale=Vector3.One*size*.35f;Fade(q,m,Vector3.One*size,seconds);}
    // Streaks flung out along a direction; they fall, shrink and cool from white through their colour to nothing.
    public void Sparks(Vector3 p,Vector3 direction,Color c,int count,float speed,float spread=55)
    {
        var sparks=new CpuParticles3D {Name="Sparks",Position=p,Amount=count,Lifetime=.4,OneShot=true,Explosiveness=1,Randomness=.5f,LocalCoords=false,
            Mesh=new BoxMesh {Size=new(.02f,.2f,.02f)},MaterialOverride=Fx(c,4),ParticleFlagAlignY=true,Direction=direction,Spread=spread,
            InitialVelocityMin=speed*.4f,InitialVelocityMax=speed,Gravity=new(0,-12,0),DampingMin=2,DampingMax=4,ScaleAmountCurve=Shrink,ColorRamp=Cool,
            CastShadow=GeometryInstance3D.ShadowCastingSetting.Off};
        AddChild(sparks);sparks.Emitting=true;Expire(sparks,.6);
    }
    // The bright line a blade leaves where it bites: drawn out along the cut, then thinned to nothing.
    public void Streak(Vector3 p,Vector3 along,Color c,float length)
    {
        var x=along.Normalized();var y=Math.Abs(x.Y)>.9f?Vector3.Right:Vector3.Up;var z=x.Cross(y).Normalized();y=z.Cross(x);
        var node=new Node3D {Name="Cut streak",Position=p,Basis=new Basis(x,y,z)};AddChild(node);
        var m=Fx(c,5);var line=Glow(node,"Streak",new SphereMesh {RadialSegments=8,Rings=4},Vector3.Zero,m);line.Scale=new(length*.3f,.07f,.07f);
        Fade(line,m,new(length,.012f,.012f),.16,node);
    }
    // A blade meeting plating: a flash at the point and sparks thrown on past it.
    public void Impact(Vector3 p,Vector3 away,Color c){Flash(p,c,1.3f,.14f,4);Sparks(p,(away+Vector3.Up*.6f).Normalized(),c,18,7);}
    public void Burst(Vector3 p,Color c,float radius){Flash(p,c,radius*1.8f,.3f);Sparks(p,Vector3.Up,c,(int)(10+radius*8),2.5f+radius*3,90);}
    // Earth thrown out along the ground in a ring, left to hang and settle.
    public void Dust(Vector3 p,float reach,int count)
    {
        var dust=new CpuParticles3D {Name="Dust",Position=p+Vector3.Up*.2f,Amount=count,Lifetime=.9,OneShot=true,Explosiveness=.9f,Randomness=.4f,LocalCoords=false,Mesh=new QuadMesh(),
            MaterialOverride=new StandardMaterial3D {ShadingMode=BaseMaterial3D.ShadingModeEnum.Unshaded,Transparency=BaseMaterial3D.TransparencyEnum.Alpha,VertexColorUseAsAlbedo=true,
                AlbedoTexture=Soft,AlbedoColor=new("cdb592"),BillboardMode=BaseMaterial3D.BillboardModeEnum.Particles,BillboardKeepScale=true},
            EmissionShape=CpuParticles3D.EmissionShapeEnum.Ring,EmissionRingAxis=Vector3.Up,EmissionRingRadius=.4f,EmissionRingInnerRadius=.2f,EmissionRingHeight=.1f,
            Gravity=new(0,.6f,0),RadialAccelMin=reach*32,RadialAccelMax=reach*42,RadialAccelCurve=Push,DampingMin=reach*3,DampingMax=reach*4,
            ScaleAmountMin=.85f,ScaleAmountMax=1.7f,ScaleAmountCurve=Swell,ColorRamp=Settle,CastShadow=GeometryInstance3D.ShadowCastingSetting.Off};
        AddChild(dust);dust.Emitting=true;Expire(dust,1.1);
    }
    // Motes drawn in from all round to a point as something gathers strength; they follow the parent.
    public void Gather(Node3D parent,Vector3 at,Color c,float seconds)
    {
        var motes=new CpuParticles3D {Name="Gathering motes",Position=at,Amount=28,Lifetime=seconds,OneShot=true,LocalCoords=true,Mesh=new SphereMesh {Radius=.03f,Height=.06f,RadialSegments=6,Rings=3},
            MaterialOverride=Fx(c,4),EmissionShape=CpuParticles3D.EmissionShapeEnum.SphereSurface,EmissionSphereRadius=1.5f,Gravity=Vector3.Zero,
            RadialAccelMin=-3/(seconds*seconds)*1.1f,RadialAccelMax=-3/(seconds*seconds)*.9f,ColorRamp=Brighten,CastShadow=GeometryInstance3D.ShadowCastingSetting.Off};
        parent.AddChild(motes);motes.Emitting=true;Expire(motes,seconds*2+.1);
    }
    // A column of light standing up from a point, widening as it fades.
    public void Pillar(Vector3 p,Color c,float seconds)
    {
        var m=Fx(c,2,height:6,rim:1);var beam=Glow(this,"Resonance pillar",new CylinderMesh {TopRadius=.35f,BottomRadius=.5f,Height=6,CapTop=false,CapBottom=false,RadialSegments=24,Rings=1},p+Vector3.Up*3,m);
        beam.Scale=new(.6f,1,.6f);Fade(beam,m,new(2,1.1f,2),seconds);
    }
    // Resonance Cut's slam: two rings out over the ground, the pillar, a flash, dust, jade sparks and a tint across the screen.
    public void Slam(Vector3 p,float radius)
    {
        var jade=Hoshikawa.World.Jade;
        Ring(p,.5f,radius,.45f,jade);Ring(p,.3f,radius*.55f,.3f,new("e6fff7"));Pillar(p,jade,.3f);Flash(p+Vector3.Up*.4f,jade,3.5f,.3f,2);
        Dust(p,radius*.6f,40);Sparks(p+Vector3.Up*.2f,Vector3.Up,jade,44,10,75);ScreenFlash(new(jade,.25f),.25f);
    }
    public void ScreenFlash(Color c,float seconds){TintColor=c;TintTime=TintLength=seconds;}
    // A flat ring on the ground that grows (or closes) from one radius to another and fades.
    public void Ring(Vector3 p,float from,float to,float seconds,Color color)
    {
        var mat=new StandardMaterial3D {ShadingMode=BaseMaterial3D.ShadingModeEnum.Unshaded,Transparency=BaseMaterial3D.TransparencyEnum.Alpha,BlendMode=BaseMaterial3D.BlendModeEnum.Add,AlbedoColor=color};
        var ring=Hoshikawa.World.Mesh(this,"Resonance wave",new TorusMesh {InnerRadius=.93f,OuterRadius=1,Rings=48,RingSegments=4},p+Vector3.Up*.08f,mat);
        ring.Scale=new(from,.4f,from);ring.CastShadow=GeometryInstance3D.ShadowCastingSetting.Off;
        var t=CreateTween().SetParallel();t.TweenProperty(ring,"scale",new Vector3(to,.4f,to),seconds).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
        t.TweenProperty(mat,"albedo_color:a",0f,seconds).SetEase(Tween.EaseType.In);t.Chain().TweenCallback(Callable.From(ring.QueueFree));
    }
    // A blow lands: the whole valley slows to a twentieth for a moment, measured in unscaled time.
    public void HitStop(float seconds)
    {
        Engine.TimeScale=.05;GetTree().CreateTimer(seconds,true,false,true).Timeout+=()=>Engine.TimeScale=1;
    }
    public void Slash(Vector3 p,Vector3 forward,bool red)
    {
        var node=new Node3D {Position=p,Rotation=new(0,Mathf.Atan2(forward.X,forward.Z),-.18f)};AddChild(node);
        var st=new SurfaceTool();st.Begin(Godot.Mesh.PrimitiveType.Triangles);
        for(int i=0;i<18;i++)
        {
            float a=-1.1f+i*.12f,b=a+.12f;
            Vector3 V(float angle,float r)=>new(Mathf.Sin(angle)*r,0,Mathf.Cos(angle)*r);
            st.AddVertex(V(a,2.25f));st.AddVertex(V(b,2.25f));st.AddVertex(V(a,1.9f));
            st.AddVertex(V(a,1.9f));st.AddVertex(V(b,2.25f));st.AddVertex(V(b,1.9f));
        }
        var mat=Hoshikawa.World.Mat(red?new("fa8163"):Hoshikawa.World.Jade,1,true);
        Hoshikawa.World.Mesh(node,"Sword arc",st.Commit(),Vector3.Zero,mat);
        var t=CreateTween();t.TweenProperty(node,"scale",Vector3.One*.4f,.16);t.TweenCallback(Callable.From(node.QueueFree));
    }
    public void FloatingNumber(Vector3 p,string text,Color c)
    {
        var label=Hoshikawa.World.Label(this,text,p,c,36);var t=CreateTween();
        t.TweenProperty(label,"position",p+Vector3.Up*.8f,.65);t.TweenCallback(Callable.From(label.QueueFree));
    }
}
