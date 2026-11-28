using Godot;
namespace Hoshikawa;

// Tunnel Mountain's traps, World.Parts rows: a rotten plank gives way a moment after Mio steps on it, and a rolling load, parked in plain
// sight at the head of its run, rumbles and rolls down it once she comes near its foot. Each stays sprung until a rest re-arms them all.
// A lock-in yard's gates drop behind her as she steps in and lift once its robots are all shut down, for good, or once she is out of it
// again, by a drop or a defeat. Far robots are not simulated: beyond FarOff metres one stops and hides until she comes back within NearBy.
public partial class Game
{
    public const float Roll=6.5f,Give=.3f,FarOff=75,NearBy=70;
    public readonly HashSet<string> Sprung=new();
    // The lock-in yards whose robots she has shut down, open until the prologue restarts, and how far each gate has dropped, 0 to 1.
    public readonly HashSet<string> Cleared=new();
    private readonly Dictionary<string,float> lowered=new();
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
            float length=t.PathLength;run=Math.Min(length,run+Roll*dt);rolling[t.Name]=run;
            Vector2 at,along;
            // A long body rides the run with both ends on it: its trailing end starts at the head and travels the whole run, its leading end
            // Long ahead, so the body spans a bend instead of cutting it. Every other load is a point at its own centre.
            if(t.Long>0){var rear=t.PathPoint(run);var front=t.PathPoint(run+t.Long);at=(rear.At+front.At)*.5f;along=(front.At-rear.At).Normalized();}
            else{var here=t.PathPoint(run);at=here.At;along=here.Along;}
            var p=new Vector3(at.X,Hoshikawa.World.Height(at.X,at.Y),at.Y);t.Body.GlobalPosition=p;t.Body.Rotation=new(0,Mathf.Atan2(along.X,along.Y),0);
            var spin=t.Body.GetNode<Node3D>("Spin");
            if(t.Long>0)foreach(var node in spin.GetChildren())
            {
                // A wagon's axles ride the rails themselves, each placed on the run at its own station and turned to the local rail, while its
                // body spans the chord; only the wheels turn about their own axis, so the chassis, roof and ribs stay rigid.
                if(node is not Node3D axle||!axle.Name.ToString().StartsWith("Wheel set"))continue;
                var rail=t.PathPoint(run+axle.GetMeta("arc").AsSingle());
                axle.GlobalPosition=new Vector3(rail.At.X,Hoshikawa.World.Height(rail.At.X,rail.At.Y)+Hoshikawa.World.WagonWheelBottom+Hoshikawa.World.WagonWheelRadius,rail.At.Y);
                axle.GlobalRotation=new(0,Mathf.Atan2(rail.Along.X,rail.Along.Y),0);
                foreach(var child in axle.GetChildren())if(child is Node3D wheel&&wheel.Name.ToString().StartsWith("Wagon wheel"))wheel.RotateY(Roll*dt/Hoshikawa.World.WagonWheelRadius);
            }
            else spin.RotateX(Roll*dt/t.Size);
            var along3=new Vector3(along.X,0,along.Y);var across=new Vector3(-along3.Z,0,along3.X);var d=mio-p;
            // A wagon's hit volume is its body, not its length: the long body never reaches up to the gantry ramp above its path.
            float tall=t.Long>0?Hoshikawa.World.WagonHitTop:2*t.Size+.6f;
            if(!struckBy.Contains(t.Name)&&Math.Abs(d.Dot(along3))<t.Size+.4f&&Math.Abs(d.Dot(across))<t.Span+.3f&&d.Y>-1&&d.Y<tall){struckBy.Add(t.Name);Player.Crush(along3);}
            if(run>=length){rolling.Remove(t.Name);t.Body.Visible=false;Dust(p,1.6f,14);Sound("break");}
        }
        foreach(var (yard,_) in Hoshikawa.World.Locks)
        {
            if(!Cleared.Contains(yard)&&Enemies.Where(e=>e.Yard==yard).All(e=>e.Dead)){Cleared.Add(yard);Sound("guard");}
            // The gates drop once she is a step past them all, clear of their leaves, and stay down while she is anywhere on the yard's side.
            var gates=World.Parts.Where(p=>p.Yard==yard).ToArray();bool down=gates.Any(p=>lowered.GetValueOrDefault(p.Name)>0);
            bool shut=!Cleared.Contains(yard)&&Hoshikawa.World.InYard(yard,mio,down?0:.8f),slam=false;
            foreach(var t in gates)
            {
                // Each falls in a third of a second and winds back up over a second and a half.
                float was=lowered.GetValueOrDefault(t.Name),f=Math.Clamp(was+(shut?dt/.35f:-dt/1.6f),0,1);if(f==was)continue;
                lowered[t.Name]=f;Drop(t,f);if(f==1){slam=true;Dust(t.From,1.4f,10);}
            }
            if(slam){Sound("slam");CameraShake=.15f;}
        }
    }
    // A gate f of the way down from where it hangs, solid once it is half down.
    private static void Drop(Hoshikawa.World.Part gate,float f){var body=(AnimatableBody3D)gate.Body;body.Position=gate.Rest.Origin+Vector3.Up*gate.Open*(1-f*f);body.CollisionLayer=f>.5f?1u:0;}
    // A rest, a recovery or a restart puts every trap back as it was built.
    public void Arm()
    {
        Sprung.Clear();giving.Clear();rolling.Clear();struckBy.Clear();
        foreach(var t in World.Parts.Where(p=>p.Kind=="plank"||p.Rolls))
        {
            t.Body.Transform=t.Rest;t.Body.Visible=true;((StaticBody3D)t.Body).CollisionLayer=1;
            if(!t.Rolls)continue;
            var spin=t.Body.GetNode<Node3D>("Spin");spin.Rotation=Vector3.Zero;
            // A wagon's axles and wheels are placed on the run at play, not turned by Spin: put each back where it was built.
            foreach(var node in spin.GetChildren())
                if(node is Node3D axle&&axle.Name.ToString().StartsWith("Wheel set"))
                {
                    axle.Transform=axle.GetMeta("parked").AsTransform3D();
                    foreach(var child in axle.GetChildren())if(child is Node3D wheel&&wheel.Name.ToString().StartsWith("Wagon wheel"))wheel.Transform=wheel.GetMeta("parked").AsTransform3D();
                }
        }
        lowered.Clear();foreach(var t in World.Parts.Where(p=>p.Kind=="lock"))Drop(t,0);
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
