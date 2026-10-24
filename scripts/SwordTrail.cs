using Godot;
namespace Hoshikawa;

// A ribbon over the outer half of the blade, kept for Life seconds while Emitting and faded by age.
public partial class SwordTrail : MeshInstance3D
{
    public bool Emitting;
    public float Life=.12f;
    public Skeleton3D Skeleton=null!;
    public int Bone;
    public Vector3 Base,Tip;
    private readonly List<(Vector3 Base,Vector3 Tip,float Age)> samples=new();
    private readonly ImmediateMesh ribbon=new();
    public override void _Ready()
    {
        // Top level keeps the global transform it had, Mio's; the samples are global, so it must be the identity.
        Name="Sword trail";TopLevel=true;GlobalTransform=Transform3D.Identity;Mesh=ribbon;CastShadow=ShadowCastingSetting.Off;
        MaterialOverride=new StandardMaterial3D {ShadingMode=BaseMaterial3D.ShadingModeEnum.Unshaded,Transparency=BaseMaterial3D.TransparencyEnum.Alpha,
            BlendMode=BaseMaterial3D.BlendModeEnum.Add,CullMode=BaseMaterial3D.CullModeEnum.Disabled,VertexColorUseAsAlbedo=true};
    }
    // Read from the hand bone's pose as it is drawn: a BoneAttachment3D lags a frame, up to 0.7 m of a swing.
    public (Vector3 Base,Vector3 Tip) Blade()
    {
        var hand=Skeleton.GlobalTransform*Skeleton.GetBoneGlobalPose(Bone);return (hand*Base,hand*Tip);
    }
    // Sampled per drawn frame, so the ribbon follows the blade's interpolated arc rather than the clip's 24 keys a second.
    public override void _Process(double delta)
    {
        float dt=(float)delta;
        for(int i=samples.Count-1;i>=0;i--){var s=samples[i];s.Age+=dt;if(s.Age>=Life)samples.RemoveAt(i);else samples[i]=s;}
        if(Emitting){var (b,t)=Blade();samples.Add((b,t,0));}
        ribbon.ClearSurfaces();
        if(samples.Count<2)return;
        ribbon.SurfaceBegin(Godot.Mesh.PrimitiveType.TriangleStrip);
        foreach(var s in samples)
        {
            float fade=1-s.Age/Life;
            ribbon.SurfaceSetColor(new Color(World.Jade,0));ribbon.SurfaceAddVertex(s.Base);
            ribbon.SurfaceSetColor(new Color(World.Jade,fade*.6f));ribbon.SurfaceAddVertex(s.Tip);
        }
        ribbon.SurfaceEnd();
    }
}
