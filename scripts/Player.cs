using Godot;
namespace Hoshikawa;

public partial class Player : CharacterBody3D
{
    public Game Game = null!;
    // Resonance Cut charges, spent one per cast and refilled only by resting (Aki, Emi, or recovering at the village).
    public const int MaxSkillCharges = 3;
    public float Health = 100, Posture, Invulnerable, Stun;
    public int Remedies = 3, MaxRemedies = 3, SkillCharges = MaxSkillCharges;
    public bool Guarding { get; private set; }
    public bool Dodging => dodge > 0;
    public float DeflectWindow { get; private set; }
    public ActorVisual Visual = null!;
    public Node3D Model = null!;
    public Vector3 Facing => new(Mathf.Sin(Model.Rotation.Y),0,Mathf.Cos(Model.Rotation.Y));
    private float attack, attackElapsed, dodge, dodgeCooldown, comboGrace, skillCast, healTimer;
    private bool struck, comboQueued;
    private int combo;
    private Vector3 dodgeDirection;
    private const float JumpVelocity=7.4f, Gravity=22;
    private float landing;
    public override void _Ready()
    {
        Name="Mio";CollisionLayer=2;CollisionMask=1|4;FloorSnapLength=.4f;FloorMaxAngle=Mathf.DegToRad(50);
        AddChild(new CollisionShape3D {Position=new(0,.9f,0),Shape=new CapsuleShape3D {Radius=.3f,Height=1.8f}});
        Model=GD.Load<PackedScene>("res://assets/models/mio.glb").Instantiate<Node3D>();AddChild(Model);Model.Rotation=new(0,Mathf.Pi,0);
        Visual=new ActorVisual();AddChild(Visual);Visual.Bind(Model);Visual.Play("idle");
    }
    public override void _PhysicsProcess(double delta)
    {
        if(Game.Mode!=GameMode.Playing)return;
        float dt=(float)delta;
        Invulnerable=Math.Max(0,Invulnerable-dt);
        Stun=Math.Max(0,Stun-dt);DeflectWindow=Math.Max(0,DeflectWindow-dt);
        dodgeCooldown=Math.Max(0,dodgeCooldown-dt);comboGrace=Math.Max(0,comboGrace-dt);
        healTimer=Math.Max(0,healTimer-dt);
        landing=Math.Max(0,landing-dt);
        var input=Input.GetVector("left","right","forward","back");
        var dir=new Vector3(input.X,0,input.Y).Rotated(Vector3.Up,Game.CameraYaw);
        if(Input.IsActionJustPressed("lock"))Game.ToggleLock();
        if(Input.IsActionJustPressed("heal"))Heal();
        Guarding=Input.IsActionPressed("guard")&&attack<=0&&dodge<=0&&skillCast<=0&&Stun<=0;
        if(Input.IsActionJustPressed("guard")&&Guarding)DeflectWindow=.19f;
        if(Stun<=0&&healTimer<=0)
        {
            if(Input.IsActionJustPressed("dodge")&&dodgeCooldown<=0&&skillCast<=0) Dodge(dir);
            if(Input.IsActionJustPressed("jump")&&IsOnFloor()&&dodge<=0&&skillCast<=0) {Velocity=new(Velocity.X,JumpVelocity,Velocity.Z);landing=0;Game.Sound("jump");}
            if(Input.IsActionJustPressed("attack")&&dodge<=0&&skillCast<=0)
            {
                if(attack>0)comboQueued=true;else Attack();
            }
            if(Input.IsActionJustPressed("skill"))Skill();
        }
        if(attack>0)
        {
            attack-=dt;attackElapsed+=dt;
            if(!struck&&attackElapsed>.17f+combo*.035f)
            {
                struck=true;Game.Slash(GlobalPosition+Vector3.Up, Facing, false);
                foreach(var enemy in Game.Enemies.Where(e=>!e.Dead))
                    if(CanReach(enemy.GlobalPosition,2.9f,.15f)) enemy.TakeHit(Enemy.SwordDamage,Enemy.SwordPosture);
            }
            if(attack<=0&&comboQueued){comboQueued=false;Attack();}
        }
        if(skillCast>0)
        {
            float previous=skillCast;skillCast-=dt;
            if(previous>.4f&&skillCast<=.4f)
            {
                Game.Burst(GlobalPosition+Vector3.Up*.8f,World.Jade,5.2f);Game.Sound("skill");
                foreach(var enemy in Game.Enemies.Where(e=>!e.Dead))
                    if(CanReach(enemy.GlobalPosition,5.5f,-1))enemy.Stun(Enemy.ResonanceStun);
                Game.Toast("RESONANCE CUT", "Zero: their actuators are locked. Get behind them.",2.5f);
            }
        }
        float speed=Guarding?1.7f:Input.IsActionPressed("run")?7.8f:2f;
        if(attack>0)speed=1.4f;
        if(Stun>0||skillCast>0||healTimer>0)speed=0;
        Vector3 horizontal=dir*speed;
        if(dodge>0){dodge-=dt;horizontal=dodgeDirection*7;}
        else if(dir.LengthSquared()>.01f&&Stun<=0&&skillCast<=0)
            Model.Rotation=new(0,Mathf.LerpAngle(Model.Rotation.Y,Mathf.Atan2(dir.X,dir.Z),dt*13),0);
        if(Game.Locked is { Dead:false } target&&dodge<=0)
        {var to=target.GlobalPosition-GlobalPosition;Model.Rotation=new(0,Mathf.LerpAngle(Model.Rotation.Y,Mathf.Atan2(to.X,to.Z),dt*12),0);}
        Velocity=new(Mathf.MoveToward(Velocity.X,horizontal.X,dt*45),IsOnFloor()&&Velocity.Y<0?-.5f:Velocity.Y-Gravity*dt,Mathf.MoveToward(Velocity.Z,horizontal.Z,dt*45));
        bool wasOnFloor=IsOnFloor();
        MoveAndSlide();
        if(GlobalPosition.Y<-8)Respawn();
        if(Stun<=0)Posture=Math.Max(0,Posture-dt*(Guarding?8:18));
        if(attack<=0&&dodge<=0&&skillCast<=0&&Stun<=0&&healTimer<=0)
        {
            if(!IsOnFloor())
            {
                // Tripo frames 2/6/9 are takeoff/apex/descent. Hold descent on long falls.
                float frame=Velocity.Y>=0?Mathf.Lerp(2,6,Mathf.Clamp(1-Velocity.Y/JumpVelocity,0,1))
                    :Mathf.Lerp(6,9,Mathf.Clamp(-Velocity.Y/JumpVelocity,0,1));
                // NLA export retains frame 1 at 1/24 second.
                Visual.Play("jump",0,wasOnFloor);Visual.Animator!.Seek(frame/24.0,true);
                landing=0;
            }
            else if(!wasOnFloor)
            {
                // Contact/recovery (frames 10–13) starts on actual collision, not a timer in midair.
                Visual.Play("jump",1,true);Visual.Animator!.Seek(10/24.0,true);landing=3/24f;
            }
            else if(landing<=0)
            {
                // Played this fast, the walk and run feet slide least at 2 and 7.8 m/s (measured in game: 0.36 and 1.9 m/s).
                string clip=Guarding?"guard":dir.Length()>.1f?(speed>5?"run":"walk"):"idle";
                Visual.Play(clip,clip=="walk"?speed/1.26f:clip=="run"?2:1);
            }
        }
    }
    public bool CanReach(Vector3 target,float range,float facingDot)
    {
        var d=target-GlobalPosition;
        if(new Vector2(d.X,d.Z).Length()>range||Math.Abs(d.Y)>1.7f)return false;
        var horizontal=new Vector3(d.X,0,d.Z).Normalized();
        return Facing.Dot(horizontal)>=facingDot&&Game.ClearSight(GlobalPosition+Vector3.Up,target+Vector3.Up);
    }
    public void Attack()
    {
        if(Stun>0||dodge>0||skillCast>0||Health<=0)return;
        var victim=Game.Enemies.FirstOrDefault(CanShutDown);
        if(victim!=null)
        {
            victim.Execute();Invulnerable=.8f;attack=.75f;attackElapsed=0;struck=true;comboQueued=false;
            Visual.Play("attack3",1,true);Game.Burst(victim.GlobalPosition+Vector3.Up,World.Gold,2.3f);return;
        }
        combo=comboGrace>0?(combo+1)%3:0;comboGrace=1.3f;attack=.48f+combo*.08f;attackElapsed=0;struck=false;
        Guarding=false;Visual.Play("attack"+(combo+1),1.2f,true);Game.Sound("swing");
    }
    // From behind, or once its posture breaks, a robot within reach is shut down by the next attack.
    public bool CanShutDown(Enemy e)=>(e.Broken||e.ExposedTo(GlobalPosition))&&CanReach(e.GlobalPosition,3,-.2f);
    public void Dodge(Vector3 direction)
    {
        if(dodgeCooldown>0||Stun>0||Health<=0)return;
        dodge=.6f;dodgeCooldown=.85f;Invulnerable=.4f;
        dodgeDirection=direction.LengthSquared()>.01f?direction.Normalized():Facing;
        attack=0;comboQueued=false;Guarding=false;Visual.Play("dodge",1,true);Game.Sound("dodge");
    }
    public bool Skill()
    {
        if(attack>0||dodge>0||skillCast>0||Stun>0||healTimer>0||Health<=0)return false;
        if(SkillCharges<=0){Game.Feedback("ZERO LINK SPENT  ·  REST TO RECHARGE",World.Jade);return false;}
        SkillCharges--;skillCast=.95f;Invulnerable=.55f;Guarding=false;Visual.Play("skill",1.2f,true);return true;
    }
    public string ReceiveHit(Enemy enemy,bool perilous)
    {
        if(Health<=0||Invulnerable>0)return "evaded";
        var delta=enemy.GlobalPosition-GlobalPosition;delta.Y=0;
        bool front=Facing.Dot(delta.Normalized())>0;
        if(Guarding&&front&&!perilous&&Stun<=0)
        {
            if(DeflectWindow>0)
            {
                enemy.TakeHit(0,48);Posture=Math.Max(0,Posture-18);DeflectWindow=0;
                Game.Burst(GlobalPosition+Vector3.Up*1.2f,World.Gold,1.2f);Game.Sound("deflect");
                Game.Feedback("DEFLECT",World.Gold);Game.CameraShake=.13f;return "deflected";
            }
            Posture+=34;Game.Sound("guard");Game.Feedback("GUARD",World.Gold);
            if(Posture>=100){Posture=100;Stun=1.2f;Guarding=false;Visual.Play("stagger",1,true);Game.Feedback("POSTURE BROKEN",new("f18470"));}
            return "guarded";
        }
        Health=Math.Max(0,Health-enemy.Strike(perilous));Invulnerable=.65f;Stun=.32f;
        attack=skillCast=0;comboQueued=false;Game.CameraShake=.2f;Visual.Play("stagger",1.3f,true);
        Game.Burst(GlobalPosition+Vector3.Up,new("ee8b76"),.8f);Game.Sound("hurt");
        if(Health<=0){Visual.Play("death",1,true);Game.Die();}
        return "hit";
    }
    public void Heal()
    {
        if(Remedies<=0||Health>=100||Stun>0||attack>0||skillCast>0||dodge>0||Health<=0)return;
        Remedies--;Health=Math.Min(100,Health+45);healTimer=.65f;
        Game.Burst(GlobalPosition+Vector3.Up*.6f,World.Jade,1.2f);Game.Sound("heal");Game.Feedback("+45 VITALITY",World.Jade);
    }
    public void Rest(){Health=100;Posture=0;Remedies=MaxRemedies;SkillCharges=MaxSkillCharges;}
    public void Respawn()
    {
        GlobalPosition=new(0,World.Height(0,31)+.1f,31);Velocity=Vector3.Zero;Rest();
        Stun=attack=dodge=skillCast=Invulnerable=DeflectWindow=healTimer=landing=0;comboQueued=false;Guarding=false;
        Model.Rotation=new(0,Mathf.Pi,0);Visual.Play("idle",1,true);
    }
}
