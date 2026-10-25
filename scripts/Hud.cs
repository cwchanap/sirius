using Godot;
using static Hoshikawa.Localization;
namespace Hoshikawa;

public partial class Hud : Control
{
    public Game Game=null!;
    private readonly Font font=ThemeDB.FallbackFont;
    private readonly SystemFont serif=new() {FontNames=new[]{"Georgia","Noto Serif","DejaVu Serif"}};
    private Texture2D art=null!,logo=null!;
    private GradientTexture2D titleShade=null!,sceneShade=null!,questShade=null!;
    private readonly Dictionary<string,Texture2D?> portraits=new();
    // White on clear, tinted where they are drawn (assets/art/ui/ui-art-generation.md).
    public static readonly string[] Icons={"zero","resonance","remedy","talk","repair","ladder","perilous","finish","stunned","shutdown","deflect","posture_break","sirius"};
    private static readonly Dictionary<string,Texture2D> icons=new();
    public static Texture2D Icon(string name)=>icons.TryGetValue(name,out var t)?t:icons[name]=GD.Load<Texture2D>($"res://assets/art/ui/{name}.png");
    private Control menu=null!;
    private Button? use;
    // The settings screen's volume sliders are bare HSliders for focus, keys and the mouse; _Draw paints them, named for their bus.
    private readonly List<HSlider> sliders=new();
    private readonly Texture2D bare=ImageTexture.CreateFromImage(Image.CreateEmpty(1,1,false,Image.Format.Rgba8));
    // The item the items screen shows, the one whose slot last took focus.
    public string Selected{get;private set;}="";
    private float scale=1,w=1440,h=900;
    public int Redraws;
    private Color ink=new("12282f"),cream=new("f1ead5"),muted=new("aec0b4"),gold=World.Gold,jade=World.Jade,red=new("f18470"),crest=new("e58b66");
    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);MouseFilter=MouseFilterEnum.Ignore;TextureFilter=TextureFilterEnum.LinearWithMipmaps;
        art=GD.Load<Texture2D>("res://assets/art/mio-keyart.png");logo=GD.Load<Texture2D>("res://assets/art/ui/logo.png");
        titleShade=new GradientTexture2D {Width=512,Height=1,FillFrom=Vector2.Zero,FillTo=Vector2.Right,
            Gradient=new Gradient {Colors=new[]{new Color(.028f,.068f,.076f,.86f),new Color(.028f,.068f,.076f,.30f),new Color(.028f,.068f,.076f,0)},Offsets=new[]{0f,.45f,1f}}};
        questShade=new GradientTexture2D {Width=512,Height=1,FillFrom=Vector2.Zero,FillTo=Vector2.Right,
            Gradient=new Gradient {Colors=new[]{new Color(.028f,.068f,.076f,.8f),new Color(.028f,.068f,.076f,.8f),new Color(.028f,.068f,.076f,0)},Offsets=new[]{0f,.7f,1f}}};
        sceneShade=new GradientTexture2D {Width=1,Height=256,FillFrom=Vector2.Zero,FillTo=Vector2.Down,
            Gradient=new Gradient {Colors=new[]{new Color(.012f,.03f,.035f,0),new Color(.012f,.03f,.035f,.9f)},Offsets=new[]{0f,1f}}};
        menu=new Control {MouseFilter=MouseFilterEnum.Ignore};AddChild(menu);Resized+=RefreshMenu;serif.Fallbacks=SerifFallbacks();
    }
    public void ApplyLanguage(){serif.Fallbacks=SerifFallbacks();wrapped=("",0,0,"",new());RefreshMenu();}
    public override void _ExitTree() { serif.Dispose(); titleShade.Dispose(); sceneShade.Dispose(); questShade.Dispose(); }
    // Portraits are looked up by speaker (KOMA-7 -> koma7.png); a speaker without one is shown by name plate alone.
    public Texture2D? Portrait(string who)
    {
        if(!portraits.TryGetValue(who,out var t))
        {string path=$"res://assets/art/portraits/{who.ToLowerInvariant().Replace("-","")}.png";portraits[who]=t=ResourceLoader.Exists(path)?GD.Load<Texture2D>(path):null;}
        return t;
    }
    private void Text(string text,Vector2 p,int size,Color color,bool display=false)
    {DrawString(display?serif:font,p,text,HorizontalAlignment.Left,-1,size,color);}
    private void Center(string text,float y,int size,Color c,bool display=false)
    {var f=display?serif:font;Text(text,new((w-f.GetStringSize(text,fontSize:size).X)/2,y),size,c,display);}
    private void Panel(Rect2 rect,float alpha=.87f)
    {
        DrawStyleBox(new StyleBoxFlat {BgColor=new Color(ink,alpha),CornerRadiusTopLeft=3,CornerRadiusBottomRight=3,
            BorderColor=new Color(gold,.35f),BorderWidthLeft=1,BorderWidthBottom=1,BorderWidthRight=1,BorderWidthTop=1},rect);
    }
    private void DrawIcon(string name,Vector2 center,float size,Color c)=>DrawTextureRect(Icon(name),new Rect2(center-Vector2.One*size/2,Vector2.One*size),false,c);
    private float KeyWidth(string key,int size)=>Math.Max(size*1.9f,font.GetStringSize(key,fontSize:size).X+size*1.1f);
    // A key drawn as a small cap with its name on it; p is the cap's top-left corner.
    private void Keycap(string key,Vector2 p,int size=12)
    {
        var rect=new Rect2(p,KeyWidth(key,size),size*1.9f);
        DrawStyleBox(new StyleBoxFlat {BgColor=new Color(ink,.92f),BorderColor=new Color(cream,.75f),BorderWidthLeft=1,BorderWidthTop=1,BorderWidthRight=1,BorderWidthBottom=2,
            CornerRadiusTopLeft=3,CornerRadiusTopRight=3,CornerRadiusBottomLeft=3,CornerRadiusBottomRight=3},rect);
        Text(key,new(rect.GetCenter().X-font.GetStringSize(key,fontSize:size).X/2,rect.GetCenter().Y+size*.36f),size,cream);
    }
    private void Bar(Vector2 p,Vector2 size,float value,Color c)
    {
        DrawRect(new Rect2(p,size),new Color(.015f,.045f,.05f,.8f));
        DrawRect(new Rect2(p,new Vector2(size.X*Math.Clamp(value,0,1),size.Y)),c);
        DrawLine(p+new Vector2(0,size.Y),p+size,new Color(cream,.3f));
    }
    public override void _Draw()
    {
        Redraws++;scale=Math.Min(GetViewportRect().Size.X/1440,GetViewportRect().Size.Y/900);
        w=GetViewportRect().Size.X/scale;h=GetViewportRect().Size.Y/scale;
        DrawSetTransform(Vector2.Zero,0,Vector2.One*scale);menu.Scale=Vector2.One*scale;
        bool settings=Game.Mode==GameMode.Settings;
        if(Game.Mode==GameMode.Title||settings&&Game.SettingsFrom==GameMode.Title){Title();if(settings){DrawRect(new(0,0,w,h),new Color(.025f,.055f,.067f,.77f));Settings();}return;}
        if(Game.Mode==GameMode.Dialogue){Dialogue();return;}
        if(Game.TintTime>0)DrawRect(new Rect2(0,0,w,h),new Color(Game.TintColor,Game.TintColor.A*Game.TintTime/Game.TintLength));
        Quest();Minimap();Vitals();
        var closest=Game.Locked??Game.Enemies.Where(e=>!e.Dead&&e.GlobalPosition.DistanceTo(Game.Player.GlobalPosition)<12).OrderBy(e=>e.GlobalPosition.DistanceTo(Game.Player.GlobalPosition)).FirstOrDefault();
        if(closest!=null)Target(closest);
        if(Game.Mode==GameMode.Playing)
        {
            var (icon,label)=Game.Interaction();
            if(icon!="")
            {
                float width=font.GetStringSize(label,fontSize:18).X+108,x=(w-width)/2;Panel(new(x,h-225,width,44),.92f);
                Keycap("E",new(x+14,h-214.4f));DrawIcon(icon,new(x+66,h-203),30,cream);Text(label,new(x+90,h-196),18,cream);
            }
            if(Game.QuestStage<4&&!Game.Camera.IsPositionBehind(Game.ObjectivePosition))
            {
                var p=Game.Camera.UnprojectPosition(Game.ObjectivePosition+Vector3.Up*3.8f)/scale;
                if(p.X>30&&p.X<w-30&&p.Y>150&&p.Y<h-230)Diamond(p,9,gold);
            }
        }
        if(Game.ToastTime>0&&Game.Mode!=GameMode.Dialogue)
        {
            bool body=Game.ToastBody!="";Panel(new(w/2-280,174,560,body?83:56),.83f);string title=T(Game.ToastTitle).ToUpperInvariant();
            Center(title,body?208:211,23,gold,true);if(body)Center(T(Game.ToastBody),235,14,cream);
            if(Game.ToastIcon!="")DrawIcon(Game.ToastIcon,new((w-serif.GetStringSize(title,fontSize:23).X)/2-28,202),34,cream);
        }
        if(Game.FlashTime>0)
        {
            // Pops in a little larger for its first tenth of a second, then fades over its last quarter.
            float pop=Game.FlashTime>Game.FlashLength?0:Math.Clamp(1-(Game.FlashLength-Game.FlashTime)/.1f,0,1);
            DrawIcon(Game.FlashIcon,new(w/2,h*.44f),96*(1+.3f*pop),new Color(Game.FlashColor,Math.Min(1,Game.FlashTime/.25f)));
        }
        if(Game.FeedbackTime>0)Center(T(Game.FeedbackText),h*.46f,21,Game.FeedbackColor);
        if(Game.Mode==GameMode.Paused||Game.Mode==GameMode.Defeated)
        {
            DrawRect(new(0,0,w,h),new Color(.025f,.055f,.067f,.77f));
            Center(T(Game.Mode==GameMode.Paused?"A moment of quiet":"The light has not gone out"),h*.28f,39,cream,true);
            if(Game.Mode==GameMode.Paused)Controls(w/2+205,h*.40f);else Keycap("R",new(w/2-206,h*.43f+14),14);
        }
        if(Game.Mode==GameMode.Inventory){DrawRect(new(0,0,w,h),new Color(.025f,.055f,.067f,.77f));Inventory();Vitals();}
        if(settings){DrawRect(new(0,0,w,h),new Color(.025f,.055f,.067f,.77f));Settings();}
    }
    // The objective on one line; the patrols it counts are crests, each dimmed as it is shut down.
    private void Quest()
    {
        string text=Game.ObjectiveText;float x=78+font.GetStringSize(text,fontSize:17).X;
        DrawTextureRect(questShade,new(0,24,x+(Game.QuestStage==1?190:110),46),false);
        Diamond(new(44,47),6,gold);Text(text,new(62,53),17,cream);
        if(Game.QuestStage==1)foreach(var patrol in Game.Patrols){DrawIcon("sirius",new(x+12,47),28,patrol.Dead?new Color(cream,.22f):crest);x+=28;}
        if(Game.QuestStage<4)Text(F("{0} m",(int)Game.Player.GlobalPosition.DistanceTo(Game.ObjectivePosition)),new(x+8,53),12,muted);
    }
    // The nearest robot: its crest (a scout's smaller), health and posture, and the TAB key until it is locked on.
    private void Target(Enemy e)
    {
        float x=w/2-175,bars=Game.Locked==e?286:236;Panel(new(x,70,350,44),.8f);
        DrawIcon("sirius",new(x+24,92),e.Scout?26:34,crest);
        Bar(new(x+48,83),new(bars,7),e.Health/e.MaxHealth,new("d48568"));
        Bar(new(x+48,96),new(bars,4),e.Posture/100,gold);
        if(Game.Locked!=e)Keycap("TAB",new(x+296,81),11);
        if(Game.Locked!=null&&!Game.Camera.IsPositionBehind(e.GlobalPosition))
        {
            var p=Game.Camera.UnprojectPosition(e.GlobalPosition+Vector3.Up*1.4f)/scale;
            DrawArc(p,15,0,Mathf.Tau,32,new Color(gold,.8f),1.5f);DrawCircle(p,3,gold);
        }
    }
    private void Title()
    {
        DrawTextureRect(art,new(0,0,w,h),false);
        DrawTextureRect(titleShade,new(0,0,w,h),false);
        DrawRect(new(0,h-88,w,88),new Color(.018f,.04f,.05f,.6f));
        // The logo carries the game's name in every language; only the chapter is text.
        DrawTextureRect(logo,new(70,h*.25f-112,460,460f*logo.GetHeight()/logo.GetWidth()),false,cream);
        DrawLine(new(85,h*.25f+204),new(445,h*.25f+204),new Color(gold,.65f));
        Text(T("The train with no passengers"),new(85,h*.25f+240),19,cream);
    }
    // Mio's face from her dialogue portrait, vitality and posture as bars, and a pouch for each remedy she can carry.
    private void Vitals()
    {
        Panel(new(36,h-132,306,94),.88f);
        var face=new Vector2(84,h-85);DrawCircle(face,39,new Color(gold,.6f));
        if(Portrait("Mio") is {} mio)
        {
            var points=new Vector2[40];var uvs=new Vector2[40];
            for(int i=0;i<40;i++){var d=Vector2.FromAngle(i*Mathf.Tau/40);points[i]=face+d*37;uvs[i]=(new Vector2(490,255)+d*215)/mio.GetSize();}
            DrawPolygon(points,new[]{Colors.White},uvs,mio);
        }
        Bar(new(136,h-112),new(190,9),Game.Player.Health/100,jade);
        Bar(new(136,h-96),new(190,5),Game.Player.Posture/100,gold);
        float x=136;
        var bag=Game.Player.Inventory;
        for(int i=0;i<bag.Capacity("remedy");i++){DrawIcon("remedy",new(x+11,h-64),26,i<bag.Count("remedy")?cream:new Color(cream,.22f));x+=26;}
        Keycap("H",new(x+6,h-75.4f));
        Skill(new(398,h-86));
    }
    // Resonance Cut: its icon lights while it is cast, under a dot per charge that flashes red when Mio tries it with none left.
    private void Skill(Vector2 c)
    {
        int charges=Game.Player.SkillCharges;bool cast=Game.Player.Casting,warn=Game.Player.Spent>0&&(Time.GetTicksMsec()/120)%2==0;
        if(cast)DrawCircle(c,54,new Color(jade,.16f));
        DrawCircle(c,40,new Color(ink,.9f));DrawArc(c,40,0,Mathf.Tau,48,cast?jade:new Color(gold,.45f),cast?3:1.5f);
        DrawIcon("resonance",c,56,cast?cream:new Color(jade,charges>0?1:.3f));
        for(int i=0;i<Player.MaxSkillCharges;i++)DrawCircle(c+new Vector2((i-(Player.MaxSkillCharges-1)/2f)*14,54),4.5f,warn?red:i<charges?jade:new Color(jade,.2f));
        Keycap("Q",c+new Vector2(28,18));
    }
    // The key reference, shown on the pause screen rather than over play.
    private void Controls(float x,float y)
    {
        Panel(new(x,y,370,122),.9f);
        Text(T("WASD  Move      SHIFT  Run      SPACE  Jump"),new(x+18,y+27),13,cream);
        Text(T("LMB / J  Attack     RMB / K  Guard & deflect"),new(x+18,y+54),13,cream);
        Text(T("CTRL / ALT  Dodge     TAB  Lock     Q  Skill"),new(x+18,y+81),13,cream);
        Text(T("E  Interact    H  Remedy    I  Items    ESC  Pause"),new(x+18,y+107),12,gold);
    }
    private Vector2 ItemsCorner=>new(w/2-430,h/2-220);
    private Vector2 SettingsCorner=>new(w/2-280,h/2-180);
    // The settings screen: a bar for each volume, lit up to its level and ticked at each step, with a diamond at the end; the focused one is outlined in gold.
    private void Settings()
    {
        var o=SettingsCorner;Panel(new(o,560,360),.95f);
        Text(T("Settings"),o+new Vector2(32,52),30,cream,true);DrawLine(o+new Vector2(32,70),o+new Vector2(528,70),new Color(gold,.5f));
        foreach(var slider in sliders)
        {
            var rect=new Rect2(slider.Position,slider.Size);bool focus=slider.HasFocus();float x=rect.Position.X+rect.Size.X*(float)(slider.Value/slider.MaxValue);
            Text(T(slider.Name),rect.Position+new Vector2(0,-12),15,focus?gold:muted);
            DrawRect(rect,new Color(ink,.9f));DrawRect(new Rect2(rect.Position,new Vector2(x-rect.Position.X,rect.Size.Y)),new Color(gold,focus?.9f:.6f));
            for(int i=1;i<Volume.Steps;i++){float tick=rect.Position.X+rect.Size.X*i/Volume.Steps;DrawLine(new(tick,rect.Position.Y),new(tick,rect.End.Y),new Color(ink,.5f));}
            DrawRect(rect,focus?gold:new Color(gold,.3f),false,focus?3:2);
            var mid=new Vector2(x,rect.GetCenter().Y);
            DrawColoredPolygon(new[]{mid+Vector2.Up*13,mid+Vector2.Right*9,mid+Vector2.Down*13,mid+Vector2.Left*9},cream);
        }
        Keycap("ESC",o+new Vector2(32,291.6f));
    }
    // The items screen's slots: a row of consumables over a row of key items, each in the table's order.
    private IEnumerable<(Item Item,Rect2 Rect)> Slots()
    {
        foreach(var kind in new[]{ItemKind.Consumable,ItemKind.Key})
        {
            int i=0;
            foreach(var item in Game.Player.Inventory.Held.Where(it=>it.Kind==kind))yield return (item,new Rect2(ItemsCorner+new Vector2(32+i++*98,kind==ItemKind.Consumable?118:252),84,84));
        }
    }
    // Keeps the shown item if Mio still holds it, else shows the first she does.
    public void Select(string? id)
    {
        var held=Game.Player.Inventory.Held.ToList();Selected=held.Any(i=>i.Id==id)?id!:held.FirstOrDefault()?.Id??"";
        if(use!=null&&Selected!=""){use.Visible=Items.Get(Selected).Kind==ItemKind.Consumable;use.Disabled=Game.Player.Inventory.Count(Selected)==0;}
    }
    // The items screen: the slots on the left, and on the right the selected item's icon, name, count and text, over its Use button.
    private void Inventory()
    {
        var o=ItemsCorner;var bag=Game.Player.Inventory;Panel(new(o,860,440),.95f);
        Text(T("Items"),o+new Vector2(32,52),30,cream,true);DrawLine(o+new Vector2(32,70),o+new Vector2(828,70),new Color(gold,.5f));
        Text(T("Supplies"),o+new Vector2(32,106),13,muted);Text(T("Key items"),o+new Vector2(32,240),13,muted);
        DrawLine(o+new Vector2(440,94),o+new Vector2(440,410),new Color(gold,.25f));
        foreach(var (item,rect) in Slots())
        {
            bool chosen=item.Id==Selected;int n=bag.Count(item.Id);
            DrawRect(rect,new Color(ink,.9f));DrawRect(rect,chosen?gold:new Color(gold,.3f),false,chosen?3:2);
            DrawIcon(item.Icon,rect.GetCenter(),56,n>0?cream:new Color(cream,.25f));
            if(item.Kind==ItemKind.Consumable){string count=n.ToString();Text(count,rect.End-new Vector2(10+font.GetStringSize(count,fontSize:15).X,9),15,n>0?cream:muted);}
        }
        Keycap("ESC",o+new Vector2(32,377.6f));
        if(Selected=="")return;
        var shown=Items.Get(Selected);var c=o+new Vector2(522,140);
        DrawCircle(c,52,new Color(ink,.9f));DrawArc(c,52,0,Mathf.Tau,48,new Color(gold,.6f),1.5f);DrawIcon(shown.Icon,c,78,cream);
        Text(T(shown.Name),o+new Vector2(592,134),26,gold,true);
        if(shown.Kind==ItemKind.Consumable){Text($"{bag.Count(shown.Id)} / {bag.Capacity(shown.Id)}",o+new Vector2(594,164),15,cream);Keycap("E",o+new Vector2(470,377.6f));}
        float y=o.Y+234;string text=T(shown.Text);
        foreach(var (start,end) in Wrap(text,358,15)){Text(text[start..end].TrimEnd(),new(o.X+470,y),15,cream);y+=24;}
    }
    private void Minimap()
    {
        var center=new Vector2(w-130,131);float r=78;
        DrawCircle(center,r+4,new Color(ink,.93f));DrawArc(center,r,0,Mathf.Tau,64,new Color(gold,.7f),1);
        Text("N",center+new Vector2(-5,-90),11,gold);
        Vector2 Map(Vector3 p)=>new((p.X-Game.Player.GlobalPosition.X)*1.5f,(p.Z-Game.Player.GlobalPosition.Z)*1.5f);
        foreach(float riverZ in new[]{-8f,World.CedarZ})
        {
            float y=(riverZ-Game.Player.GlobalPosition.Z)*1.5f;
            if(Math.Abs(y)<r-3){float width=Mathf.Sqrt((r-3)*(r-3)-y*y);DrawLine(center+new Vector2(-width,y),center+new Vector2(width,y),new Color(.24f,.57f,.58f,.7f),5);}
        }
        for(float z=-6;z<56;z+=2)
        {var a=Map(new(World.BrookX(z),0,z));var b=Map(new(World.BrookX(z+2),0,z+2));if(a.Length()<r-3&&b.Length()<r-3)DrawLine(center+a,center+b,new Color(.24f,.57f,.58f,.7f),2);}
        void Road(Vector3 from,Vector3 to){var a=Map(from);var b=Map(to);if(a.Length()<r-2&&b.Length()<r-2)DrawLine(center+a,center+b,new Color(gold,.6f),2);}
        // The relay road follows its painted trails; RoadX() guides the road either side of it.
        for(float z=Game.Player.GlobalPosition.Z-52;z<Game.Player.GlobalPosition.Z+52;z+=2)
            if(z>=World.NorthEdge&&z<=World.SouthEdge-2&&!(z<-13&&z>World.RelayNorth+4))Road(new(World.RoadX(z),0,z),new(World.RoadX(z+2),0,z+2));
        foreach(var (_,_,paint,points) in World.Trails)if(paint>0)for(int i=1;i<points.Length;i++)
        {int n=(int)Math.Ceiling(points[i-1].DistanceTo(points[i])/2);for(int k=0;k<n;k++)Road(points[i-1].Lerp(points[i],(float)k/n),points[i-1].Lerp(points[i],(k+1f)/n));}
        foreach(var b in Game.World.Buildings)
        {var p=Map(b.Position);if(p.Length()<r-6)DrawRect(new Rect2(center+p-new Vector2(4,3),new Vector2(8,6)),new("9a9b77"));}
        foreach(var n in Game.World.Residents)
        {var p=Map(n.Body.Position);if(p.Length()<r-3)DrawCircle(center+p,2,jade);}
        foreach(var e in Game.Enemies.Where(e=>!e.Dead))
        {var p=Map(e.GlobalPosition);if(p.Length()<r-4)DrawCircle(center+p,3,new("e58b66"));}
        DrawCircle(center,4,cream);DrawLine(center,center+new Vector2(-Mathf.Sin(Game.CameraYaw),-Mathf.Cos(Game.CameraYaw))*10,cream,2);
        if(Game.QuestStage<4)Diamond(center+Map(Game.ObjectivePosition).LimitLength(r-9),4,gold);
        var link=center+new Vector2(-60,60);DrawCircle(link,17,new Color(ink,.95f));DrawArc(link,17,0,Mathf.Tau,32,new Color(jade,.6f),1);DrawIcon("zero",link,30,jade);
    }
    private void Diamond(Vector2 p,float radius,Color c)
        =>DrawPolyline(new[]{p+Vector2.Up*radius,p+Vector2.Right*radius,p+Vector2.Down*radius,p+Vector2.Left*radius,p+Vector2.Up*radius},c,2);
    // Visual-novel scene: Mio stands on the left, the resident she is talking to on the right, and whoever is not speaking dims.
    // Zero has no figure; its voice is a pulsing link emblem beside Mio, because it speaks from her wrist terminal.
    private void Dialogue()
    {
        bool night=Game.NightFrom>=0&&Game.Beat>=Game.NightFrom;
        DrawRect(new(0,0,w,h),night?new Color(.012f,.02f,.06f,.8f):new Color(.015f,.035f,.04f,.3f));
        DrawTextureRect(sceneShade,new(0,h-470,w,470),false);
        string who=Game.Speaker;var script=Game.Script;int beat=Math.Clamp(Game.Beat,0,Math.Max(0,script.Count-1));
        bool Other(string s)=>s is not ("" or "Mio" or "Zero");
        string partner=script.Take(beat+1).Select(b=>b.Speaker).LastOrDefault(Other)??script.Select(b=>b.Speaker).FirstOrDefault(Other)??"";
        Figure("Mio",false,who=="Mio");
        if(partner!="")Figure(partner,true,who==partner);
        if(script.Any(b=>b.Speaker=="Zero"))Link(new(w/2-44,h-430),who=="Zero");
        var box=new Rect2(w/2-560,h-262,1120,206);Panel(box,.95f);
        DrawLine(box.Position,box.Position+new Vector2(box.Size.X,0),gold,2);
        if(who!="")
        {
            bool right=Other(who);int nameSize=27;string name=T(who),role=T(Game.SpeakerRole);
            float nameWidth=serif.GetStringSize(name,fontSize:nameSize).X,plate=nameWidth+font.GetStringSize(role,fontSize:11).X+62;
            float px=right?box.End.X-30-plate:box.Position.X+30;
            Panel(new(px,box.Position.Y-38,plate,42),.97f);
            Text(name,new(px+18,box.Position.Y-8),nameSize,who=="Zero"?jade:gold,true);
            Text(role,new(px+nameWidth+32,box.Position.Y-12),11,who=="Zero"?gold:jade);
        }
        int shown=(int)Game.Reveal;float y=box.Position.Y+58;string text=Game.Dialogue;
        foreach(var (start,end) in Wrap(text,1030,20))
        {
            if(shown<=start)break;string line=text[start..end].TrimEnd(),part=text[start..Math.Min(end,shown)];
            if(who=="")Text(part,new((w-serif.GetStringSize(line,fontSize:21).X)/2,y),21,new Color(cream,.86f),true);
            else Text(part,new(box.Position.X+44,y),20,cream);
            y+=32;
        }
        // Next is E and an arrow that blinks once the line has typed out; skip is ESC and a double chevron.
        float kx=box.End.X-30,ky=box.End.Y-36;var dim=new Color(gold,.8f);
        foreach(float dx in new[]{-12f,-3f}){var v=new Vector2(kx+dx,ky+10.5f);DrawColoredPolygon(new[]{v+new Vector2(-4,-6),v+new Vector2(4,0),v+new Vector2(-4,6)},dim);}
        kx-=26+KeyWidth("ESC",11);Keycap("ESC",new(kx,ky),11);
        var c=new Vector2(kx-20,ky+10.5f);bool ready=Game.Reveal>=Game.Dialogue.Length;
        DrawColoredPolygon(new[]{c+new Vector2(-6,-4),c+new Vector2(6,-4),c+new Vector2(0,4)},ready&&(Time.GetTicksMsec()/450)%2==0?gold:new Color(gold,.35f));
        Keycap("E",new(c.X-14-KeyWidth("E",11),ky),11);
    }
    private void Figure(string who,bool right,bool speaking)
    {
        var t=Portrait(who);if(t==null)return;
        float height=760,width=t.GetWidth()*height/t.GetHeight();
        var rect=new Rect2(right?w-30-width:30,h-750+(speaking?0:14),width,height);
        DrawTextureRect(t,rect,false,speaking?Colors.White:new Color(.4f,.45f,.5f));
    }
    private void Link(Vector2 c,bool speaking)
    {
        float t=Time.GetTicksMsec()/1000f,glow=speaking?1:.35f;
        DrawCircle(c,64,new Color(jade,.10f*glow));DrawCircle(c,47,new Color(ink,.94f));
        DrawArc(c,47,0,Mathf.Tau,48,new Color(jade,.35f+.4f*glow),1.5f);
        for(int i=0;i<3;i++)DrawArc(c,55,t*.9f+i*Mathf.Tau/3,t*.9f+i*Mathf.Tau/3+.8f,12,new Color(jade,.7f*glow),2);
        DrawIcon("zero",c,72,new Color(jade,.5f+.5f*glow));
        bool voicing=speaking&&Game.Reveal<Game.Dialogue.Length;
        for(int i=0;i<9;i++)
        {
            float level=voicing?.35f+.65f*Mathf.Abs(Mathf.Sin(t*11+i*1.7f)*Mathf.Sin(t*4.3f+i)):.12f;
            DrawRect(new Rect2(c.X+72+i*9,c.Y-level*22,4,level*44),new Color(jade,.4f+.6f*glow));
        }
    }
    private (string Text,int Size,float Width,string Language,List<(int Start,int End)> Lines) wrapped=("",0,0,"",new());
    // Line breaks come from the text server, which knows where CJK text may break; the ranges index into text.
    public List<(int Start,int End)> Wrap(string text,float width,int size)
    {
        if(wrapped.Text==text&&wrapped.Size==size&&wrapped.Width==width&&wrapped.Language==Current)return wrapped.Lines;
        var paragraph=new TextParagraph {Width=width};paragraph.AddString(text,font,size,Current);
        var lines=Enumerable.Range(0,paragraph.GetLineCount()).Select(i=>{var r=paragraph.GetLineRange(i);return (r.X,r.Y);}).ToList();
        wrapped=(text,size,width,Current,lines);return lines;
    }
    public void RefreshMenu()
    {
        if(menu==null)return;
        foreach(var child in menu.GetChildren()){menu.RemoveChild(child);child.QueueFree();}
        use=null;sliders.Clear();
        float width=GetViewportRect().Size.X,height=GetViewportRect().Size.Y;
        scale=Math.Min(width/1440,height/900);w=width/scale;h=height/scale;
        if(Game.Mode==GameMode.Title)
        {
            Button("BEGIN JOURNEY     →",new(84,h*.25f+295),new(320,58),Game.Begin,true);
            Button("Settings",new(84,h*.25f+367),new(320,40),()=>Game.ShowSettings(true),false);
            Button("Quit",new(84,h*.25f+419),new(110,40),()=>GetTree().Quit(),false);
            LanguageButtons(214,h*.25f+419,104,40,8);
        }
        if(Game.Mode==GameMode.Paused)
        {
            Button("Resume journey",new(w/2-155,h*.40f),new(310,52),()=>Game.SetPaused(false),true);
            Button("Items",new(w/2-155,h*.40f+65),new(310,48),()=>Game.ShowItems(true),false);
            Button("Settings",new(w/2-155,h*.40f+126),new(310,48),()=>Game.ShowSettings(true),false);
            Button("Recover at the village",new(w/2-155,h*.40f+187),new(310,48),Game.Retry,false);
            Button("Restart prologue",new(w/2-155,h*.40f+248),new(310,48),Game.Restart,false);
            Button("Quit game",new(w/2-155,h*.40f+309),new(310,48),()=>GetTree().Quit(),false);
            LanguageButtons(w/2-155,h*.40f+372,98,40,8);
        }
        if(Game.Mode==GameMode.Inventory)
        {
            // The slots are bare buttons that only take focus and clicks; _Draw paints them, under the menu's children.
            Button? chosen=null;var o=ItemsCorner;
            foreach(var (item,rect) in Slots())
            {
                var slot=new Button {Position=rect.Position,Size=rect.Size,MouseDefaultCursorShape=CursorShape.PointingHand};
                foreach(string state in new[]{"normal","hover","pressed","focus"})slot.AddThemeStyleboxOverride(state,new StyleBoxEmpty());
                slot.FocusEntered+=()=>Select(item.Id);menu.AddChild(slot);if(item.Id==Selected)chosen=slot;
            }
            Button("Back",new(o.X+80,o.Y+366),new(170,46),()=>Game.ShowItems(false),false,grab:false);
            use=Button("Use",new(o.X+502,o.Y+366),new(170,46),()=>Game.UseItem(Selected),true,grab:false);
            Select(Selected);chosen?.GrabFocus();
        }
        if(Game.Mode==GameMode.Settings)
        {
            var o=SettingsCorner;
            for(int i=0;i<Volume.Buses.Length;i++)Slider(Volume.Buses[i],new(o.X+32,o.Y+130+i*80),new(496,32));
            Button("Back",new(o.X+80,o.Y+280),new(170,46),()=>Game.ShowSettings(false),false,grab:false);
            sliders[0].GrabFocus();
        }
        if(Game.Mode==GameMode.Defeated)
            Button("Return to Hoshikawa",new(w/2-160,h*.43f),new(320,55),Game.Retry,true);
    }
    private void Slider(string bus,Vector2 pos,Vector2 size)
    {
        var slider=new HSlider {Name=bus,Position=pos,Size=size,MaxValue=Volume.Steps,Step=1,Value=Volume.Level(bus),MouseDefaultCursorShape=CursorShape.PointingHand};
        foreach(string part in new[]{"slider","grabber_area","grabber_area_highlight"})slider.AddThemeStyleboxOverride(part,new StyleBoxEmpty());
        foreach(string part in new[]{"grabber","grabber_highlight","grabber_disabled"})slider.AddThemeIconOverride(part,bare);
        slider.ValueChanged+=value=>Game.SetVolume(bus,(int)value);menu.AddChild(slider);sliders.Add(slider);
    }
    // One button per language, each named in its own script; the current one is lit.
    private void LanguageButtons(float x,float y,float width,float height,float gap)
    {
        foreach(var (code,name) in Languages)
        {Button(name,new(x,y),new(width,height),()=>Game.SetLanguage(code),code==Current,NameFont(code),15,false);x+=width+gap;}
    }
    private Button Button(string text,Vector2 pos,Vector2 size,Action action,bool primary,Font? face=null,int fontSize=17,bool grab=true)
    {
        var b=new Button {Text=text,Position=pos,Size=size,MouseDefaultCursorShape=CursorShape.PointingHand};
        b.AddThemeFontSizeOverride("font_size",fontSize);if(face!=null)b.AddThemeFontOverride("font",face);
        foreach(string color in new[]{"font_color","font_hover_color","font_pressed_color","font_focus_color"})
            b.AddThemeColorOverride(color,primary?ink:cream);
        b.AddThemeColorOverride("font_disabled_color",new Color(cream,.35f));
        foreach(string state in new[]{"normal","hover","pressed","focus","disabled"})
            b.AddThemeStyleboxOverride(state,new StyleBoxFlat {BgColor=state=="disabled"?new Color(ink,.6f):primary?(state=="hover"?cream:gold):new Color(ink,.9f),
                BorderColor=state=="disabled"?new Color(gold,.3f):gold,BorderWidthLeft=1,BorderWidthRight=1,BorderWidthTop=1,BorderWidthBottom=1,
                ContentMarginLeft=16,ContentMarginRight=16,CornerRadiusTopLeft=2,CornerRadiusBottomRight=2});
        b.Pressed+=()=>action();menu.AddChild(b);if(primary&&grab)b.GrabFocus();return b;
    }
}
