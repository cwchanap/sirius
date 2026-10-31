using Godot;
namespace Hoshikawa;

// The opening's staging: the camera shots and clips Prologue.Stage gives each beat, the walks Yui takes between scenes, the fade from
// black, and Mio's room, the first map, which she leaves by its door for the valley.
public partial class Game
{
    // Fixed shots: the eye and what it looks at, in the world or (Room) in the room's own frame, and the seconds the camera glides there.
    // The seconds the camera moves from one shot to a character shot within a scene.
    public const float CameraMove=2;
    public static readonly Dictionary<string,(Vector3 Eye,Vector3 At,float Glide,bool Room)> Shots=new()
    {
        ["sky"]=(new(40,44,24),new(160,100,-120),0,false),["city"]=(new(40,36,24),new(160,22,-120),7,false),
        ["valley"]=(new(36,22,-4),new(0,2,26),7,false),["home"]=(new(23,7,25),new(31,3,7.5f),6,false),
        ["bed"]=(new(.5f,1.15f,-.55f),new(-.6f,.2f,-.65f),0,true),["futon"]=(new(.9f,1.2f,.8f),new(-.6f,.55f,-.35f),2,true),
        ["floor"]=(new(-.1f,1.3f,1),new(.45f,0,0),2,true),["room"]=(new(1.5f,1.6f,1.45f),new(-.6f,1.3f,-.3f),2,true),
        ["shoji"]=(new(.5f,1.55f,-1.45f),new(-1.3f,1,5),2,true),["backlit"]=(new(1.4f,1.35f,-1.6f),new(-.6f,1.1f,1.6f),2,true),
        ["photo"]=(new(1.55f,1.6f,-.55f),new(.9f,1.35f,-1.79f),0,true),
    };
    // Where Yui stands as the story moves her: at Mio's gate, by Renji's bench until he has heard the news, then outside the clinic.
    public static readonly (Vector3 At,float Yaw) YuiWorkshop=(new(-2.6f,0,10.6f),Mathf.Pi/2),YuiClinic=(new(-1.8f,0,34.2f),-.84f);
    public (Vector3 At,float Yaw) YuiSpot=>!Heard.Contains("Gate")?(Hoshikawa.World.YuiGate,Mathf.Pi):QuestStage==0?YuiWorkshop:YuiClinic;
    public static string RestClip(string name)=>name=="Renji"?"work":"idle";
    public Node3D? Room;
    public bool InRoom=>Room!=null;
    public bool Staged=>shot.Current;
    public (Vector3 Eye,Vector3 At) ShotTarget=>shotTo;
    // The black over the screen, lifting as a map or the opening comes in.
    public float Blackout;
    private Camera3D shot=null!;
    private (Vector3 Eye,Vector3 At) shotFrom,shotTo,shotNow;
    private float shotTime,shotLength;
    // A shot on the characters, which follows them as their clips move their heads; a fixed shot stays put.
    private string? framed;
    private readonly List<(string Name,float Yaw)> staged=new();
    private class Move {public string Name="";public Vector3[] Path=null!;public int Next;public float Speed,Yaw;}
    private readonly List<Move> moves=new();
    private void Stage()
    {
        shot=new Camera3D {Name="Scene camera",Fov=50,Near=.05f,Far=320};shot.CullMask&=~Hoshikawa.World.NameLayer;AddChild(shot);
        Actor("Renji").Visual.Play(RestClip("Renji"));
    }
    // A named actor: Mio is the player, whose model turns; a resident turns whole.
    public (Node3D Body,Node3D Turn,ActorVisual Visual) Actor(string name)
    {
        if(name=="Mio")return (Player,Player.Model,Player.Visual);
        var body=World.Residents.First(r=>r.Name==name).Body;return (body,body,body.GetChildren().OfType<ActorVisual>().Single());
    }
    private Vector3 Head(string name)
    {
        var body=Actor(name).Body;var skeleton=ActorVisual.Descendants(body).OfType<Skeleton3D>().First();int bone=skeleton.FindBone("Head");
        return bone>=0?skeleton.GlobalTransform*skeleton.GetBoneGlobalPose(bone).Origin:body.GlobalPosition+Vector3.Up*1.5f;
    }
    // "face A" is in front of A's face, "over A B" looks past A's shoulder at B, "two A B" frames both from the side with A on the left,
    // far enough back that both stand between the portraits at the screen's edges.
    private (Vector3 Eye,Vector3 At) Frame(string name)
    {
        var words=name.Split(' ');
        if(words[0]=="face")
        {var head=Head(words[1]);var ahead=Actor(words[1]).Turn.GlobalBasis.Z;return (Clear(head,head+ahead*1.3f+ahead.Cross(Vector3.Up)*.35f,words),head+Vector3.Down*.12f);}
        if(words[0] is "over" or "two")
        {
            var a=Head(words[1]);var b=Head(words[2]);var along=new Vector3(b.X-a.X,0,b.Z-a.Z).Normalized();var right=along.Cross(Vector3.Up);
            if(words[0]=="over")return (Clear(b,a-along*.95f+right*.5f+Vector3.Up*.12f,words),b+Vector3.Down*.1f);
            var mid=(a+b)/2;return (Clear(mid,mid+right*(a.DistanceTo(b)*1.6f+3)+Vector3.Up*.2f,words),mid+Vector3.Down*.75f);
        }
        var (eye,at,_,room)=Shots[name];
        if(room&&Room!=null)return (Room.GlobalPosition+eye,Room.GlobalPosition+at);
        return (new(eye.X,Math.Max(eye.Y,Hoshikawa.World.Height(eye.X,eye.Z)+3),eye.Z),at);
    }
    // A character shot's eye, drawn in front of a wall or house that would stand between it and the characters it frames.
    private Vector3 Clear(Vector3 at,Vector3 eye,string[] names)
    {
        var query=PhysicsRayQueryParameters3D.Create(at,eye,1);
        query.Exclude=new Godot.Collections.Array<Rid>(names.Where(n=>World.Residents.Any(r=>r.Name==n)).SelectMany(n=>Actor(n).Body.GetChildren().OfType<StaticBody3D>()).Select(b=>b.GetRid()));
        var hit=GetWorld3D().DirectSpaceState.IntersectRay(query);return hit.Count>0?at.Lerp((Vector3)hit["position"],.9f):eye;
    }
    private void Aim(string name)
    {
        shotTo=Frame(name);float glide=Shots.TryGetValue(name,out var s)?s.Glide:CameraMove;framed=Shots.ContainsKey(name)?null:name;
        shotFrom=shot.Current&&glide>0?shotNow:shotTo;shotLength=shot.Current?glide:0;shotTime=0;shot.MakeCurrent();Look(shotFrom);
    }
    private void Look((Vector3 Eye,Vector3 At) view){shotNow=view;shot.GlobalPosition=view.Eye;shot.LookAt(view.At,Vector3.Up);}
    // What the 3D scene does as a beat starts (Prologue.Stage).
    private void Cue(string key)
    {
        if(!Prologue.Stage.TryGetValue(key,out var cue))return;
        foreach(var act in cue.Acts.Split(',',StringSplitOptions.RemoveEmptyEntries|StringSplitOptions.TrimEntries))
        {
            var words=act.Split(' ');Actor(words[0]).Visual.Play(words[1],1,true,.25f);
            // Getting up, she folds the cover in half at the futon's foot.
            if(act=="Mio rise"&&Room?.GetNodeOrNull<Node3D>("Blanket") is {} blanket)
            {var fold=CreateTween().SetParallel();fold.TweenProperty(blanket,"position",blanket.Position+new Vector3(0,-.04f,.33f),.6f).SetDelay(.2f);fold.TweenProperty(blanket,"scale",new Vector3(1,1,.5f),.6f).SetDelay(.2f);}
        }
        if(cue.Shot!="")Aim(cue.Shot);
    }
    // A staged scene turns its residents to Mio and Mio to the first of them; its end turns them back to what they were doing.
    private void Gather()
    {
        if(scene==null)return;
        var names=Prologue.Stage.Where(c=>c.Key.StartsWith(scene+".")).SelectMany(c=>(c.Value.Shot+" "+c.Value.Acts).Replace(","," ").Split(' '))
            .Where(n=>n!="Mio"&&World.Residents.Any(r=>r.Name==n)).Distinct().ToList();
        foreach(var name in names)
        {
            var turn=Actor(name).Turn;staged.Add((name,turn.Rotation.Y));
            var to=Player.GlobalPosition-turn.GlobalPosition;turn.Rotation=new(0,Mathf.Atan2(to.X,to.Z),0);
        }
        if(names.Count>0){var to=Actor(names[0]).Body.GlobalPosition-Player.GlobalPosition;Player.Model.Rotation=new(0,Mathf.Atan2(to.X,to.Z),0);}
    }
    private void Release()
    {
        if(shot.Current)Camera.MakeCurrent();
        foreach(var (name,yaw) in staged){var actor=Actor(name);actor.Turn.Rotation=new(0,yaw,0);actor.Visual.Play(RestClip(name),1,true,.25f);}
        staged.Clear();
    }
    // What a scene's end sets going.
    private void After(string? ended)
    {
        if(ended=="Opening"){EnterRoom();Play(Prologue.Intro);}
        else if(ended=="Gate")Walk("Yui",new[]{new Vector3(30,0,15),new Vector3(.5f,0,15),YuiWorkshop.At},"run",4.2f,YuiWorkshop.Yaw);
        else if(ended=="Briefing")Walk("Yui",new[]{new Vector3(.5f,0,15),new Vector3(.5f,0,31),YuiClinic.At},"walk",1.4f,YuiClinic.Yaw);
    }
    // The title's journey, and a restarted prologue: from black, over the valley to Mio's house, then into her room.
    private void Opening(){ToastTime=0;Player.Visible=false;Blackout=1;Aim("sky");Play(Prologue.Opening);}
    private void EnterRoom()
    {
        Room=Hoshikawa.World.Room();AddChild(Room);World.Visible=false;Player.Visible=true;Sword(false);
        Place(Room.GlobalPosition+Hoshikawa.World.Bed,0,Mathf.Pi*.8f);spring.SpringLength=2.2f;Blackout=1;
    }
    // Out of the door: the valley comes back with Mio on her doorstep, and Yui at the gate the first time.
    public void LeaveRoom()
    {
        ExitRoom();var door=Hoshikawa.World.MioDoor;Place(new(door.X,Hoshikawa.World.Height(door.X,door.Z)+.1f,door.Z),0,Mathf.Pi/2);
        Blackout=1;Toast("HOSHIKAWA",seconds:5);if(!Heard.Contains("Gate"))Play(Prologue.Gate);
    }
    private void ExitRoom(){if(Room==null)return;Room.QueueFree();Room=null;World.Visible=true;Sword(true);spring.SpringLength=7.5f;}
    private void Place(Vector3 at,float yaw,float cameraYaw)
    {
        Player.GlobalPosition=at;Player.Velocity=Vector3.Zero;Player.Model.Rotation=new(0,yaw,0);
        CameraYaw=cameraYaw;CameraPitch=-.29f;cameraRig.Position=at+Vector3.Up*1.35f;
    }
    // The sword stays on its rack at home.
    private void Sword(bool shown){foreach(var m in ActorVisual.Descendants(Player.Model).OfType<MeshInstance3D>().Where(m=>m.Name.ToString().StartsWith("Sword")))m.Visible=shown;}
    private bool Near(Vector3 local,float reach){var p=Player.GlobalPosition-Room!.GlobalPosition;return new Vector2(p.X-local.X,p.Z-local.Z).Length()<reach;}
    public bool AtDoor=>InRoom&&Near(Hoshikawa.World.Door,1.3f);
    public bool AtPhoto=>InRoom&&Near(Hoshikawa.World.Photo,1.1f);
    private void Walk(string name,Vector3[] path,string clip,float speed,float yaw)
    {
        moves.RemoveAll(m=>m.Name==name);moves.Add(new Move {Name=name,Path=path,Speed=speed,Yaw=yaw});Actor(name).Visual.Play(clip,1,true,.2f);
    }
    private void Walking(float dt)
    {
        foreach(var move in moves.ToArray())
        {
            var body=Actor(move.Name).Body;var p=body.GlobalPosition;var to=move.Path[move.Next];var flat=new Vector3(to.X-p.X,0,to.Z-p.Z);float left=flat.Length(),step=move.Speed*dt;
            if(step>=left){p=new(to.X,p.Y,to.Z);if(++move.Next==move.Path.Length){Arrive(move);continue;}}
            else p+=flat/left*step;
            body.GlobalPosition=new(p.X,Ground(p,body),p.Z);
            if(left>.01f)body.Rotation=new(0,Mathf.LerpAngle(body.Rotation.Y,Mathf.Atan2(flat.X,flat.Z),Math.Min(1,dt*10)),0);
        }
    }
    private void Arrive(Move move)
    {
        var (body,_,visual)=Actor(move.Name);var end=move.Path[^1];
        body.GlobalPosition=new(end.X,Hoshikawa.World.Height(end.X,end.Z),end.Z);body.Rotation=new(0,move.Yaw,0);visual.Play(RestClip(move.Name),1,true,.25f);moves.Remove(move);
    }
    // Finishes every walk at once, as a restart or a scene that needs Yui in place does.
    public void FinishWalks(){foreach(var move in moves.ToArray())Arrive(move);}
    private void PlaceYui()
    {
        FinishWalks();var (at,yaw)=YuiSpot;var yui=Actor("Yui");
        yui.Body.GlobalPosition=new(at.X,Hoshikawa.World.Height(at.X,at.Z),at.Z);yui.Body.Rotation=new(0,yaw,0);yui.Visual.Play("idle",1,true);
    }
    // The ground under a walker, bridges included, but not its own collider.
    private float Ground(Vector3 p,Node3D who)
    {
        var query=PhysicsRayQueryParameters3D.Create(p+Vector3.Up*2,p+Vector3.Down*4,1);
        query.Exclude=new Godot.Collections.Array<Rid>(who.GetChildren().OfType<StaticBody3D>().Select(b=>b.GetRid()));
        var hit=GetWorld3D().DirectSpaceState.IntersectRay(query);return hit.Count>0?((Vector3)hit["position"]).Y:Hoshikawa.World.Height(p.X,p.Z);
    }
    private void Staging(float dt)
    {
        if(Mode!=GameMode.Paused)Blackout=Math.Max(0,Blackout-dt*.8f);
        if(Mode is GameMode.Playing or GameMode.Dialogue)Walking(dt);
        if(shot.Current&&shotTime<shotLength){shotTime+=dt;float t=Mathf.SmoothStep(0,1,Math.Min(1,shotTime/shotLength));Look((shotFrom.Eye.Lerp(shotTo.Eye,t),shotFrom.At.Lerp(shotTo.At,t)));}
        else if(shot.Current&&framed!=null){var to=Frame(framed);float k=1-Mathf.Exp(-5*dt);Look((shotNow.Eye.Lerp(to.Eye,k),shotNow.At.Lerp(to.At,k)));}
    }
}
