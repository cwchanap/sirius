using Godot;
using static Hoshikawa.Localization;
namespace Hoshikawa;

public enum GameMode { Title, Playing, Dialogue, Paused, Defeated, Inventory, Settings, Rest, History }
// The prologue's quest stages in order, which Game.QuestStage holds. The first day: Renji's briefing, the relay road's patrols, the relay
// and back to Renji. The second: his news in the morning, four errands to get ready, the telephone to him at evening and home to bed.
// The third: the rockfall east of the relay plaza, and the station road beyond it.
public static class Quest {public const int Renji=0,Road=1,Relay=2,Return=3,Morning=4,Ready=5,Handover=6,Home=7,Rockfall=8,Beyond=9;}
public partial class Game : Node3D
{
    public World World=null!;
    public Player Player=null!;
    public readonly List<Enemy> Enemies=new();
    // The three the relay mission counts; the scouts and drones are extra.
    public IEnumerable<Enemy> Patrols=>Enemies.Where(e=>e.Kind==Enemy.Patrol);
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
    // Every line a scene has shown since the prologue began, oldest first, kept in English and translated as the history draws it, with its voice file if it has one.
    public readonly List<(string? Scene,int Beat,string Speaker,string Text,string? Voice)> Log=new();
    public const int LogLength=300;
    // How many lines the history is scrolled back from the newest.
    public int LogScroll;
    // Held, CTRL fast-forwards a scene: each line is shown whole, silent, for FastBeat seconds before the next.
    public const float FastBeat=.08f;
    public bool Fast=>Mode==GameMode.Dialogue&&Input.IsActionPressed("skip");
    private float fastClock;
    private bool hushed;
    public int Beat,NightFrom=-1;
    public float Reveal;
    public Color FeedbackColor=Hoshikawa.World.Gold,FlashColor=Hoshikawa.World.Gold;
    public float ToastTime,FeedbackTime,FlashTime,GainTime;
    // The stardust last earned, shown rising over the HUD's count.
    public int Gained;
    private readonly List<Node3D> savePointGlows=new();
    private bool arrived;
    public bool SelfTest;
    private Node3D cameraRig=null!;
    private SpringArm3D spring=null!;
    public readonly Dictionary<string,AudioStreamWav> Sounds=new();
    public Music Music=null!;
    public AudioStreamPlayer Voice=null!;
    private AudioStreamPlayer bark=null!,replay=null!;
    private int replayed=-1;
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
        foreach(var (homes,kind) in new[]{(Hoshikawa.World.PatrolHomes,Enemy.Patrol),(Hoshikawa.World.ScoutHomes,Enemy.Scout),(Hoshikawa.World.DroneHomes,Enemy.Drone)})for(int i=0;i<homes.Length;i++)
        {
            var p=new Vector3(homes[i].X,Hoshikawa.World.Height(homes[i].X,homes[i].Y)+.1f,homes[i].Y);
            var enemy=new Enemy {Game=this,Home=p,Position=p,Index=i+1,Kind=kind};Enemies.Add(enemy);AddChild(enemy);
        }
        // Outside World, whose static meshes are baked: each save point's light over its well.
        foreach(var (village,well) in Hoshikawa.World.SavePoints)
        {
            var mouth=new Vector3(well.X,Hoshikawa.World.Height(well.X,well.Y)+1.15f,well.Y);
            var glow=Glow(this,"Save point glow "+village,new QuadMesh(),mouth,Fx(Hoshikawa.World.Jade,1.6f,Soft,true));glow.Scale=Vector3.One*1.6f;savePointGlows.Add(glow);
            AddChild(new OmniLight3D {Name="Save point light "+village,Position=mouth,LightColor=Hoshikawa.World.Jade,LightEnergy=1.4f,OmniRange=5});
        }
        cameraRig=new Node3D {Position=Player.Position+Vector3.Up*1.3f};AddChild(cameraRig);
        spring=new SpringArm3D {SpringLength=7.5f,Margin=.25f,CollisionMask=1|Hoshikawa.World.CameraLayer,Shape=new SphereShape3D {Radius=.2f}};cameraRig.AddChild(spring);
        Camera=new Camera3D {Fov=59,Near=.08f,Far=260,Current=true};spring.AddChild(Camera);
        var layer=new CanvasLayer();AddChild(layer);Hud=new Hud {Game=this};layer.AddChild(Hud);
        foreach(string name in new[]{"swing","impact","guard","deflect","break","skill","slam","stun","hurt","jump","dodge","heal","warning","mio_attack1","mio_attack2","mio_attack3","mio_skill","mio_hurt"})
            Sounds[name]=GD.Load<AudioStreamWav>("res://assets/audio/"+name+".wav");
        Music=new Music();AddChild(Music);
        Voice=new AudioStreamPlayer {Bus="Voice"};AddChild(Voice);bark=new AudioStreamPlayer {VolumeDb=-10,Bus="Voice"};AddChild(bark);replay=new AudioStreamPlayer {Bus="Voice"};AddChild(replay);
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
        Stage();fov=Camera.Fov;Hud.RefreshMenu();GD.Print($"HOSHIKAWA_READY buildings={World.Buildings.Count} residents={World.Residents.Count} patrols={Patrols.Count()} scouts={Enemies.Count(e=>e.Kind==Enemy.Scout)} drones={Enemies.Count(e=>e.Kind==Enemy.Drone)}");
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
    public void Begin(){Start();Opening();}
    public override void _UnhandledInput(InputEvent e)
    {
        // The history takes every key and the mouse while it is open: up and down step through it, E or Enter replays the voice of the line
        // at the bottom, a click the voice of the line clicked, and L or Esc closes it to the scene.
        if(Mode==GameMode.History)
        {
            if(e is InputEventKey {Pressed:true} k)
            {
                if(!k.Echo&&(k.PhysicalKeycode==Key.L||k.Keycode==Key.Escape))ShowHistory(false);
                else if(!k.Echo&&(k.PhysicalKeycode==Key.E||k.Keycode==Key.Enter))Replay(Log.Count-1-LogScroll);
                else if(k.Keycode==Key.Up||k.PhysicalKeycode==Key.W)ScrollLog(1);else if(k.Keycode==Key.Down||k.PhysicalKeycode==Key.S)ScrollLog(-1);
            }
            if(e is InputEventMouseButton {Pressed:true} m)
            {if(m.ButtonIndex==MouseButton.Left&&Hud.LineAt(m.Position) is int line)Replay(line);else ScrollLog(m.ButtonIndex==MouseButton.WheelUp?1:m.ButtonIndex==MouseButton.WheelDown?-1:0);}
            GetViewport().SetInputAsHandled();return;
        }
        if(Mode==GameMode.Dialogue&&(e is InputEventKey {Pressed:true,Echo:false,PhysicalKeycode:Key.L}||e is InputEventMouseButton {Pressed:true,ButtonIndex:MouseButton.WheelUp}))
        {ShowHistory(true);GetViewport().SetInputAsHandled();return;}
        if(e is InputEventKey key&&key.Pressed&&!key.Echo)
        {
            if(key.Keycode==Key.F8){SaveScreenshot("res://artifacts/gameplay.png");return;}
            if(key.Keycode==Key.Enter&&Mode==GameMode.Title){Begin();GetViewport().SetInputAsHandled();return;}
            if(Mode==GameMode.Dialogue&&(key.PhysicalKeycode==Key.E||key.Keycode==Key.Enter)){Advance();GetViewport().SetInputAsHandled();return;}
            if(Mode==GameMode.Dialogue&&key.Keycode==Key.Escape){EndDialogue();GetViewport().SetInputAsHandled();return;}
            if(Mode==GameMode.Rest&&key.Keycode==Key.Escape){LeaveRest();return;}
            if(key.Keycode==Key.Escape&&Mode is GameMode.Playing or GameMode.Paused)
            {SetPaused(Mode==GameMode.Playing);return;}
            if(Mode==GameMode.Inventory&&(key.Keycode==Key.Escape||key.PhysicalKeycode==Key.I)||Mode==GameMode.Playing&&key.PhysicalKeycode==Key.I)
            {ShowItems(Mode==GameMode.Playing);return;}
            if(key.Keycode==Key.Escape&&Mode==GameMode.Settings){ShowSettings(false);return;}
            if(key.PhysicalKeycode==Key.E&&Mode==GameMode.Inventory){UseItem(Hud.Selected);return;}
            if(key.PhysicalKeycode==Key.E&&Mode==GameMode.Playing){if(Player.Free)Interact();return;}
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
        if(Mode==GameMode.Dialogue)
        {
            // The first held frame moves on at once, cutting the voice; Advance() may end the scene, or start the next. Let go, the line it stopped on speaks.
            if(Fast){if((fastClock-=dt)<=0){fastClock=FastBeat;Reveal=Dialogue.Length;Advance();}Reveal=Dialogue.Length;}else{fastClock=0;if(hushed){hushed=false;Voice.Play();}}
            Reveal=Math.Min(Dialogue.Length,Reveal+dt*RevealSpeed);
        }
        if(Mode==GameMode.Playing)
        {
            ToastTime=Math.Max(0,ToastTime-dt);FeedbackTime=Math.Max(0,FeedbackTime-dt);FlashTime=Math.Max(0,FlashTime-dt);GainTime=Math.Max(0,GainTime-dt);
            if(Region!=previousRegion)
            {
                previousRegion=Region;Toast(Region.ToUpperInvariant(),seconds:3.5f);
            }
            if(Locked!=null&&(Locked.Dead||Locked.GlobalPosition.DistanceTo(Player.GlobalPosition)>22))Locked=null;
            // The station road ends where the relay road's extension, the next map, will begin.
            if(QuestStage==Quest.Beyond&&!arrived&&Player.GlobalPosition.DistanceTo(World.StationRoadEnd)<4){arrived=true;Toast("TO BE CONTINUED",seconds:6);}
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
        foreach(var glow in savePointGlows)glow.Scale=Vector3.One*(1.6f+.15f*Mathf.Sin((float)elapsed*1.7f));
        TintTime=Math.Max(0,TintTime-dt);Staging(Mode==GameMode.History?0:dt);
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
    public void Die(){Mode=GameMode.Defeated;Input.MouseMode=Input.MouseModeEnum.Visible;Hud.RefreshMenu();}
    public void Retry()
    {
        ExitRoom();Player.Visible=true;Player.Respawn();Locked=null;danger=0;Regroup();
        CameraYaw=0;FreezeAnimations(false);Start();Toast("BACK AT THE VILLAGE");
    }
    public void Restart()
    {
        QuestStage=Quest.Renji;Heard.Clear();Log.Clear();arrived=false;foreach(var e in Enemies)e.Reset();
        opening?.Kill();Opened.Clear();foreach(var s in World.Shortcuts)s.Swing(s.Closed);World.ClearRockfall(false);
        foreach(var pickup in World.Pickups)pickup.Node.Visible=true;Player.Inventory.Reset();
        Player.Growth.Reset();
        Retry();PlaceCast();Opening();
    }
    // Recovering at the village or resting at the save point sends every robot home and brings back the scouts and drones Mio shut down;
    // the patrols stay down, because the mission counts them.
    private void Regroup(){foreach(var e in Enemies)if(!e.Dead||e.Kind!=Enemy.Patrol)e.Reset();}
    public bool AtSavePoint=>Hoshikawa.World.SavePoints.Any(p=>new Vector2(Player.GlobalPosition.X,Player.GlobalPosition.Z).DistanceTo(p.At)<2.4f);
    // The save point rests Mio and opens its screen, where she levels up and trades; play stays frozen under it.
    public void Rest()
    {
        Player.Rest();Regroup();Sound("heal");
        Mode=GameMode.Rest;Input.MouseMode=Input.MouseModeEnum.Visible;FreezeAnimations(true);Hud.RefreshMenu();
    }
    public void LeaveRest()=>SetPaused(false);
    // A new point fills the stat it raises, as resting would.
    public bool Raise(string id){bool raised=Player.Growth.Raise(id);if(raised){Player.Rest();Sound("heal");}Hud.RefreshMenu();return raised;}
    public bool Buy(string id){bool bought=Player.Growth.Buy(Player.Inventory,id);if(bought)Sound("heal");Hud.RefreshMenu();return bought;}
    public void Earn(int amount){Player.Growth.Stardust+=amount;Gained=amount;GainTime=2.5f;}
    // The second day's four errands, in any order, each a scene and who starts it: Sota joining, Yui's crossbow at the clinic (with Chiaki),
    // KOMA-7 asking its keeper, and Renji's telephone mended in Mio's room.
    public static readonly (string Scene,string By)[] Errands={("SotaJoins","Sota"),("Crossbow","Yui"),("KomaAsks","KOMA-7"),("PhoneFixed","Telephone")};
    public int ErrandsDone=>Errands.Count(e=>Heard.Contains(e.Scene));
    public bool MendPhone=>QuestStage==Quest.Ready&&!Heard.Contains("PhoneFixed");
    // English is what quest logic and the route tests read; the HUD shows the translated one, and counts the patrols in crests.
    public string Objective=>InRoom?MendPhone?"Mend Renji's telephone":"Head outside":QuestStage switch
    {
        Quest.Renji or Quest.Morning=>"Speak to Renji at the workshop",
        Quest.Road=>"Clear the old relay road",
        Quest.Relay=>"Restart the manual relay",
        Quest.Return=>"Return to Renji in Hoshikawa",
        Quest.Ready=>"Get ready to set out ({0}/4)",
        Quest.Handover=>"Bring Renji his telephone",
        Quest.Home=>"Go home and rest",
        Quest.Rockfall=>"Meet the others at the rockfall",
        _=>"Take the station road east"
    };
    public string ObjectiveText=>F(Objective,ErrandsDone);
    public Vector3 ObjectivePosition=>InRoom?Room!.GlobalPosition+(MendPhone?Hoshikawa.World.Telephone:Hoshikawa.World.Door):QuestStage switch
    {
        Quest.Road=>Patrols.FirstOrDefault(e=>!e.Dead)?.GlobalPosition??World.RelayPosition,
        Quest.Relay=>World.RelayPosition,
        Quest.Ready=>Errands.Where(e=>!Heard.Contains(e.Scene)).Select(e=>e.By=="Telephone"?Doorstep:Actor(e.By).Body.GlobalPosition).MinBy(p=>p.DistanceTo(Player.GlobalPosition)),
        Quest.Home=>Doorstep,
        Quest.Beyond=>World.StationRoadEnd,
        _=>Actor("Renji").Body.GlobalPosition
    };
    private static Vector3 Doorstep=>new(Hoshikawa.World.MioDoor.X,Hoshikawa.World.Height(Hoshikawa.World.MioDoor.X,Hoshikawa.World.MioDoor.Z),Hoshikawa.World.MioDoor.Z);
    // From the second day Mio can go home through her own door, to mend the telephone and to sleep.
    public bool AtHome=>!InRoom&&QuestStage>=Quest.Ready&&Player.GlobalPosition.DistanceTo(Doorstep)<1.8f;
    // What E would do here, as the HUD's prompt shows it: an icon under assets/art/ui/ and what it acts on.
    public (string Icon,string Label) Interaction()
    {
        if(InRoom)return AtDoor?("",T("DOOR")):AtPhoto?("",T("PHOTO")):AtPhone?("repair",T("TELEPHONE")):("","");
        if(Nearest() is {} r)return ("talk",T(r.Name).ToUpperInvariant());
        if(World.RelayPosition.DistanceTo(Player.GlobalPosition)<3.3f)return ("repair",T("MANUAL RELAY"));
        if(AtSavePoint)return (Hud.StardustIcon,T(Hoshikawa.World.SavePointName).ToUpperInvariant());
        if(AtHome)return ("",T("DOOR"));
        if(ShortcutHere()!=null)return ("ladder",T("LADDER"));
        if(PickupHere() is {} pickup){var item=Items.Get(pickup.Item);return (item.Icon,T(item.Name).ToUpperInvariant());}
        return ("","");
    }
    public void Interact()
    {
        if(InRoom){if(AtDoor)LeaveRoom();else if(AtPhoto)Play(Prologue.Photo);else if(AtPhone)Errand("PhoneFixed");return;}
        if(Nearest() is {} r){Talk(r.Name);return;}
        if(World.RelayPosition.DistanceTo(Player.GlobalPosition)<3.3f)
        {
            if(QuestStage==Quest.Renji)Play(Prologue.RelayAsleep);
            else if(QuestStage==Quest.Road)Play(Prologue.RelayGuardedFor(Patrols.Count(e=>!e.Dead)));
            else if(QuestStage==Quest.Relay){QuestStage=Quest.Return;Burst(World.RelayPosition+Vector3.Up*2,Hoshikawa.World.Jade,4);Sound("skill");Play(Prologue.RelayOnline);}
            else Play(Prologue.RelaySteady);
            return;
        }
        if(AtSavePoint){Rest();return;}
        // Home on the second night, she talks with Zero and sleeps, and wakes on the third morning.
        if(AtHome){EnterRoom(false);if(QuestStage==Quest.Home){QuestStage=Quest.Rockfall;Play(Prologue.NightTwo);}return;}
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
    // What a resident says depends on the quest stage; quest stages change here, before the scene plays.
    private void Talk(string name)
    {
        if(name is "Chiaki" or "Emiko"){Player.Rest();Sound("heal");}
        if(QuestStage==Quest.Rockfall&&name is "Renji" or "Yui" or "Sota" or "KOMA-7"){QuestStage=Quest.Beyond;Play(Prologue.Rockfall);return;}
        if(name=="Renji")
        {
            // Yui brings the news with Mio, so she is there even if Mio skipped meeting her at the gate.
            if(QuestStage==Quest.Renji){Heard.Add("Gate");PlaceCast();QuestStage=Patrols.All(e=>e.Dead)?Quest.Relay:Quest.Road;Toast("THE TRAIN WITH NO PASSENGERS",seconds:4);Play(Prologue.Briefing);}
            else if(QuestStage==Quest.Return){QuestStage=Quest.Morning;Sound("heal");Play(Prologue.Homecoming);}
            else if(QuestStage==Quest.Morning){QuestStage=Quest.Ready;Play(Prologue.HalfDone);}
            else if(QuestStage==Quest.Handover){QuestStage=Quest.Home;Play(Prologue.Evening);}
            else Play(QuestStage switch {Quest.Road=>Prologue.RenjiReminder,Quest.Relay=>Prologue.RenjiRoadClear,Quest.Ready=>Prologue.RenjiReady,Quest.Home=>Prologue.RenjiNight,_=>Prologue.RenjiSeesOff});
            return;
        }
        if(name=="Yui"&&!Heard.Contains("Gate")){Play(Prologue.Gate);return;}
        if(name=="Yui"&&QuestStage==Quest.Renji){Play(Prologue.YuiWaits);return;}
        if(QuestStage==Quest.Ready&&(name is "Yui" or "Chiaki"?"Crossbow":Errands.FirstOrDefault(e=>e.By==name).Scene) is {} errand&&!Heard.Contains(errand))
        {
            // Sota's recruiting takes his first meeting's place; Chiaki sends Mio off with her remedies.
            if(errand=="SotaJoins")Heard.Add("Sota");if(errand=="Crossbow")Player.Inventory.Fill("remedy");
            Errand(errand);return;
        }
        if(name=="Chiyo"&&QuestStage is Quest.Ready or Quest.Handover&&!Heard.Contains("ChiyoLamp")){Play(Prologue.ChiyoLamp);return;}
        if(name=="Sota"&&QuestStage>=Quest.Morning)Heard.Add("Sota");
        Play(Heard.Add(name)&&Prologue.FirstMeeting.TryGetValue(name,out var scene)?scene:Prologue.Usual(name));
    }
    // One of the second day's errands; the last of the four sends Mio back to Renji.
    private void Errand(string scene)
    {
        Play(Prologue.Named(scene));
        if(QuestStage==Quest.Ready&&ErrandsDone==Errands.Length){QuestStage=Quest.Handover;Toast("READY TO SET OUT",seconds:4);}
    }
    private (string Name,string Role,Node3D Body)? Nearest()
    {
        var r=World.Residents.MinBy(r=>r.Body.GlobalPosition.DistanceTo(Player.GlobalPosition));
        return r.Body.GlobalPosition.DistanceTo(Player.GlobalPosition)<3.1f?r:null;
    }
    private World.Shortcut? ShortcutHere()=>World.Shortcuts.FirstOrDefault(s=>!Opened.Contains(s.Name)&&s.From.DistanceTo(Player.GlobalPosition)<2.2f);
    private World.Pickup? PickupHere()=>World.Pickups.FirstOrDefault(p=>p.Node.Visible&&p.Node.GlobalPosition.DistanceTo(Player.GlobalPosition)<2.2f);
    public void Play(params (string Speaker,string Text)[] beats)
    {
        Release();Script.Clear();Script.AddRange(beats);scene=Prologue.SceneNames.GetValueOrDefault(beats);if(scene!=null)Heard.Add(scene);
        Beat=-1;NightFrom=Prologue.Nightfall.GetValueOrDefault(scene??"",-1);Mode=GameMode.Dialogue;Input.MouseMode=Input.MouseModeEnum.Visible;Gather();Advance();
    }
    // The first press finishes a line still typing out; the next one moves on.
    public void Advance()
    {
        if(Beat>=0&&Reveal<Dialogue.Length){Reveal=Dialogue.Length;return;}
        if(++Beat>=Script.Count){EndDialogue();return;}
        Speaker=Script[Beat].Speaker;Dialogue=Line(scene,Beat,Script[Beat].Text);Reveal=OnCard?Dialogue.Length:0;
        var line=$"res://assets/audio/voice/ja/{Speaker}/{scene}.{Beat}.mp3";var voice=scene!=null&&ResourceLoader.Exists(line)?line:null;
        Log.Add((scene,Beat,Speaker,Script[Beat].Text,voice));if(Log.Count>LogLength)Log.RemoveAt(0);
        Voice.Stop();Voice.Stream=voice!=null?GD.Load<AudioStream>(voice):null;hushed=Voice.Stream!=null&&Fast;if(Voice.Stream!=null&&!Fast)Voice.Play();
        Cue($"{scene}.{Beat}");
        SpeakerRole=Speaker switch {"Mio"=>"OFFLINE RUNNER","Zero"=>"SENSORY LINK · CITY SIDE",_=>World.Residents.FirstOrDefault(r=>r.Name==Speaker).Role??""};
    }
    // A day's title card, drawn over black; the black lifts as the scene's next line starts.
    public bool OnCard=>Mode is GameMode.Dialogue or GameMode.History&&Prologue.Cards.Contains($"{scene}.{Beat}");
    // The history opens over a scene, which holds still under it (its camera, clips, the cover and its voice), until it closes back to the same line; a voice replayed there stops.
    public void ShowHistory(bool show)
    {
        Mode=show?GameMode.History:GameMode.Dialogue;LogScroll=0;Voice.StreamPaused=show;FreezeAnimations(show);replay.Stop();
        if(tuck?.IsValid()==true){if(show)tuck.Pause();else tuck.Play();}
    }
    // A logged line's voice plays again on a player of its own, so the paused line on screen resumes where it was when the history closes.
    public bool Replay(int line)
    {
        if(line<0||line>=Log.Count||Log[line].Voice is not {} voice)return false;
        replay.Stream=GD.Load<AudioStream>(voice);replay.Play();replayed=line;return true;
    }
    // The logged line whose voice is replaying, or -1.
    public int Replaying=>replay.Playing?replayed:-1;
    public void ScrollLog(int lines)=>LogScroll=Math.Clamp(LogScroll+lines,0,Math.Max(0,Log.Count-1));
    public void EndDialogue(){Voice.Stop();Mode=GameMode.Playing;Input.MouseMode=SelfTest?Input.MouseModeEnum.Visible:Input.MouseModeEnum.Captured;Release(true);After(scene);}
    public void OnEnemyDefeated(Enemy enemy)
    {
        Earn(enemy.Kind.Reward);
        if(QuestStage==Quest.Road&&Patrols.All(e=>e.Dead)){QuestStage=Quest.Relay;Toast("ROAD CLEARED",seconds:5);}
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
        KeyAction("skill",Key.Q);KeyAction("heal",Key.H);KeyAction("lock",Key.Tab);KeyAction("attack",Key.J);KeyAction("guard",Key.K);KeyAction("skip",Key.Ctrl);
        InputMap.ActionAddEvent("attack",new InputEventMouseButton {ButtonIndex=MouseButton.Left});
        InputMap.ActionAddEvent("guard",new InputEventMouseButton {ButtonIndex=MouseButton.Right});
    }
    private async void RunSelfTest()
    {
        try {await IntegrationCheck.Run(this);GD.Print("SELF_TEST_PASS");GetTree().Quit(0);}
        catch(Exception e){GD.PushError("SELF_TEST_FAIL "+e);GetTree().Quit(1);}
    }
}
