using Godot;
namespace Hoshikawa;

// A ribbon between two points on the blade, kept for Life seconds while Emitting and faded by age.
public partial class SwordTrail : MeshInstance3D
{
    public bool Emitting;
    public float Life=.16f;
    public Node3D Base=null!,Tip=null!;
    private readonly List<(Vector3 Base,Vector3 Tip,float Age)> samples=new();
    private readonly ImmediateMesh ribbon=new();
    public override void _Ready()
    {
        Name="Sword trail";TopLevel=true;Mesh=ribbon;CastShadow=ShadowCastingSetting.Off;
        MaterialOverride=new StandardMaterial3D {ShadingMode=BaseMaterial3D.ShadingModeEnum.Unshaded,Transparency=BaseMaterial3D.TransparencyEnum.Alpha,
            BlendMode=BaseMaterial3D.BlendModeEnum.Add,CullMode=BaseMaterial3D.CullModeEnum.Disabled,VertexColorUseAsAlbedo=true,NoDepthTest=false};
    }
    // Sampled per drawn frame, so the ribbon follows the blade's interpolated arc rather than the clip's 24 keys a second.
    public override void _Process(double delta)
    {
        float dt=(float)delta;
        for(int i=samples.Count-1;i>=0;i--){var s=samples[i];s.Age+=dt;if(s.Age>=Life)samples.RemoveAt(i);else samples[i]=s;}
        if(Emitting)samples.Add((Base.GlobalPosition,Tip.GlobalPosition,0));
        ribbon.ClearSurfaces();
        if(samples.Count<2)return;
        ribbon.SurfaceBegin(Godot.Mesh.PrimitiveType.TriangleStrip);
        foreach(var s in samples)
        {
            float fade=1-s.Age/Life;
            ribbon.SurfaceSetColor(new Color(World.Jade,fade*.2f));ribbon.SurfaceAddVertex(s.Base);
            ribbon.SurfaceSetColor(new Color(World.Jade.Lightened(.6f),fade));ribbon.SurfaceAddVertex(s.Tip);
        }
        ribbon.SurfaceEnd();
    }
    public void Clear()=>samples.Clear();
}
