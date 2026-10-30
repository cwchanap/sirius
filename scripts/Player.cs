using Godot;
namespace Hoshikawa;

public partial class Player : CharacterBody3D
{
    public Game Game = null!;
    // Resonance Cut charges, spent one per cast and refilled only by resting (Chiaki, Emiko, or recovering at the village).
    public const int MaxSkillCharges = 3;
    // Per swing: seconds to its hit, to its link (the follow-through the next swing starts from, where a queued attack waits)
    // and to its end, and the lunge forward (metres) between two times. tools/mio_combat.py keys the clips to the same numbers.
    public static readonly (float Hit,float Link,float End,float Lunge,float From,float To)[] Swings={(.17f,.3f,.55f,.45f,.04f,.2f),(.11f,.26f,.55f,.5f,.02f,.15f),(.3f,.5f,.85f,1.2f,.06f,.3f)};
    // Resonance Cut: a crouch, a leap spinning twice, and the slam that locks every robot within SkillRadius.
    public const float SkillLength=1.3f,SkillSlam=.78f,SkillRadius=5.5f;
    public float Health = 100, Stamina = 100, Posture, Invulnerable, Stun, Spent;
    public int SkillCharges = MaxSkillCharges;
    public readonly Inventory Inventory = new();
    public readonly Growth Growth = new();
    public float MaxHealth => Growth.Value("hp");
    public float MaxStamina => Growth.Value("sp");
    // Run dry, until her stamina is back to Stats.Winded's share.
    public bool Winded { get; private set; }
    public bool Guarding { get; private set; }
    public bool Dodging => dodge > 0;
    public bool Casting => skillCast > 0;
    public float DeflectWindow { get; private set; }
    public ActorVisual Visual = null!;
    public Node3D Model = null!;
    public Vector3 Facing => new(Mathf.Sin(Model.Rotation.Y),0,Mathf.Cos(Model.Rotation.Y));
    private float attack, attackElapsed, dodge, dodgeCooldown, comboGrace, skillCast, healTimer, lunge, sinceRun;
    private bool struck, comboQueued;
    private int combo;
    private Vector3 dodgeDirection;
    private SwordTrail trail=null!;
    private const float JumpVelocity=7.4f, Gravity=22;
    private float landing;
    public override void _Ready()
    {
        Name="Mio";CollisionLayer=2;CollisionMask=1|4;FloorSnapLength=.4f;FloorMaxAngle=Mathf.DegToRad(50);
        AddChild(new CollisionShape3D {Position=new(0,.9f,0),Shape=new CapsuleShape3D {Radius=.3f,Height=1.8f}});
        Model=GD.Load<PackedScene>("res://assets/models/mio.glb").Instantiate<Node3D>();AddChild(Model);Model.Rotation=new(0,Mathf.Pi,0);
        Visual=new ActorVisual();AddChild(Visual);Visual.Bind(Model);Visual.Play("idle");
        // The trail spans the blade between hilt and tip, read from the skinned blade in the hand bone's frame.
        var skeleton=ActorVisual.Descendants(Model).OfType<Skeleton3D>().Single();
        var blade=ActorVisual.Descendants(Model).OfType<MeshInstance3D>().Single(m=>m.Name.ToString().Contains("blade"));
        int bind=Enumerable.Range(0,blade.Skin.GetBindCount()).First(i=>blade.Skin.GetBindName(i)=="RightHand"||blade.Skin.GetBindBone(i)==skeleton.FindBone("RightHand"));
        var points=blade.Mesh.SurfaceGetArrays(0)[(int)Mesh.ArrayType.Vertex].AsVector3Array().Select(v=>blade.Skin.GetBindPose(bind)*v).ToArray();
        var tip=points.MaxBy(v=>v.LengthSquared());var hilt=points.MaxBy(v=>v.DistanceSquaredTo(tip));
        trail=new SwordTrail {Skeleton=skeleton,Bone=skeleton.FindBone("RightHand"),Hilt=hilt,Tip=tip};AddChild(trail);
    }
    public override void _PhysicsProcess(double delta)
    {
        if(Game.Mode!=GameMode.Playing)return;
        float dt=(float)delta;
        Invulnerable=Math.Max(0,Invulnerable-dt);
        Stun=Math.Max(0,Stun-dt);DeflectWindow=Math.Max(0,DeflectWindow-dt);
        dodgeCooldown=Math.Max(0,dodgeCooldown-dt);comboGrace=Math.Max(0,comboGrace-dt);
        healTimer=Math.Max(0,healTimer-dt);Spent=Math.Max(0,Spent-dt);
        landing=Math.Max(0,landing-dt);trail.Emitting=false;
        var input=Input.GetVector("left","right","forward","back");
        var dir=new Vector3(input.X,0,input.Y).Rotated(Vector3.Up,Game.CameraYaw);
        if(Input.IsActionJustPressed("lock"))Game.ToggleLock();
        if(Input.IsActionJustPressed("heal"))Use("remedy");
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
        var swing=Swings[combo];
        if(attack>0)
        {
            attack-=dt;attackElapsed+=dt;trail.Emitting=struck?attackElapsed<swing.Hit+.12f:attackElapsed>swing.Hit-.09f;
            if(!struck&&attackElapsed>=swing.Hit)
            {
                struck=true;bool landed=false;
                foreach(var enemy in Game.Enemies.Where(e=>!e.Dead))
                    if(CanReach(enemy.GlobalPosition,2.9f,.15f)){enemy.TakeHit(Enemy.SwordDamage*Growth.Scale("atk"),Enemy.SwordPosture*Growth.Scale("atk"));landed=true;Cut(enemy,1.8f);}
                if(combo==2){Game.Ring(GlobalPosition+Facing*1.1f,.3f,2.2f,.3f,World.Gold);Game.Dust(GlobalPosition+Facing*1.1f,1.5f,16);Game.CameraShake=.14f;}
                if(landed)Game.HitStop(combo==2?.1f:.055f);
            }
            if(comboQueued&&attackElapsed>=swing.Link){comboQueued=false;Attack();swing=Swings[combo];}
        }
        if(skillCast>0)
        {
            float was=SkillLength-skillCast;skillCast-=dt;float now=SkillLength-skillCast;trail.Emitting=now>.38f&&now<SkillSlam+.04f;
            if(was<SkillSlam&&now>=SkillSlam)
            {
                Game.Slam(GlobalPosition,SkillRadius);Game.Sound("slam");
                Game.CameraShake=.3f;Game.CameraPunch=1;Game.HitStop(.12f);
                var caught=Game.Enemies.Where(e=>!e.Dead&&CanReach(e.GlobalPosition,SkillRadius,-1)).ToList();
                foreach(var enemy in caught){enemy.Stun(Enemy.ResonanceStun);Game.Burst(enemy.GlobalPosition+Vector3.Up*1.3f,World.Jade,1.3f);enemy.Blink(World.Jade);}
                if(caught.Count>0)Game.Sound("stun");
            }
        }
        bool running=Input.IsActionPressed("run")&&!Winded&&!Guarding&&dodge<=0&&dir.LengthSquared()>.01f&&Stun<=0&&skillCast<=0&&healTimer<=0&&attack<=0;
        float speed=Guarding?1.7f:running?7.8f:2f;
        if(Stun>0||skillCast>0||healTimer>0||attack>0)speed=0;
        if(running){Stamina=Math.Max(0,Stamina-Stats.RunCost*dt);sinceRun=0;Winded=Stamina<=0;}
        else{sinceRun+=dt;if(sinceRun>=Stats.RegenDelay)Stamina=Math.Min(MaxStamina,Stamina+Stats.Regen*dt);if(Stamina>=MaxStamina*Stats.Winded)Winded=false;}
        Vector3 horizontal=dir*speed;
        if(attack>0)
        {
            // The lunge's speed is the slope of a smoothstep over its window, so Mio covers exactly the clip's step.
            float u=(attackElapsed-swing.From)/(swing.To-swing.From);
            if(u>0&&u<1)horizontal=Facing*(lunge*6*u*(1-u)/(swing.To-swing.From));
        }
        if(dodge>0){dodge-=dt;horizontal=dodgeDirection*7;}
        else if(dir.LengthSquared()>.01f&&Stun<=0&&skillCast<=0&&attack<=0)
            Model.Rotation=new(0,Mathf.LerpAngle(Model.Rotation.Y,Mathf.Atan2(dir.X,dir.Z),dt*13),0);
        if(Game.Locked is { Dead:false } target&&dodge<=0)
        {var to=target.GlobalPosition-GlobalPosition;Model.Rotation=new(0,Mathf.LerpAngle(Model.Rotation.Y,Mathf.Atan2(to.X,to.Z),dt*12),0);}
        float grip=attack>0?1000:45;
        Velocity=new(Mathf.MoveToward(Velocity.X,horizontal.X,dt*grip),IsOnFloor()&&Velocity.Y<0?-.5f:Velocity.Y-Gravity*dt,Mathf.MoveToward(Velocity.Z,horizontal.Z,dt*grip));
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
            victim.Execute();Invulnerable=.8f;combo=2;attack=Swings[2].End;attackElapsed=0;lunge=0;struck=true;comboQueued=false;comboGrace=0;
            Visual.Play("attack3",1,true);Game.Burst(victim.GlobalPosition+Vector3.Up,World.Gold,2.3f);Cut(victim,2.6f);Game.HitStop(.12f);Game.CameraShake=.18f;Game.Sound("impact");Game.Bark("attack3");return;
        }
        combo=comboGrace>0?(combo+1)%3:0;var swing=Swings[combo];comboGrace=swing.End+.15f;attack=swing.End;attackElapsed=0;struck=false;
        // Each swing turns to the locked robot, or the nearest within reach, else the way she is steering, and lunges no closer than 1.3 m.
        var aim=Game.Locked is {Dead:false} locked&&locked.GlobalPosition.DistanceTo(GlobalPosition)<5?locked
            :Game.Enemies.Where(e=>!e.Dead&&e.GlobalPosition.DistanceTo(GlobalPosition)<3.5f).MinBy(e=>e.GlobalPosition.DistanceSquaredTo(GlobalPosition));
        var input=Input.GetVector("left","right","forward","back");
        var heading=aim!=null?aim.GlobalPosition-GlobalPosition:new Vector3(input.X,0,input.Y).Rotated(Vector3.Up,Game.CameraYaw);heading.Y=0;
        if(heading.LengthSquared()>.01f)Model.Rotation=new(0,Mathf.Atan2(heading.X,heading.Z),0);
        lunge=aim!=null?Math.Clamp(heading.Length()-1.3f,0,swing.Lunge):swing.Lunge;
        // A chained swing starts from the pose the last one links at, so it needs only the shortest blend.
        Guarding=false;trail.Life=.12f;trail.From=.5f;trail.Glow(combo==2?World.Gold:World.Jade,2.5f);Visual.Play("attack"+(combo+1),1,true,.05f);Game.Sound("swing");Game.Bark("attack"+(combo+1));
    }
    // The bright cut a landed blow leaves on a robot's plating, along the way the blade's tip is moving.
    private void Cut(Enemy enemy,float length)
    {
        var toward=GlobalPosition-enemy.GlobalPosition;toward.Y=0;var sweep=trail.Sweep;
        Game.Streak(enemy.GlobalPosition+Vector3.Up*1.25f+toward.Normalized()*.4f,sweep.LengthSquared()>1e-4f?sweep:Facing.Cross(Vector3.Up),new("fff1cf"),length);
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
        if(SkillCharges<=0){Spent=1;Game.Feedback("REST TO RECHARGE",World.Jade);return false;}
        // The spin draws the whole blade round her, its full moon.
        SkillCharges--;skillCast=SkillLength;Invulnerable=SkillSlam+.1f;Guarding=false;trail.Life=.3f;trail.From=.15f;trail.Glow(World.Jade,3);Visual.Play("skill",1,true);Game.Bark("skill");
        Game.Ring(GlobalPosition,2.6f,.4f,.36f,World.Jade);Game.Gather(this,Vector3.Up*1.1f,World.Jade,.38f);return true;
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
                Game.Flash("deflect",World.Gold);Game.CameraShake=.13f;return "deflected";
            }
            Posture+=34;Game.Sound("guard");
            if(Posture>=100){Posture=100;Stun=1.2f;Guarding=false;Visual.Play("stagger",1,true);Game.Flash("posture_break",new("f18470"));}
            return "guarded";
        }
        Health=Math.Max(0,Health-enemy.Strike(perilous)/Growth.Scale("def"));Invulnerable=.65f;Stun=.32f;
        attack=skillCast=0;comboQueued=false;Game.CameraShake=.2f;Visual.Play("stagger",1.3f,true);
        Game.Burst(GlobalPosition+Vector3.Up,new("ee8b76"),.8f);Game.Sound("hurt");Game.Bark("hurt");
        if(Health<=0){Visual.Play("death",1,true);Game.Die();}
        return "hit";
    }
    // Spends one of a consumable on its effect, unless she has none or it would do nothing now.
    public bool Use(string id)
    {
        var item=Items.Get(id);
        return Inventory.Count(id)>0&&item.Use!=null&&item.Use(this)&&Inventory.Take(id);
    }
    public bool Heal()
    {
        if(Health>=MaxHealth||Stun>0||attack>0||skillCast>0||dodge>0||Health<=0)return false;
        Health=Math.Min(MaxHealth,Health+45);healTimer=.65f;
        Game.Burst(GlobalPosition+Vector3.Up*.6f,World.Jade,1.2f);Game.Sound("heal");Game.FloatingNumber(GlobalPosition+Vector3.Up*2.1f,"+45",World.Jade);return true;
    }
    public bool Recharge()
    {
        if(SkillCharges>=MaxSkillCharges||Health<=0)return false;
        SkillCharges++;Spent=0;Game.Burst(GlobalPosition+Vector3.Up*.8f,World.Jade,1.6f);Game.Sound("heal");return true;
    }
    public void Rest(){Health=MaxHealth;Stamina=MaxStamina;Winded=false;Posture=0;Inventory.Fill("remedy");SkillCharges=MaxSkillCharges;Spent=0;}
    public void Respawn()
    {
        GlobalPosition=new(0,World.Height(0,31)+.1f,31);Velocity=Vector3.Zero;Rest();
        Stun=attack=dodge=skillCast=Invulnerable=DeflectWindow=healTimer=landing=0;comboQueued=false;Guarding=false;
        Model.Rotation=new(0,Mathf.Pi,0);Visual.Play("idle",1,true);
    }
}
