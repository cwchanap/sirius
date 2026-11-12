using Godot;
namespace Hoshikawa;

// Tunnel Mountain's traps, World.Parts rows: a rotten plank gives way a moment after Mio steps on it, and a rolling load, parked in plain
// sight at the head of its run, rumbles and rolls down it once she comes near its foot. Each stays sprung until a rest re-arms them all.
// Far robots are not simulated: beyond FarOff metres one stops and hides until she comes back within NearBy.
public partial class Game
{
    public const float Roll=6.5f,Give=.3f,FarOff=75,NearBy=70;
    public readonly HashSet<string> Sprung=new();
    // How long each sprung plank has been giving way, ticked only in play so a pause holds it, how far each load now rolling has come,
    // and the loads that have struck her on this run.
    private readonly Dictionary<string,float> giving=new();
    private readonly Dictionary<string,float> rolling=new();
    private readonly HashSet<string> struckBy=new();
    private void Traps(float dt)
    {
        var mio=Player.GlobalPosition;static float Flat(Vector3 v)=>new Vector2(v.X,v.Z).Length();
        foreach(var t in World.Parts)
        {
            if(t.Kind=="plank")
            {
                if(!Sprung.Contains(t.Name)&&Flat(t.From-mio)<t.Size&&Math.Abs(mio.Y-t.From.Y)<.6f){Sprung.Add(t.Name);giving[t.Name]=0;Sound("guard");}
                if(!giving.TryGetValue(t.Name,out float g))continue;
                g+=dt;giving[t.Name]=g;var body=(StaticBody3D)t.Body;if(g<Give)continue;
                float f=Math.Min(1,g-Give);body.CollisionLayer=0;body.Position=t.Rest.Origin+Vector3.Down*8*f*f;
                if(f>=1){giving.Remove(t.Name);body.Visible=false;}
            }
            if(!t.Rolls)continue;
            if(!Sprung.Contains(t.Name)&&Flat(t.To-mio)<2.4f){Sprung.Add(t.Name);rolling[t.Name]=0;((StaticBody3D)t.Body).CollisionLayer=0;Sound("slam");CameraShake=.12f;}
            if(!rolling.TryGetValue(t.Name,out float run))continue;
            float length=Flat(t.To-t.From);run=Math.Min(length,run+Roll*dt);rolling[t.Name]=run;
            var p=t.From.Lerp(t.To,run/length);p.Y=Hoshikawa.World.Height(p.X,p.Z);t.Body.GlobalPosition=p;t.Body.GetNode<Node3D>("Spin").RotateX(Roll*dt/t.Size);
            var along=(t.To-t.From).Normalized();along.Y=0;var across=new Vector3(-along.Z,0,along.X);var d=mio-p;
            if(!struckBy.Contains(t.Name)&&Math.Abs(d.Dot(along))<t.Size+.4f&&Math.Abs(d.Dot(across))<t.Span+.3f&&d.Y>-1&&d.Y<2*t.Size+.6f){struckBy.Add(t.Name);Player.Crush(along);}
            if(run>=length){rolling.Remove(t.Name);t.Body.Visible=false;Dust(p,1.6f,14);Sound("break");}
        }
    }
    // A rest, a recovery or a restart puts every trap back as it was built.
    public void Arm()
    {
        Sprung.Clear();giving.Clear();rolling.Clear();struckBy.Clear();
        foreach(var t in World.Parts.Where(p=>p.Kind=="plank"||p.Rolls))
        {
            t.Body.Transform=t.Rest;t.Body.Visible=true;((StaticBody3D)t.Body).CollisionLayer=1;
            if(t.Rolls)t.Body.GetNode<Node3D>("Spin").Rotation=Vector3.Zero;
        }
    }
    private void Cull()
    {
        var mio=new Vector2(Player.GlobalPosition.X,Player.GlobalPosition.Z);
        foreach(var e in Enemies)
        {
            bool near=new Vector2(e.GlobalPosition.X,e.GlobalPosition.Z).DistanceTo(mio)<(e.Visible?FarOff:NearBy);
            if(near!=e.Visible){e.Visible=near;e.ProcessMode=near?ProcessModeEnum.Inherit:ProcessModeEnum.Disabled;}
        }
    }
}
