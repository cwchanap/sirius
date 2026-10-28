using Godot;
namespace Hoshikawa;

// A kind of robot: its health, its strike as a share of a patrol's, its model's scale, how far its patrol loop reaches as a share of a
// patrol's, and the stardust it leaves Mio. A flying one has no legs and holds its body over the ground on a thruster.
public record RobotKind(string Name,float Health,float Strike,float Size,float Roam,int Reward,bool Flies=false);
public partial class Enemy : CharacterBody3D
{
    // Robot combat tuning, all in one place. Mio's sword only scratches the plating, a robot's strike nearly fells her,
    // and from behind (within BehindDot of its back) her attack shuts it down outright. Resonance Cut only stuns.
    public const float SwordDamage=2,SwordPosture=5,StrikeDamage=50,PerilousDamage=75,ResonanceStun=4,BehindDot=-.5f;
    // The patrols are the three the relay mission counts. Scouts are smaller, weaker and keep closer to home. Drones are the easy ones,
    // stardust to level on: a full combo of the sword shuts one down, each hit knocks it off its swing, and it never winds up the perilous sweep.
    // A new kind is a row here and its homes in World.
    public static readonly RobotKind Patrol=new("Patrol",500,1,1.12f,1,Stats.PatrolReward),Scout=new("Scout",300,.6f,.86f,.5f,Stats.ScoutReward),
        Drone=new("Drone",6,.3f,.55f,.6f,Stats.DroneReward,true);
    // A flyer's origin keeps Hover over the ground, bobbing, and carries its model Lift higher, its body where a patrol's chest would be.
    public const float Hover=.1f,Lift=.6f;
    private static readonly string[] Legs={"Thigh","Knee","Greave","Robot foot"};
    private static readonly Color Amber=new("ffa552");
    public Game Game=null!;
    public Vector3 Home;
    public int Index;
    public RobotKind Kind=Patrol;
    public float MaxHealth=>Kind.Health;
    public float Health, Posture;
    public bool Dead=>Health<=0;
    public bool Broken=>broken>0&&!Dead;
    public bool Stunned=>stunned>0&&!Dead;
    public Vector3 Forward=>new(Mathf.Sin(Model.Rotation.Y),0,Mathf.Cos(Model.Rotation.Y));
    public bool Perilous {get;private set;}
    public string State {get;private set;}="PATROL";
    // The icon over its head (assets/art/ui/): perilous, finish, stunned, or shutdown when Mio stands where her attack would shut it down.
    public string Badge {get;private set;}="";
    public ActorVisual Visual=null!;
    public Node3D Model=null!;
    private Label3D marker=null!;
    private Sprite3D badge=null!,badgeShade=null!;
    private CpuParticles3D crackle=null!;
    private Node3D? thruster;
    private Tween? fall;
    private float clock,attackTimer,recovery,broken,stagger,stunned;
    private bool attacking,struck;
    private int swings;
    private Vector3 attackDirection;
    public override void _Ready()
    {
        Name=Kind.Name+"_"+Index;Health=MaxHealth;CollisionLayer=4;CollisionMask=1|2|4;FloorSnapLength=.5f;
        AddChild(new CollisionShape3D {Position=new(0,Kind.Flies?1.3f:1,0),Shape=Kind.Flies?new SphereShape3D {Radius=.4f}:new CapsuleShape3D {Radius=.38f,Height=2}});
        Model=GD.Load<PackedScene>("res://assets/models/patrol.glb").Instantiate<Node3D>();AddChild(Model);Model.Scale=Vector3.One*Kind.Size;
        if(Kind.Flies)
        {
            MotionMode=MotionModeEnum.Floating;Model.Position=Vector3.Up*Lift;
            foreach(var part in ActorVisual.Descendants(Model).OfType<MeshInstance3D>().Where(m=>Legs.Any(m.Name.ToString().Contains)))part.Visible=false;
            // An amber glow under its pelvis and a jet streaming down from it, left behind as it moves.
            thruster=new Node3D {Name="Thruster",Position=new(0,Lift+.42f,0)};AddChild(thruster);
            var glow=World.Mesh(thruster,"Thruster glow",new QuadMesh(),Vector3.Zero,Game.Fx(Amber,1.6f,Game.Soft,true));glow.Scale=Vector3.One*.35f;glow.CastShadow=GeometryInstance3D.ShadowCastingSetting.Off;
            thruster.AddChild(new CpuParticles3D {Name="Thruster jet",Amount=10,Lifetime=.3,LocalCoords=false,Mesh=new SphereMesh {Radius=.02f,Height=.04f,RadialSegments=6,Rings=3},
                MaterialOverride=Game.Fx(Amber,2.5f),Direction=Vector3.Down,Spread=12,InitialVelocityMin=1.5f,InitialVelocityMax=2.5f,Gravity=Vector3.Zero,ColorRamp=Game.Cool,
                CastShadow=GeometryInstance3D.ShadowCastingSetting.Off});
        }
        Visual=new ActorVisual();AddChild(Visual);Visual.Bind(Model);Visual.Play("walk");
        marker=World.Label(this,"",new(0,2.8f,0),World.Gold,38);clock=Index*2;
        // The shade, a little larger and drawn first, keeps the tinted icon readable against the sky.
        foreach(var (size,priority) in new[]{(.0029f,0),(.0025f,1)})
        {var s=new Sprite3D {Position=new(0,2.9f,0),PixelSize=size,RenderPriority=priority,Billboard=BaseMaterial3D.BillboardModeEnum.Enabled,TextureFilter=BaseMaterial3D.TextureFilterEnum.LinearWithMipmaps};AddChild(s);if(priority==0){badgeShade=s;s.Modulate=new(.04f,.08f,.09f,.75f);}else badge=s;}
        // Jade sparks jump over the plating while Resonance Cut holds its actuators.
        crackle=new CpuParticles3D {Name="Stun crackle",Position=new(0,1.2f,0),Amount=14,Lifetime=.16,Emitting=false,LocalCoords=false,Randomness=1,Mesh=new BoxMesh {Size=new(.015f,.16f,.015f)},
            MaterialOverride=Game.Fx(World.Jade,4),ParticleFlagAlignY=true,EmissionShape=CpuParticles3D.EmissionShapeEnum.Box,EmissionBoxExtents=new(.35f,.8f,.35f),
            Spread=180,InitialVelocityMin=1.5f,InitialVelocityMax=3,Gravity=Vector3.Zero,ColorRamp=Game.Cool,CastShadow=GeometryInstance3D.ShadowCastingSetting.Off};
        AddChild(crackle);
    }
    public override void _PhysicsProcess(double delta)
    {
        if(crackle.Emitting!=Stunned)crackle.Emitting=Stunned;
        if(Game.Mode!=GameMode.Playing||Dead)return;
        float dt=(float)delta;clock+=dt;recovery=Math.Max(0,recovery-dt);stagger=Math.Max(0,stagger-dt);
        var to=Game.Player.GlobalPosition-GlobalPosition;float distance=new Vector2(to.X,to.Z).Length();to.Y=0;
        Vector3 movement=Vector3.Zero;
        if(broken>0)
        {
            broken-=dt;State="BROKEN";
            if(broken<=0){Posture=40;recovery=1;}
        }
        else if(stunned>0)
        {
            stunned-=dt;State="STUNNED";
            if(stunned<=0)recovery=.6f;
        }
        else if(attacking)
        {
            attackTimer-=dt;State=attackTimer>.22f?(Perilous?"PERILOUS":"WINDUP"):"STRIKE";
            if(attackTimer<=.22f&&!struck)
            {
                struck=true;Visual.Play("attack2",1.6f,true);Game.Slash(GlobalPosition+Vector3.Up,attackDirection,Perilous);
                if(distance<2.9f&&Math.Abs(Game.Player.GlobalPosition.Y-GlobalPosition.Y)<(Perilous?.75f:1.5f)
                    &&attackDirection.Dot(to.Normalized())>.1f&&Game.ClearSight(GlobalPosition+Vector3.Up,Game.Player.GlobalPosition+Vector3.Up))
                    Game.Player.ReceiveHit(this,Perilous);
            }
            if(attackTimer<=0){attacking=false;recovery=1.05f;}
        }
        else if(stagger>0){State="STAGGER";}
        // While it recovers from a swing, a deflect or a stun it neither moves nor turns, in sight of Mio or not: the opening to get round to its back.
        else if(recovery>0){State="RECOVER";Visual.Play("guard");}
        // Only on its own level: a patrol on the teahouse terrace never walks off its cliff after Mio in the junction below.
        else if(distance<13&&Math.Abs(Game.Player.GlobalPosition.Y-GlobalPosition.Y)<2.5f&&Game.Player.GlobalPosition.Z<World.RelaySouth&&GlobalPosition.DistanceTo(Home)<20
                &&Game.ClearSight(GlobalPosition+Vector3.Up,Game.Player.GlobalPosition+Vector3.Up))
        {
            State="PURSUE";
            if(distance>2.2f){movement=to.Normalized()*2.8f;Visual.Play(Kind.Flies?"idle":"run",.8f);}
            else BeginAttack(to.Normalized());
            Model.Rotation=new(0,Mathf.LerpAngle(Model.Rotation.Y,Mathf.Atan2(to.X,to.Z),dt*5),0);
        }
        else
        {
            State="PATROL";Posture=Math.Max(0,Posture-dt*12);
            var destination=Home+new Vector3(Mathf.Sin(clock*.27f+Index)*3.2f,0,Mathf.Cos(clock*.27f+Index)*2.4f)*Kind.Roam;
            var d=destination-GlobalPosition;d.Y=0;movement=d.Normalized()*1.25f;
            Model.Rotation=new(0,Mathf.LerpAngle(Model.Rotation.Y,Mathf.Atan2(d.X,d.Z),dt*3),0);Visual.Play(Kind.Flies?"idle":"walk",.8f);
        }
        marker.Text=State=="WINDUP"?"◇":"";
        ShowBadge(Broken?"finish":Game.Player.CanShutDown(this)?"shutdown":Stunned?"stunned":State=="PERILOUS"?"perilous":"");
        // A flyer eases back to its height over whatever ground it crosses; a walker falls to it.
        float rise=Kind.Flies?(World.Height(GlobalPosition.X,GlobalPosition.Z)+Hover+Mathf.Sin(clock*2.4f)*.08f-GlobalPosition.Y)*6:IsOnFloor()?-.5f:Velocity.Y-22*dt;
        Velocity=new(movement.X,rise,movement.Z);MoveAndSlide();
        if(GlobalPosition.Y<-8)Reset();
    }
    private void ShowBadge(string icon)
    {
        if(icon==Badge)return;
        Badge=icon;badge.Texture=badgeShade.Texture=icon==""?null:Hud.Icon(icon);badge.Modulate=icon switch {"perilous"=>new("ff6d58"),"stunned"=>World.Jade,_=>World.Gold};
    }
    private void BeginAttack(Vector3 direction)
    {
        swings++;Perilous=!Kind.Flies&&swings%3==0;attackTimer=Perilous?1.15f:.95f;attacking=true;struck=false;attackDirection=direction;
        Visual.Play("guard",.7f,true);Game.Sound("warning");
    }
    public float Strike(bool perilous)=>(perilous?PerilousDamage:StrikeDamage)*Kind.Strike;
    public static bool Behind(Vector3 forward,Vector3 robot,Vector3 mio)
    {var d=mio-robot;d.Y=0;return d.LengthSquared()>.0001f&&forward.Dot(d.Normalized())<=BehindDot;}
    public bool ExposedTo(Vector3 mio)=>!Dead&&Behind(Forward,GlobalPosition,mio);
    public void Stun(float seconds)
    {
        if(Dead||Broken)return;
        stunned=seconds;attacking=false;stagger=0;crackle.Emitting=true;Visual.Play("stagger",.4f,true);
    }
    public void TakeHit(float damage,float posture)
    {
        if(Dead)return;
        Health=Math.Max(0,Health-damage);Posture=Math.Min(100,Posture+posture);
        if(damage>0)
        {
            var away=GlobalPosition-Game.Player.GlobalPosition;away.Y=0;away=away.Normalized();
            Game.Impact(GlobalPosition+Vector3.Up*1.25f-away*.35f,away,World.Gold);Blink(new("fff1cf"));Game.Sound("impact");
            Game.FloatingNumber(GlobalPosition+Vector3.Up*2.1f,((int)damage).ToString(),World.Gold);
        }
        if(Health<=0){Execute();return;}
        if(Posture>=100)
        {
            broken=4.5f;attacking=false;stagger=stunned=0;Visual.Play("stagger",.65f,true);Game.Sound("break");
            Game.Flash("posture_break",World.Gold);
        }
        else if(damage==0||Kind.Flies)
        {stagger=.22f;attacking=false;recovery=.6f;Visual.Play("stagger",1.6f,true);}
    }
    // The plating flares for a moment when struck, held through the hit-stop, which slows everything but this.
    public void Blink(Color c)
    {
        var flare=new StandardMaterial3D {ShadingMode=BaseMaterial3D.ShadingModeEnum.Unshaded,Transparency=BaseMaterial3D.TransparencyEnum.Alpha,BlendMode=BaseMaterial3D.BlendModeEnum.Add,AlbedoColor=new(c,.4f)};
        var plates=ActorVisual.Descendants(Model).OfType<MeshInstance3D>().ToArray();foreach(var plate in plates)plate.MaterialOverlay=flare;
        GetTree().CreateTimer(.06,true,false,true).Timeout+=()=>{foreach(var plate in plates)if(plate.MaterialOverlay==flare)plate.MaterialOverlay=null;};
    }
    public void Execute()
    {
        if(State=="DEFEATED")return;
        Health=0;State="DEFEATED";marker.Text="";ShowBadge("");CollisionLayer=0;CollisionMask=0;
        Visual.Play("death",1.5f,true);Game.Sound("break");
        // A drone's thruster cuts out and it drops to lie on the ground.
        if(thruster!=null){thruster.Visible=false;fall=CreateTween();fall.TweenProperty(Model,"position:y",.12f,.45f).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);}
        Game.FloatingNumber(GlobalPosition+Vector3.Up*2,"DISABLED",World.Jade);Game.OnEnemyDefeated(this);
    }
    public void Reset()
    {
        GlobalPosition=Home+Vector3.Up*.15f;Velocity=Vector3.Zero;Health=MaxHealth;Posture=0;broken=attackTimer=recovery=stagger=stunned=0;
        attacking=struck=false;swings=0;CollisionLayer=4;CollisionMask=1|2|4;State="PATROL";marker.Text="";ShowBadge("");Visual.Play("idle",1,true);
        if(thruster!=null){fall?.Kill();thruster.Visible=true;Model.Position=Vector3.Up*Lift;}
    }
}
