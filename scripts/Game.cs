using Godot;
using static Hoshikawa.Localization;
namespace Hoshikawa;

public enum GameMode { Title, Playing, Dialogue, Paused, Defeated, Inventory, Settings }
public partial class Game : Node3D
{
    public World World=null!;
    public Player Player=null!;
    public readonly List<Enemy> Enemies=new();
    // The three the relay mission counts; the scouts are extra.
    public IEnumerable<Enemy> Patrols=>Enemies.Where(e=>!e.Scout);
    public readonly HashSet<string> Opened=new();
    private Tween? opening;
    public Enemy? Locked;
    public GameMode Mode=GameMode.Title;
    public int QuestStage;
    public float CameraYaw,CameraPitch=-.29f,CameraShake,CameraPunch;
    public Camera3D Camera=null!;
    public Hud Hud=null!;
    public string Speaker="",SpeakerRole="",Dialogue="",ToastTitle="",ToastBody="",ToastIcon="",FeedbackText="",FlashIcon="";
    public readonly List<(string Speaker,string Text)> Script=new();
    public readonly HashSet<string> Heard=new();
    public int Beat,NightFrom=-1;
    public float Reveal;
    public Color FeedbackColor=Hoshikawa.World.Gold,FlashColor=Hoshikawa.World.Gold;
    public float ToastTime,FeedbackTime,FlashTime;
    public bool SelfTest;
    private Node3D cameraRig=null!;
    private SpringArm3D spring=null!;
    public readonly Dictionary<string,AudioStreamWav> Sounds=new();
    public Music Music=null!;
    public AudioStreamPlayer Voice=null!;
    private AudioStreamPlayer bark=null!;
    private string? scene;
    private double elapsed;
    private bool captured;
    private string capturePath="";
    private string previousRegion="Hoshikawa";
    private float fov,danger;
    private GameMode itemsFrom;
    public string Region=>Hoshikawa.World.RegionAt(Player.GlobalPosition);
    public override void _Ready()
    {
        Localization.Initialize(OS.GetCmdlineUserArgs());Volume.Initialize(OS.GetCmdlineUserArgs());InputBindings();
        World=new World();AddChild(World);
        Player=new Player {Game=this,Position=new(0,.12f,31)};AddChild(Player);
        foreach(var (homes,scout) in new[]{(Hoshikawa.World.PatrolHomes,false),(Hoshikawa.World.ScoutHomes,true)})for(int i=0;i<homes.Length;i++)
        {
            var p=new Vector3(homes[i].X,Hoshikawa.World.Height(homes[i].X,homes[i].Y)+.1f,homes[i].Y);
            var enemy=new Enemy {Game=this,Home=p,Position=p,Index=i+1,Scout=scout};Enemies.Add(enemy);AddChild(enemy);
        }
        cameraRig=new Node3D {Position=Player.Position+Vector3.Up*1.3f};AddChild(cameraRig);
        spring=new SpringArm3D {SpringLength=7.5f,Margin=.25f,CollisionMask=1|Hoshikawa.World.CameraLayer,Shape=new SphereShape3D {Radius=.2f}};cameraRig.AddChild(spring);
        Camera=new Camera3D {Fov=59,Near=.08f,Far=260,Current=true};spring.AddChild(Camera);
        var layer=new CanvasLayer();AddChild(layer);Hud=new Hud {Game=this};layer.AddChild(Hud);
        foreach(string name in new[]{"swing","impact","guard","deflect","break","skill","slam","stun","hurt","jump","dodge","heal","warning","mio_attack1","mio_attack2","mio_attack3","mio_skill","mio_hurt"})
            Sounds[name]=GD.Load<AudioStreamWav>("res://assets/audio/"+name+".wav");
        Music=new Music();AddChild(Music);
        Voice=new AudioStreamPlayer {Bus="Voice"};AddChild(Voice);bark=new AudioStreamPlayer {VolumeDb=-10,Bus="Voice"};AddChild(bark);
        var args=OS.GetCmdlineUserArgs();SelfTest=args.Contains("--self-test");
        var capture=args.FirstOrDefault(a=>a.StartsWith("--capture="));if(capture!=null)capturePath=capture[10..];
        if(SelfTest||args.Contains("--play")||(capturePath!=""&&!args.Contains("--title-view")))Start();
        if(SelfTest)CallDeferred(MethodName.RunSelfTest);
        if(args.Contains("--combat-view")){Player.GlobalPosition=new(0,Hoshikawa.World.Height(0,-21)+.1f,-21);CameraYaw=.15f;}
        if(args.Contains("--overview")){Player.GlobalPosition=new(0,2,49);CameraPitch=-.5f;spring.SpringLength=13;CameraYaw=-.18f;}
        if(args.Contains("--portrait")){Player.GlobalPosition=new(0,.1f,23);CameraYaw=Mathf.Pi;CameraPitch=-.13f;spring.SpringLength=3.2f;}
        if(args.Contains("--portrait-close")){Player.GlobalPosition=new(0,.1f,23);CameraYaw=Mathf.Pi+.45f;CameraPitch=-.09f;spring.SpringLength=2.1f;Camera.Fov=37;}
        if(args.Contains("--komorebi")){Player.GlobalPosition=new(0,Hoshikawa.World.Height(0,-235)+.1f,-235);CameraPitch=-.3f;spring.SpringLength=11;}
        if(args.Contains("--pass-view")){Player.GlobalPosition=new(Hoshikawa.World.RoadX(-199),Hoshikawa.World.Height(Hoshikawa.World.RoadX(-199),-199)+.1f,-199);CameraPitch=-.27f;spring.SpringLength=10;}
        fov=Camera.Fov;Hud.RefreshMenu();GD.Print($"HOSHIKAWA_READY buildings={World.Buildings.Count} residents={World.Residents.Count} patrols={Patrols.Count()} scouts={Enemies.Count(e=>e.Scout)}");
    }
    // Leaving the tree only pauses a player's playback, and a stopped one, with the wav it holds, is only released on the audio
    // thread's next mix step; if the engine shuts the AudioServer down first, both leak at exit. So every player still sounding,
    // the music, a voice or a long one-shot such as the stun, is stopped here, and blocking briefly lets that step run first.
    public override void _ExitTree(){foreach(var player in FindChildren("*",nameof(AudioStreamPlayer),true,false).Cast<AudioStreamPlayer>())player.Stop();OS.DelayMsec(100);}
    public void Start()
    {
        Mode=GameMode.Playing;Input.MouseMode=SelfTest?Input.MouseModeEnum.Visible:Input.MouseModeEnum.Captured;
        Toast("HOSHIKAWA",seconds:5);Hud.RefreshMenu();
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
            if(Mode==GameMode.Inventory&&(key.Keycode==Key.Escape||key.PhysicalKeycode==Key.I)||Mode==GameMode.Playing&&key.PhysicalKeycode==Key.I)
            {ShowItems(Mode==GameMode.Playing);return;}
            if(key.Keycode==Key.Escape&&Mode==GameMode.Settings){ShowSettings(false);return;}
            if(key.PhysicalKeycode==Key.E&&Mode==GameMode.Inventory){UseItem(Hud.Selected);return;}
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
        if(Mode==GameMode.Dialogue)Reveal=Math.Min(Dialogue.Length,Reveal+dt*RevealSpeed);
        if(Mode==GameMode.Playing)
        {
            ToastTime=Math.Max(0,ToastTime-dt);FeedbackTime=Math.Max(0,FeedbackTime-dt);FlashTime=Math.Max(0,FlashTime-dt);
            if(Region!=previousRegion)
            {
                previousRegion=Region;Toast(Region.ToUpperInvariant(),seconds:3.5f);
            }
            if(Locked!=null&&(Locked.Dead||Locked.GlobalPosition.DistanceTo(Player.GlobalPosition)>22))Locked=null;
            if(Locked!=null)
            {var to=Locked.GlobalPosition-Player.GlobalPosition;CameraYaw=Mathf.LerpAngle(CameraYaw,Mathf.Atan2(-to.X,-to.Z),dt*3.8f);}
        }
        // Any robot out of its patrol, chasing, striking, recovering or held, is a fight; robots only ever engage Mio. A pause, the items
        // screen or a scene freezes the fight and keeps its music; a defeat ends it, and so does Retry() (recovering or restarting).
        if(Mode==GameMode.Playing)danger=Enemies.Any(e=>!e.Dead&&e.State!="PATROL")?Music.Calm:Math.Max(0,danger-dt);else if(Mode==GameMode.Defeated)danger=0;
        Music.Want(Music.Track(Region,danger>0));
        cameraRig.Position=cameraRig.Position.Lerp(Player.GlobalPosition+Vector3.Up*1.35f,1-Mathf.Exp(-dt*10));
        cameraRig.Rotation=new(CameraPitch,CameraYaw,0);
        CameraShake=Math.Max(0,CameraShake-dt);
        Camera.HOffset=CameraShake>0?Mathf.Sin((float)elapsed*100)*CameraShake*.2f:0;
        Camera.VOffset=CameraShake>0?Mathf.Cos((float)elapsed*86)*CameraShake*.15f:0;
        CameraPunch=Math.Max(0,CameraPunch-dt*3);Camera.Fov=fov-CameraPunch*CameraPunch*7;
        TintTime=Math.Max(0,TintTime-dt);
        Hud.QueueRedraw();
        if(capturePath!=""&&!captured&&elapsed>6){captured=true;SaveScreenshot(capturePath);}
    }
    public void SetPaused(bool pause)
    {
        Mode=pause?GameMode.Paused:GameMode.Playing;Input.MouseMode=pause?Input.MouseModeEnum.Visible:Input.MouseModeEnum.Captured;
        FreezeAnimations(pause);Hud.RefreshMenu();
    }
    // The items screen opens over play (I) or from the pause menu, and closes back to whichever it came from; play stays frozen under it.
    public void ShowItems(bool show)
    {
        if(show){itemsFrom=Mode;Mode=GameMode.Inventory;Input.MouseMode=Input.MouseModeEnum.Visible;FreezeAnimations(true);Hud.Select(null);Hud.RefreshMenu();}
        else if(itemsFrom==GameMode.Paused){Mode=GameMode.Paused;Hud.RefreshMenu();}
        else SetPaused(false);
    }
    public bool UseItem(string id){bool used=Player.Use(id);Hud.RefreshMenu();return used;}
    // The settings screen opens from the title or the pause menu and closes back to it; play is already frozen under either.
    public GameMode SettingsFrom{get;private set;}
    public void ShowSettings(bool show){if(show)SettingsFrom=Mode;Mode=show?GameMode.Settings:SettingsFrom;Hud.RefreshMenu();}
    // A voice slider plays one of Mio's shouts, if none is sounding, so the level can be judged without leaving the menu.
    public void SetVolume(string bus,int level){Volume.Set(bus,level,!SelfTest);if(bus=="Voice"&&!bark.Playing)Bark("attack1");}
    private void FreezeAnimations(bool freeze)
    {
        foreach(var a in ActorVisual.Descendants(this).OfType<AnimationPlayer>())a.SpeedScale=freeze?0:1;
    }
    public void Die()
    {Mode=GameMode.Defeated;Input.MouseMode=Input.MouseModeEnum.Visible;Hud.RefreshMenu();}
    public void Retry()
    {
        Player.Respawn();Locked=null;danger=0;
        foreach(var e in Enemies)if(!e.Dead)e.Reset();
        CameraYaw=0;FreezeAnimations(false);Start();Toast("BACK AT THE VILLAGE");
    }
    public void Restart()
    {
        QuestStage=0;Heard.Clear();foreach(var e in Enemies)e.Reset();
        opening?.Kill();Opened.Clear();foreach(var s in World.Shortcuts)s.Swing(s.Closed);
        foreach(var pickup in World.Pickups)pickup.Node.Visible=true;Player.Inventory.Reset();
        Retry();Play(Prologue.Intro);
    }
    // English is what quest logic and the route tests read; the HUD shows the translated one, and counts the patrols in crests.
    public string Objective=>QuestStage switch
    {
        0=>"Speak to Ren at the workshop",
        1=>"Clear the old relay road",
        2=>"Restart the manual relay",
        3=>"Return to Ren in Hoshikawa",
        _=>"Evacuation route secured"
    };
    public string ObjectiveText=>T(Objective);
    public Vector3 ObjectivePosition=>QuestStage switch
    {
        0 or 3=>World.Residents[0].Body.GlobalPosition,
        1=>Patrols.FirstOrDefault(e=>!e.Dead)?.GlobalPosition??World.RelayPosition,
        _=>World.RelayPosition
    };
    // What E would do here, as the HUD's prompt shows it: an icon under assets/art/ui/ and what it acts on.
    public (string Icon,string Label) Interaction()
    {
        foreach(var r in World.Residents)if(r.Body.GlobalPosition.DistanceTo(Player.GlobalPosition)<3.1f)return ("talk",T(r.Name).ToUpperInvariant());
        if(World.RelayPosition.DistanceTo(Player.GlobalPosition)<3.3f)return ("repair",T("MANUAL RELAY"));
        if(ShortcutHere()!=null)return ("ladder",T("LADDER"));
        if(PickupHere() is {} pickup){var item=Items.Get(pickup.Item);return (item.Icon,T(item.Name).ToUpperInvariant());}
        return ("","");
    }
    public void Interact()
    {
        foreach(var r in World.Residents)
        {
            if(r.Body.GlobalPosition.DistanceTo(Player.GlobalPosition)>=3.1f)continue;
            if(r.Name=="Ren")
            {
                if(QuestStage==0){QuestStage=Patrols.All(e=>e.Dead)?2:1;Toast("THE TRAIN WITH NO PASSENGERS",seconds:4);Play(Prologue.Briefing);}
                else if(QuestStage==3){QuestStage=4;Toast("ROUTE SECURED","Chapter prologue complete · Hoshikawa is ready.",8);Sound("heal");Play(Prologue.Homecoming);NightFrom=Prologue.NightFalls;}
                else Play(QuestStage==4?Prologue.RenRest:QuestStage==2?Prologue.RenRoadClear:Prologue.RenReminder);
                return;
            }
            if(r.Name is "Aki" or "Emi"){Player.Rest();Sound("heal");}
            Play(Heard.Add(r.Name)&&Prologue.FirstMeeting.TryGetValue(r.Name,out var scene)?scene:Prologue.Usual(r.Name));return;
        }
        if(World.RelayPosition.DistanceTo(Player.GlobalPosition)<3.3f)
        {
            if(QuestStage==0)Play(Prologue.RelayAsleep);
            else if(QuestStage==1)Play(Prologue.RelayGuardedFor(Patrols.Count(e=>!e.Dead)));
            else if(QuestStage==2){QuestStage=3;Burst(World.RelayPosition+Vector3.Up*2,Hoshikawa.World.Jade,4);Sound("skill");Play(Prologue.RelayOnline);}
            else Play(Prologue.RelaySteady);
            return;
        }
        if(ShortcutHere() is {} shortcut)
        {
            Opened.Add(shortcut.Name);opening=CreateTween();opening.TweenMethod(Callable.From<float>(shortcut.Swing),shortcut.Closed,shortcut.Open,1.8f).SetTrans(Tween.TransitionType.Bounce).SetEase(Tween.EaseType.Out);
            Sound("guard");Toast("SHORTCUT OPENED",seconds:4);return;
        }
        // A pickup that raises what Mio carries, the remedy pouch, refills what it raises.
        if(PickupHere() is {} pickup&&Player.Inventory.Add(pickup.Item))
        {
            var item=Items.Get(pickup.Item);pickup.Node.Visible=false;if(item.Raises!=null)Player.Inventory.Fill(item.Raises);
            Sound("heal");Toast(item.Name,seconds:4,icon:item.Icon);
        }
    }
    private World.Shortcut? ShortcutHere()=>World.Shortcuts.FirstOrDefault(s=>!Opened.Contains(s.Name)&&s.From.DistanceTo(Player.GlobalPosition)<2.2f);
    private World.Pickup? PickupHere()=>World.Pickups.FirstOrDefault(p=>p.Node.Visible&&p.Node.GlobalPosition.DistanceTo(Player.GlobalPosition)<2.2f);
    public void Play(params (string Speaker,string Text)[] beats)
    {Script.Clear();Script.AddRange(beats);scene=Prologue.SceneNames.GetValueOrDefault(beats);Beat=-1;NightFrom=-1;Mode=GameMode.Dialogue;Input.MouseMode=Input.MouseModeEnum.Visible;Advance();}
    // The first press finishes a line still typing out; the next one moves on.
    public void Advance()
    {
        if(Beat>=0&&Reveal<Dialogue.Length){Reveal=Dialogue.Length;return;}
        if(++Beat>=Script.Count){EndDialogue();return;}
        Speaker=Script[Beat].Speaker;Dialogue=Line(scene,Beat,Script[Beat].Text);Reveal=0;
        var line=$"res://assets/audio/voice/ja/{Speaker}/{scene}.{Beat}.mp3";
        Voice.Stop();Voice.Stream=scene!=null&&ResourceLoader.Exists(line)?GD.Load<AudioStream>(line):null;if(Voice.Stream!=null)Voice.Play();
        SpeakerRole=Speaker switch {"Mio"=>"OFFLINE RUNNER","Zero"=>"SENSORY LINK · CITY SIDE",_=>World.Residents.FirstOrDefault(r=>r.Name==Speaker).Role??""};
    }
    public void EndDialogue(){Voice.Stop();Mode=GameMode.Playing;Input.MouseMode=SelfTest?Input.MouseModeEnum.Visible:Input.MouseModeEnum.Captured;}
    public void OnEnemyDefeated()
    {
        if(QuestStage==1&&Patrols.All(e=>e.Dead)){QuestStage=2;Toast("ROAD CLEARED",seconds:5);}
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
    public void SetLanguage(string code){Localization.Set(code);if(!SelfTest)Localization.Save();Hud.ApplyLanguage();}
    public void Toast(string title,string body="",float seconds=3,string icon=""){ToastTitle=title;ToastBody=body;ToastTime=seconds;ToastIcon=icon;}
    public void Feedback(string text,Color color){FeedbackText=text;FeedbackColor=color;FeedbackTime=1.2f;}
    // A combat beat shown as an icon (assets/art/ui/) flashing in the middle of the screen, not as words.
    public void Flash(string icon,Color color){FlashIcon=icon;FlashColor=color;FlashTime=FlashLength;}
    public const float FlashLength=.7f;
    public void Sound(string name)
    {
        if(SelfTest||!Sounds.TryGetValue(name,out var sound))return;
        var player=new AudioStreamPlayer {Stream=sound,VolumeDb=-13,PitchScale=(float)GD.RandRange(.94,1.06)};
        AddChild(player);player.Finished+=player.QueueFree;player.Play();
    }
    // Mio's shouts share one player, so a chained swing cuts her last shout off instead of talking over it.
    public void Bark(string name)
    {
        if(SelfTest||!Sounds.TryGetValue("mio_"+name,out var sound))return;
        bark.Stream=sound;bark.Play();
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
