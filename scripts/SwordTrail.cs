using Godot;
namespace Hoshikawa;

// A ribbon over the blade from From (0 hilt, 1 tip) to its tip, kept for Life seconds while Emitting; sword_trail.gdshader
// burns its fresh edge white and fades it by age.
public partial class SwordTrail : MeshInstance3D
{
    public bool Emitting;
    public float Life=.12f,From=.5f;
    public new Skeleton3D Skeleton=null!;
    public int Bone;
    public Vector3 Hilt,Tip;
    private readonly List<(Vector3 Base,Vector3 Tip,float Age)> samples=new();
    private readonly ImmediateMesh ribbon=new();
    private readonly ShaderMaterial look=new() {Shader=GD.Load<Shader>("res://assets/shaders/sword_trail.gdshader")};
    public override void _Ready()
    {
        // Top level keeps the global transform it had, Mio's; the samples are global, so it must be the identity.
        Name="Sword trail";TopLevel=true;GlobalTransform=Transform3D.Identity;Mesh=ribbon;CastShadow=ShadowCastingSetting.Off;MaterialOverride=look;Glow(World.Jade,2.5f);
    }
    // The way the tip moved over the last drawn frame.
    public Vector3 Sweep=>samples.Count<2?Vector3.Zero:samples[^1].Tip-samples[^2].Tip;
    public void Glow(Color tint,float energy){look.SetShaderParameter("tint",tint);look.SetShaderParameter("energy",energy);}
    // Read from the hand bone's pose as it is drawn: a BoneAttachment3D lags a frame, up to 0.7 m of a swing.
    public (Vector3 Base,Vector3 Tip) Blade()
    {
        var hand=Skeleton.GlobalTransform*Skeleton.GetBoneGlobalPose(Bone);return (hand*Hilt.Lerp(Tip,From),hand*Tip);
    }
    // Sampled per drawn frame and joined by a spline through four samples at a time, so a fast spin stays round.
    public override void _Process(double delta)
    {
        float dt=(float)delta;
        for(int i=samples.Count-1;i>=0;i--){var s=samples[i];s.Age+=dt;if(s.Age>=Life)samples.RemoveAt(i);else samples[i]=s;}
        if(Emitting){var (b,t)=Blade();samples.Add((b,t,0));}
        ribbon.ClearSurfaces();
        if(samples.Count<2)return;
        ribbon.SurfaceBegin(Godot.Mesh.PrimitiveType.TriangleStrip);
        void Add(Vector3 b,Vector3 t,float age)
        {ribbon.SurfaceSetUV(new(age/Life,0));ribbon.SurfaceAddVertex(b);ribbon.SurfaceSetUV(new(age/Life,1));ribbon.SurfaceAddVertex(t);}
        for(int i=0;i<samples.Count-1;i++)
        {
            var (a,b,c,d)=(samples[Math.Max(i-1,0)],samples[i],samples[i+1],samples[Math.Min(i+2,samples.Count-1)]);
            for(int k=0;k<4;k++){float w=k/4f;Add(b.Base.CubicInterpolate(c.Base,a.Base,d.Base,w),b.Tip.CubicInterpolate(c.Tip,a.Tip,d.Tip,w),Mathf.Lerp(b.Age,c.Age,w));}
        }
        var last=samples[^1];Add(last.Base,last.Tip,last.Age);
        ribbon.SurfaceEnd();
    }
}
