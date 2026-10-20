using Godot;
namespace Hoshikawa;

public enum GameMode { Title, Playing, Dialogue, Paused, Defeated }
public partial class Game : Node3D
{
    public World World=null!;
    public Player Player=null!;
    public readonly List<Enemy> Enemies=new();
    public Enemy? Locked;
    public GameMode Mode=GameMode.Title;
    public int QuestStage;
    public float CameraYaw,CameraPitch=-.29f,CameraShake;
    public Camera3D Camera=null!;
    public Hud Hud=null!;
    public string Speaker="",SpeakerRole="",Dialogue="",ToastTitle="",ToastBody="",FeedbackText="";
    public readonly List<(string Speaker,string Text)> Script=new();
    public readonly HashSet<string> Heard=new();
    public int Beat,NightFrom=-1;
    public float Reveal;
    public Color FeedbackColor=Hoshikawa.World.Gold;
    public float ToastTime,FeedbackTime;
    public bool SelfTest;
    private Node3D cameraRig=null!;
    private SpringArm3D spring=null!;
    private readonly Dictionary<string,AudioStreamWav> sounds=new();
    private readonly List<(Node3D Node,float Life,float Total,float Radius)> effects=new();
    private AudioStreamPlayer ambience=null!;
    public AudioStreamPlayer Voice=null!;
    private string? scene;
    private double elapsed;
    private bool captured;
    private string capturePath="";
    private string previousRegion="Hoshikawa";
    public string Region=>Hoshikawa.World.RegionAt(Player.GlobalPosition);
    public override void _Ready()
    {
        InputBindings();
        World=new World();AddChild(World);
        Player=new Player {Game=this,Position=new(0,.12f,31)};AddChild(Player);
        Vector3[] homes={new(-1,0,-27),new(6,0,-40),new(-2,0,-51)};
        for(int i=0;i<homes.Length;i++)
        {
            var p=homes[i];p.Y=Hoshikawa.World.Height(p.X,p.Z)+.1f;
            var enemy=new Enemy {Game=this,Home=p,Position=p,Index=i+1};Enemies.Add(enemy);AddChild(enemy);
        }
        cameraRig=new Node3D {Position=Player.Position+Vector3.Up*1.3f};AddChild(cameraRig);
        spring=new SpringArm3D {SpringLength=7.5f,Margin=.25f,CollisionMask=1|Hoshikawa.World.CameraLayer,Shape=new SphereShape3D {Radius=.2f}};cameraRig.AddChild(spring);
        Camera=new Camera3D {Fov=59,Near=.08f,Far=260,Current=true};spring.AddChild(Camera);
        var layer=new CanvasLayer();AddChild(layer);Hud=new Hud {Game=this};layer.AddChild(Hud);
        foreach(string name in new[]{"swing","impact","guard","deflect","break","skill","hurt","jump","dodge","heal","warning"})
            sounds[name]=GD.Load<AudioStreamWav>("res://assets/audio/"+name+".wav");
        ambience=new AudioStreamPlayer {Stream=GD.Load<AudioStreamWav>("res://assets/audio/valley.wav"),VolumeDb=-16};AddChild(ambience);ambience.Finished+=()=>ambience.Play();ambience.Play();
        Voice=new AudioStreamPlayer();AddChild(Voice);
        var args=OS.GetCmdlineUserArgs();SelfTest=args.Contains("--self-test");
        var capture=args.FirstOrDefault(a=>a.StartsWith("--capture="));if(capture!=null)capturePath=capture[10..];
        if(SelfTest||args.Contains("--play")||(capturePath!=""&&!args.Contains("--title-view")))Start();
        if(SelfTest)CallDeferred(MethodName.RunSelfTest);
        if(args.Contains("--combat-view")){Player.GlobalPosition=new(0,Hoshikawa.World.Height(0,-21)+.1f,-21);CameraYaw=.15f;}
        if(args.Contains("--overview")){Player.GlobalPosition=new(0,2,49);CameraPitch=-.5f;spring.SpringLength=13;CameraYaw=-.18f;}
        if(args.Contains("--portrait")){Player.GlobalPosition=new(0,.1f,23);CameraYaw=Mathf.Pi;CameraPitch=-.13f;spring.SpringLength=3.2f;}
        if(args.Contains("--portrait-close")){Player.GlobalPosition=new(0,.1f,23);CameraYaw=Mathf.Pi+.45f;CameraPitch=-.09f;spring.SpringLength=2.1f;Camera.Fov=37;}
        if(args.Contains("--komorebi")){Player.GlobalPosition=new(0,Hoshikawa.World.Height(0,-135)+.1f,-135);CameraPitch=-.3f;spring.SpringLength=11;}
        if(args.Contains("--pass-view")){Player.GlobalPosition=new(Hoshikawa.World.RoadX(-99),Hoshikawa.World.Height(Hoshikawa.World.RoadX(-99),-99)+.1f,-99);CameraPitch=-.27f;spring.SpringLength=10;}
        Hud.RefreshMenu();GD.Print($"HOSHIKAWA_READY buildings={World.Buildings.Count} residents={World.Residents.Count} patrols={Enemies.Count}");
    }
    // A stopped playback, and the valley.wav it holds, is only released on the audio thread's next mix step; if the engine
    // shuts the AudioServer down first, both leak at exit. Blocking briefly at quit lets that step run first.
    public override void _ExitTree(){ambience.Stop();Voice.Stop();OS.DelayMsec(100);}
    public void Start()
    {
        Mode=GameMode.Playing;Input.MouseMode=SelfTest?Input.MouseModeEnum.Visible:Input.MouseModeEnum.Captured;
        Toast("HOSHIKAWA","An offline valley, beyond the city's reach.",5);Hud.RefreshMenu();
    }
    public void Begin(){Start();Play(Prologue.Intro);}
    public override void _UnhandledInput(InputEvent e)
    {
        if(e is InputEventKey key&&key.Pressed&&!key.Echo)
        {
            if(key.Keycode==Key.F8){SaveScreenshot("res://artifacts/gameplay.png");return;}
            if(key.Keycode==Key.Enter&&Mode==GameMode.Title){Begin();GetViewport().SetInputAsHandled();return;}
            if(Mode==GameMode.Dialogue&&(key.PhysicalKeycode==Key.E||key.Keycode==Key.Enter)){Advance();GetViewport().SetInputAsHandled();return;}
            if(Mode==GameMode.Dialogue&&key.Keycode==Key.Escape){EndDialogue();GetViewport().SetInputAsHandled();return;}
            if(key.Keycode==Key.Escape&&Mode is GameMode.Playing or GameMode.Paused)
            {SetPaused(Mode==GameMode.Playing);return;}
            if(key.PhysicalKeycode==Key.E&&Mode==GameMode.Playing){Interact();return;}
            if(key.PhysicalKeycode==Key.R&&Mode==GameMode.Defeated){Retry();return;}
        }
        if(e is InputEventMouseMotion motion&&Mode==GameMode.Playing&&Input.MouseMode==Input.MouseModeEnum.Captured)
        {
            CameraYaw-=motion.Relative.X*.0026f;CameraPitch=Mathf.Clamp(CameraPitch-motion.Relative.Y*.0023f,-.95f,-.09f);
        }
        // The left button is also "attack": releasing the action stops the click that closes a scene from swinging the sword.
        if(e is InputEventMouseButton click&&click.Pressed&&click.ButtonIndex==MouseButton.Left&&Mode==GameMode.Dialogue)
        {Advance();Input.ActionRelease("attack");GetViewport().SetInputAsHandled();return;}
        if(e is InputEventMouseButton mouse&&mouse.Pressed&&Mode==GameMode.Playing)
        {
            if(mouse.ButtonIndex==MouseButton.WheelUp)spring.SpringLength=Math.Max(3,spring.SpringLength-.6f);
            if(mouse.ButtonIndex==MouseButton.WheelDown)spring.SpringLength=Math.Min(12,spring.SpringLength+.6f);
        }
    }
    public override void _Process(double delta)
    {
        float dt=(float)delta;elapsed+=delta;
        if(Mode==GameMode.Dialogue)Reveal=Math.Min(Dialogue.Length,Reveal+dt*52);
        if(Mode==GameMode.Playing)
        {
            ToastTime=Math.Max(0,ToastTime-dt);FeedbackTime=Math.Max(0,FeedbackTime-dt);
            if(Region!=previousRegion)
            {
                previousRegion=Region;
                Toast(Region.ToUpperInvariant(),Region=="Komorebi"?"Orchards, open doors, and a place to recover.":Region=="Cedar Pass"?"Follow the lanterns to the northern village.":Region=="Hoshikawa"?"The lanterns of home.":"The manual relay lies beneath the viaduct.",3.5f);
            }
            if(Locked!=null&&(Locked.Dead||Locked.GlobalPosition.DistanceTo(Player.GlobalPosition)>22))Locked=null;
            if(Locked!=null)
            {var to=Locked.GlobalPosition-Player.GlobalPosition;CameraYaw=Mathf.LerpAngle(CameraYaw,Mathf.Atan2(-to.X,-to.Z),dt*3.8f);}
        }
        cameraRig.Position=cameraRig.Position.Lerp(Player.GlobalPosition+Vector3.Up*1.35f,1-Mathf.Exp(-dt*10));
        cameraRig.Rotation=new(CameraPitch,CameraYaw,0);
        CameraShake=Math.Max(0,CameraShake-dt);
        Camera.HOffset=CameraShake>0?Mathf.Sin((float)elapsed*100)*CameraShake*.2f:0;
        Camera.VOffset=CameraShake>0?Mathf.Cos((float)elapsed*86)*CameraShake*.15f:0;
        for(int i=effects.Count-1;i>=0;i--)
        {
            var fx=effects[i];float life=fx.Life-dt;
            if(life<=0){fx.Node.QueueFree();effects.RemoveAt(i);continue;}
            float t=1-life/fx.Total;fx.Node.Scale=Vector3.One*(.25f+t*fx.Radius);
            effects[i]=(fx.Node,life,fx.Total,fx.Radius);
        }
        Hud.QueueRedraw();
        if(capturePath!=""&&!captured&&elapsed>6){captured=true;SaveScreenshot(capturePath);}
    }
    public void SetPaused(bool pause)
    {
        Mode=pause?GameMode.Paused:GameMode.Playing;Input.MouseMode=pause?Input.MouseModeEnum.Visible:Input.MouseModeEnum.Captured;
        FreezeAnimations(pause);Hud.RefreshMenu();
    }
    private void FreezeAnimations(bool freeze)
    {
        foreach(var a in ActorVisual.Descendants(this).OfType<AnimationPlayer>())a.SpeedScale=freeze?0:1;
    }
    public void Die()
    {Mode=GameMode.Defeated;Input.MouseMode=Input.MouseModeEnum.Visible;Hud.RefreshMenu();}
    public void Retry()
    {
        Player.Respawn();Locked=null;
        foreach(var e in Enemies)if(!e.Dead)e.Reset();
        CameraYaw=0;FreezeAnimations(false);Start();Toast("BACK AT THE VILLAGE","Aki: breathe. Your work here is not finished.",3);
    }
    public void Restart()
    {
        QuestStage=0;Heard.Clear();foreach(var e in Enemies)e.Reset();Retry();Play(Prologue.Intro);
    }
    public string Objective=>QuestStage switch
    {
        0=>"Speak to Ren at the workshop",
        1=>$"Clear the old relay road   {Enemies.Count(e=>e.Dead)} / 3",
        2=>"Restart the manual relay",
        3=>"Return to Ren in Hoshikawa",
        _=>"Evacuation route secured"
    };
    public Vector3 ObjectivePosition=>QuestStage switch
    {
        0 or 3=>World.Residents[0].Body.GlobalPosition,
        1=>Enemies.FirstOrDefault(e=>!e.Dead)?.GlobalPosition??World.RelayPosition,
        _=>World.RelayPosition
    };
    public string InteractionPrompt()
    {
        foreach(var r in World.Residents)if(r.Body.GlobalPosition.DistanceTo(Player.GlobalPosition)<3.1f)return "E   Talk to "+r.Name;
        if(World.RelayPosition.DistanceTo(Player.GlobalPosition)<3.3f)return "E   Inspect manual relay";
        return "";
    }
    public void Interact()
    {
        foreach(var r in World.Residents)
        {
            if(r.Body.GlobalPosition.DistanceTo(Player.GlobalPosition)>=3.1f)continue;
            if(r.Name=="Ren")
            {
                if(QuestStage==0){QuestStage=Enemies.All(e=>e.Dead)?2:1;Toast("THE TRAIN WITH NO PASSENGERS","Clear the patrols. Reopen the road.",4);Play(Prologue.Briefing);}
                else if(QuestStage==3){QuestStage=4;Toast("ROUTE SECURED","Chapter prologue complete · Hoshikawa is ready.",8);Sound("heal");Play(Prologue.Homecoming);NightFrom=Prologue.NightFalls;}
                else Play(("Ren",QuestStage==4?Prologue.RenRest:QuestStage==2?Prologue.RenRoadClear:Prologue.RenReminder));
                return;
            }
            if(r.Name is "Aki" or "Emi"){Player.Health=100;Player.Posture=0;Player.Remedies=3;Sound("heal");}
            Play(Heard.Add(r.Name)&&Prologue.FirstMeeting.TryGetValue(r.Name,out var scene)?scene:new[]{(r.Name,r.Line)});return;
        }
        if(World.RelayPosition.DistanceTo(Player.GlobalPosition)<3.3f)
        {
            if(QuestStage==0)Play(("Zero",Prologue.RelayAsleep));
            else if(QuestStage==1)Play(("Zero",Prologue.RelayGuarded(Enemies.Count(e=>!e.Dead))));
            else if(QuestStage==2){QuestStage=3;Burst(World.RelayPosition+Vector3.Up*2,Hoshikawa.World.Jade,4);Sound("skill");Play(Prologue.RelayOnline);}
            else Play(("Zero",Prologue.RelaySteady));
        }
    }
    public void Play(params (string Speaker,string Text)[] beats)
    {Script.Clear();Script.AddRange(beats);scene=Prologue.SceneNames.GetValueOrDefault(beats);Beat=-1;NightFrom=-1;Mode=GameMode.Dialogue;Input.MouseMode=Input.MouseModeEnum.Visible;Advance();}
    // The first press finishes a line still typing out; the next one moves on.
    public void Advance()
    {
        if(Beat>=0&&Reveal<Dialogue.Length){Reveal=Dialogue.Length;return;}
        if(++Beat>=Script.Count){EndDialogue();return;}
        (Speaker,Dialogue)=Script[Beat];Reveal=0;
        var line=$"res://assets/audio/voice/ja/{Speaker}/{scene}.{Beat}.mp3";
        Voice.Stop();Voice.Stream=scene!=null&&ResourceLoader.Exists(line)?GD.Load<AudioStream>(line):null;if(Voice.Stream!=null)Voice.Play();
        SpeakerRole=Speaker switch {"Mio"=>"OFFLINE RUNNER","Zero"=>"SENSORY LINK · CITY SIDE",_=>World.Residents.FirstOrDefault(r=>r.Name==Speaker).Role??""};
    }
    public void EndDialogue(){Voice.Stop();Mode=GameMode.Playing;Input.MouseMode=SelfTest?Input.MouseModeEnum.Visible:Input.MouseModeEnum.Captured;}
    public void OnEnemyDefeated()
    {
        if(QuestStage==1&&Enemies.All(e=>e.Dead)){QuestStage=2;Toast("ROAD CLEARED","Follow the lanterns to the manual relay.",5);}
    }
    public void ToggleLock()
    {
        if(Locked!=null){Locked=null;return;}
        Locked=Enemies.Where(e=>!e.Dead&&e.GlobalPosition.DistanceTo(Player.GlobalPosition)<18)
            .OrderBy(e=>e.GlobalPosition.DistanceTo(Player.GlobalPosition)).FirstOrDefault();
    }
    public bool ClearSight(Vector3 a,Vector3 b)
    {
        var query=PhysicsRayQueryParameters3D.Create(a,b,1);
        return GetWorld3D().DirectSpaceState.IntersectRay(query).Count==0;
    }
    public void Toast(string title,string body,float seconds=3){ToastTitle=title;ToastBody=body;ToastTime=seconds;}
    public void Feedback(string text,Color color){FeedbackText=text;FeedbackColor=color;FeedbackTime=1.2f;}
    public void Burst(Vector3 p,Color color,float radius)
    {
        var ring=Hoshikawa.World.Mesh(this,"Resonance ring",new TorusMesh {InnerRadius=.9f,OuterRadius=1, Rings=32,RingSegments=6},p,Hoshikawa.World.Mat(color,1));
        effects.Add((ring,.35f,.35f,radius));
        for(int i=0;i<8;i++)
        {
            var spark=Hoshikawa.World.Box(this,"Spark",p+new Vector3(Mathf.Sin(i)*.3f,.15f,Mathf.Cos(i)*.3f),new(.035f,.14f,.035f),Hoshikawa.World.Mat(color,1));
            spark.Rotation=new(i,.2f*i,i*.5f);
            var tween=CreateTween();tween.TweenProperty(spark,"position",p+new Vector3(Mathf.Sin(i)*radius,Mathf.Sin(i*4)*radius*.4f,Mathf.Cos(i)*radius),.24);tween.TweenCallback(Callable.From(spark.QueueFree));
        }
    }
    public void Slash(Vector3 p,Vector3 forward,bool red)
    {
        var node=new Node3D {Position=p,Rotation=new(0,Mathf.Atan2(forward.X,forward.Z),-.18f)};AddChild(node);
        var st=new SurfaceTool();st.Begin(Godot.Mesh.PrimitiveType.Triangles);
        for(int i=0;i<18;i++)
        {
            float a=-1.1f+i*.12f,b=a+.12f;
            Vector3 V(float angle,float r)=>new(Mathf.Sin(angle)*r,0,Mathf.Cos(angle)*r);
            st.AddVertex(V(a,2.25f));st.AddVertex(V(b,2.25f));st.AddVertex(V(a,1.9f));
            st.AddVertex(V(a,1.9f));st.AddVertex(V(b,2.25f));st.AddVertex(V(b,1.9f));
        }
        var mat=Hoshikawa.World.Mat(red?new("fa8163"):Hoshikawa.World.Jade,1,true);
        Hoshikawa.World.Mesh(node,"Sword arc",st.Commit(),Vector3.Zero,mat);
        var t=CreateTween();t.TweenProperty(node,"scale",Vector3.One*.4f,.16);t.TweenCallback(Callable.From(node.QueueFree));
    }
    public void FloatingNumber(Vector3 p,string text,Color c)
    {
        var label=Hoshikawa.World.Label(this,text,p,c,36);var t=CreateTween();
        t.TweenProperty(label,"position",p+Vector3.Up*.8f,.65);t.TweenCallback(Callable.From(label.QueueFree));
    }
    public void Sound(string name)
    {
        if(SelfTest||!sounds.TryGetValue(name,out var sound))return;
        var player=new AudioStreamPlayer {Stream=sound,VolumeDb=-13,PitchScale=(float)GD.RandRange(.94,1.06)};
        AddChild(player);player.Finished+=player.QueueFree;player.Play();
    }
    public async void SaveScreenshot(string path)
    {
        if(DisplayServer.GetName()=="headless")return;
        await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
        string absolute=ProjectSettings.GlobalizePath(path);System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(absolute)!);
        var error=GetViewport().GetTexture().GetImage().SavePng(absolute);GD.Print($"SCREENSHOT {error} {absolute}");
        GD.Print($"RENDER_METRICS fps={Engine.GetFramesPerSecond()} draw_calls={Performance.GetMonitor(Performance.Monitor.RenderTotalDrawCallsInFrame)} objects={Performance.GetMonitor(Performance.Monitor.RenderTotalObjectsInFrame)}");
    }
    private void InputBindings()
    {
        void KeyAction(string name,Key key)
        {if(!InputMap.HasAction(name))InputMap.AddAction(name);InputMap.ActionAddEvent(name,new InputEventKey {PhysicalKeycode=key});}
        KeyAction("forward",Key.W);KeyAction("back",Key.S);KeyAction("left",Key.A);KeyAction("right",Key.D);
        KeyAction("run",Key.Shift);KeyAction("jump",Key.Space);KeyAction("dodge",Key.Ctrl);KeyAction("dodge",Key.Alt);
        KeyAction("skill",Key.Q);KeyAction("heal",Key.H);KeyAction("lock",Key.Tab);KeyAction("attack",Key.J);KeyAction("guard",Key.K);
        InputMap.ActionAddEvent("attack",new InputEventMouseButton {ButtonIndex=MouseButton.Left});
        InputMap.ActionAddEvent("guard",new InputEventMouseButton {ButtonIndex=MouseButton.Right});
    }
    private async void RunSelfTest()
    {
        try {await IntegrationCheck.Run(this);GD.Print("SELF_TEST_PASS");GetTree().Quit(0);}
        catch(Exception e){GD.PushError("SELF_TEST_FAIL "+e);GetTree().Quit(1);}
    }
}
