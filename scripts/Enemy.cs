using Godot;
namespace Hoshikawa;

public partial class Enemy : CharacterBody3D
{
    // Robot combat tuning, all in one place. Mio's sword only scratches the plating, a robot's strike nearly fells her,
    // and from behind (within BehindDot of its back) her attack shuts it down outright. Resonance Cut only stuns.
    public const float PatrolHealth=500,ScoutHealth=300,SwordDamage=2,SwordPosture=5,StrikeDamage=50,PerilousDamage=75,ScoutStrike=.6f,ResonanceStun=4,BehindDot=-.5f;
    public Game Game=null!;
    public Vector3 Home;
    public int Index;
    // Scouts are smaller, weaker and keep closer to home; the relay mission does not count them.
    public bool Scout;
    public float MaxHealth=>Scout?ScoutHealth:PatrolHealth;
    public float Health=PatrolHealth, Posture;
    public bool Dead=>Health<=0;
    public bool Broken=>broken>0&&!Dead;
    public bool Stunned=>stunned>0&&!Dead;
    public Vector3 Forward=>new(Mathf.Sin(Model.Rotation.Y),0,Mathf.Cos(Model.Rotation.Y));
    public bool Perilous {get;private set;}
    public string State {get;private set;}="PATROL";
    public ActorVisual Visual=null!;
    public Node3D Model=null!;
    private Label3D marker=null!;
    private float clock,attackTimer,recovery,broken,stagger,stunned;
    private bool attacking,struck;
    private int swings;
    private Vector3 attackDirection;
    public override void _Ready()
    {
        Name=(Scout?"Scout_":"Patrol_")+Index;Health=MaxHealth;CollisionLayer=4;CollisionMask=1|2|4;FloorSnapLength=.5f;
        AddChild(new CollisionShape3D {Position=new(0,1,0),Shape=new CapsuleShape3D {Radius=.38f,Height=2}});
        Model=GD.Load<PackedScene>("res://assets/models/patrol.glb").Instantiate<Node3D>();AddChild(Model);Model.Scale=Vector3.One*(Scout?.86f:1.12f);
        Visual=new ActorVisual();AddChild(Visual);Visual.Bind(Model);Visual.Play("walk");
        marker=World.Label(this,"",new(0,2.8f,0),World.Gold,38);clock=Index*2;
    }
    public override void _PhysicsProcess(double delta)
    {
        if(Game.Mode!=GameMode.Playing||Dead)return;
        float dt=(float)delta;clock+=dt;recovery=Math.Max(0,recovery-dt);stagger=Math.Max(0,stagger-dt);
        var to=Game.Player.GlobalPosition-GlobalPosition;float distance=new Vector2(to.X,to.Z).Length();to.Y=0;
        Vector3 movement=Vector3.Zero;
        if(broken>0)
        {
            broken-=dt;State="BROKEN";marker.Text="◆  FINISH";marker.Modulate=World.Gold;
            if(broken<=0){Posture=40;recovery=1;marker.Text="";}
        }
        else if(stunned>0)
        {
            stunned-=dt;State="STUNNED";marker.Text="◇  STUNNED";marker.Modulate=World.Jade;
            if(stunned<=0){recovery=.6f;marker.Text="";}
        }
        else if(attacking)
        {
            attackTimer-=dt;State=attackTimer>.22f?(Perilous?"PERILOUS":"WINDUP"):"STRIKE";
            marker.Text=attackTimer>.22f?(Perilous?"◆  DODGE / JUMP":"◇"):"";
            marker.Modulate=Perilous?new("ff6d58"):World.Gold;
            if(attackTimer<=.22f&&!struck)
            {
                struck=true;Visual.Play("attack2",1.6f,true);Game.Slash(GlobalPosition+Vector3.Up,attackDirection,Perilous);
                if(distance<2.9f&&Math.Abs(Game.Player.GlobalPosition.Y-GlobalPosition.Y)<(Perilous?.75f:1.5f)
                    &&attackDirection.Dot(to.Normalized())>.1f&&Game.ClearSight(GlobalPosition+Vector3.Up,Game.Player.GlobalPosition+Vector3.Up))
                    Game.Player.ReceiveHit(this,Perilous);
            }
            if(attackTimer<=0){attacking=false;recovery=1.05f;marker.Text="";}
        }
        else if(stagger>0){State="STAGGER";}
        // Only on its own level: a patrol on the teahouse terrace never walks off its cliff after Mio in the junction below.
        else if(distance<13&&Math.Abs(Game.Player.GlobalPosition.Y-GlobalPosition.Y)<2.5f&&Game.Player.GlobalPosition.Z<World.RelaySouth&&GlobalPosition.DistanceTo(Home)<20
                &&Game.ClearSight(GlobalPosition+Vector3.Up,Game.Player.GlobalPosition+Vector3.Up))
        {
            State="PURSUE";
            // While it recovers from a swing, a deflect or a stun it neither moves nor turns: the opening to get round to its back.
            if(recovery>0)Visual.Play("guard");
            else
            {
                if(distance>2.2f){movement=to.Normalized()*2.8f;Visual.Play("run",.8f);}
                else BeginAttack(to.Normalized());
                Model.Rotation=new(0,Mathf.LerpAngle(Model.Rotation.Y,Mathf.Atan2(to.X,to.Z),dt*5),0);
            }
        }
        else
        {
            State="PATROL";Posture=Math.Max(0,Posture-dt*12);
            var destination=Home+new Vector3(Mathf.Sin(clock*.27f+Index)*3.2f,0,Mathf.Cos(clock*.27f+Index)*2.4f)*(Scout?.5f:1);
            var d=destination-GlobalPosition;d.Y=0;movement=d.Normalized()*1.25f;
            Model.Rotation=new(0,Mathf.LerpAngle(Model.Rotation.Y,Mathf.Atan2(d.X,d.Z),dt*3),0);Visual.Play("walk",.8f);
        }
        Velocity=new(movement.X,IsOnFloor()?-.5f:Velocity.Y-22*dt,movement.Z);MoveAndSlide();
        if(GlobalPosition.Y<-8)Reset();
    }
    private void BeginAttack(Vector3 direction)
    {
        swings++;Perilous=swings%3==0;attackTimer=Perilous?1.15f:.95f;attacking=true;struck=false;attackDirection=direction;
        Visual.Play("guard",.7f,true);Game.Sound("warning");
    }
    public float Strike(bool perilous)=>(perilous?PerilousDamage:StrikeDamage)*(Scout?ScoutStrike:1);
    public static bool Behind(Vector3 forward,Vector3 robot,Vector3 mio)
    {var d=mio-robot;d.Y=0;return d.LengthSquared()>.0001f&&forward.Dot(d.Normalized())<=BehindDot;}
    public bool ExposedTo(Vector3 mio)=>!Dead&&Behind(Forward,GlobalPosition,mio);
    public void Stun(float seconds)
    {
        if(Dead||Broken)return;
        stunned=seconds;attacking=false;stagger=0;Visual.Play("stagger",.4f,true);
    }
    public void TakeHit(float damage,float posture)
    {
        if(Dead)return;
        Health=Math.Max(0,Health-damage);Posture=Math.Min(100,Posture+posture);
        if(damage>0)
        {
            Game.Burst(GlobalPosition+Vector3.Up*1.3f,World.Gold,.7f);Game.Sound("impact");
            Game.FloatingNumber(GlobalPosition+Vector3.Up*2.1f,((int)damage).ToString(),World.Gold);
        }
        if(Health<=0){Execute();return;}
        if(Posture>=100)
        {
            broken=4.5f;attacking=false;stagger=stunned=0;Visual.Play("stagger",.65f,true);Game.Sound("break");
            Game.Feedback("POSTURE BREAK  ·  ATTACK TO FINISH",World.Gold);
        }
        else if(damage==0)
        {stagger=.22f;attacking=false;recovery=.6f;Visual.Play("stagger",1.6f,true);}
    }
    public void Execute()
    {
        if(State=="DEFEATED")return;
        Health=0;State="DEFEATED";marker.Text="";CollisionLayer=0;CollisionMask=0;
        Visual.Play("death",1.5f,true);Game.Sound("break");
        Game.FloatingNumber(GlobalPosition+Vector3.Up*2,"DISABLED",World.Jade);Game.OnEnemyDefeated();
    }
    public void Reset()
    {
        GlobalPosition=Home+Vector3.Up*.15f;Velocity=Vector3.Zero;Health=MaxHealth;Posture=0;broken=attackTimer=recovery=stagger=stunned=0;
        attacking=struck=false;swings=0;CollisionLayer=4;CollisionMask=1|2|4;State="PATROL";marker.Text="";Visual.Play("idle",1,true);
    }
}
