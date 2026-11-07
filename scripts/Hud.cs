using Godot;
using static Hoshikawa.Localization;
namespace Hoshikawa;

public partial class Hud : Control
{
    public Game Game=null!;
    private readonly Font font=ThemeDB.FallbackFont;
    private readonly SystemFont serif=new() {FontNames=new[]{"Georgia","Noto Serif","DejaVu Serif"}};
    private Texture2D art=null!,logo=null!,lacquer=null!,starwell=null!,corner=null!,ring=null!,divider=null!;
    private GradientTexture2D titleShade=null!,sceneShade=null!,questShade=null!,vignette=null!;
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
    // The save point's row whose button was last pressed, which keeps the focus as the screen is rebuilt.
    private string pressed="";
    // The item the items screen shows, the one whose slot last took focus.
    public string Selected{get;private set;}="";
    private float scale=1,w=1440,h=900;
    public int Redraws;
    private Color ink=new("12282f"),cream=new("f1ead5"),muted=new("aec0b4"),gold=World.Gold,jade=World.Jade,red=new("f18470"),crest=new("e58b66");
    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);MouseFilter=MouseFilterEnum.Ignore;TextureFilter=TextureFilterEnum.LinearWithMipmaps;
        art=GD.Load<Texture2D>("res://assets/art/mio-keyart.png");logo=GD.Load<Texture2D>("res://assets/art/ui/logo.png");
        Texture2D Ui(string name)=>GD.Load<Texture2D>($"res://assets/art/ui/{name}.png");
        lacquer=Ui("lacquer");starwell=Ui("starwell");corner=Ui("corner");ring=Ui("crest");divider=Ui("divider");
        titleShade=new GradientTexture2D {Width=512,Height=1,FillFrom=Vector2.Zero,FillTo=Vector2.Right,
            Gradient=new Gradient {Colors=new[]{new Color(.028f,.068f,.076f,.86f),new Color(.028f,.068f,.076f,.30f),new Color(.028f,.068f,.076f,0)},Offsets=new[]{0f,.45f,1f}}};
        questShade=new GradientTexture2D {Width=512,Height=1,FillFrom=Vector2.Zero,FillTo=Vector2.Right,
            Gradient=new Gradient {Colors=new[]{new Color(.028f,.068f,.076f,.8f),new Color(.028f,.068f,.076f,.8f),new Color(.028f,.068f,.076f,0)},Offsets=new[]{0f,.7f,1f}}};
        sceneShade=new GradientTexture2D {Width=1,Height=256,FillFrom=Vector2.Zero,FillTo=Vector2.Down,
            Gradient=new Gradient {Colors=new[]{new Color(.012f,.03f,.035f,0),new Color(.012f,.03f,.035f,.9f)},Offsets=new[]{0f,1f}}};
        vignette=new GradientTexture2D {Width=256,Height=256,Fill=GradientTexture2D.FillEnum.Radial,FillFrom=new(.5f,.45f),FillTo=new(1,.45f),
            Gradient=new Gradient {Colors=new[]{new Color(.012f,.03f,.035f,.6f),new Color(.012f,.03f,.035f,.92f)},Offsets=new[]{0f,1f}}};
        menu=new Control {MouseFilter=MouseFilterEnum.Ignore};AddChild(menu);Resized+=RefreshMenu;serif.Fallbacks=SerifFallbacks();
    }
    public void ApplyLanguage(){serif.Fallbacks=SerifFallbacks();wrapped.Clear();RefreshMenu();}
    public override void _ExitTree() { serif.Dispose(); titleShade.Dispose(); sceneShade.Dispose(); questShade.Dispose(); vignette.Dispose(); }
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
    // Where each portrait's face is, as fractions of its image (centre, and radius as a share of the width), for the circle crops.
    private static readonly Dictionary<string,(float X,float Y,float R)> Faces=new(){["Mio"]=(.485f,.167f,.213f),["Chiaki"]=(.47f,.14f,.21f),["Chiyo"]=(.52f,.12f,.21f),["Emiko"]=(.52f,.12f,.21f),
        ["Haruka"]=(.53f,.13f,.21f),["KOMA-7"]=(.57f,.12f,.22f),["Naomi"]=(.47f,.17f,.21f),["Renji"]=(.44f,.14f,.21f),["Sota"]=(.46f,.15f,.21f),["Toma"]=(.52f,.14f,.21f),["Yui"]=(.53f,.16f,.21f)};
    // Each speaker's role is shown by its icon on their name ribbon; Zero's crest already holds its emblem.
    private static readonly Dictionary<string,string> RoleIcons=new(){["OFFLINE RUNNER"]="runner",["ENGINEER"]="engineer",["VILLAGE ELDER"]="elder",["MEDIC"]="medic",["MOUNTAIN MEDIC"]="medic",
        ["FARMER"]="farmer",["COURIER"]="courier",["PASS COURIER"]="courier",["OFFLINE GARDENER"]="gardener",["LODGE KEEPER"]="lodge",["ORCHARD KEEPER"]="orchard"};
    private void Face(string who,Vector2 c,float r)
    {
        if(Portrait(who) is not {} t)return;var (fx,fy,fr)=Faces.GetValueOrDefault(who,(.5f,.15f,.21f));float aspect=t.GetWidth()/(float)t.GetHeight();
        var points=new Vector2[40];var uvs=new Vector2[40];
        for(int i=0;i<40;i++){var d=Vector2.FromAngle(i*Mathf.Tau/40);points[i]=c+d*r;uvs[i]=new(fx+d.X*fr,fy+d.Y*fr*aspect);}
        DrawPolygon(points,new[]{Colors.White},uvs,t);
    }
    private static Vector2[] Octagon(Rect2 r,float cut)=>new Vector2[]{new(r.Position.X+cut,r.Position.Y),new(r.End.X-cut,r.Position.Y),new(r.End.X,r.Position.Y+cut),new(r.End.X,r.End.Y-cut),
        new(r.End.X-cut,r.End.Y),new(r.Position.X+cut,r.End.Y),new(r.Position.X,r.End.Y-cut),new(r.Position.X,r.Position.Y+cut)};
    private static readonly Color GoldDark=new("a8893f"),GoldLight=new("fff2c0"),LacquerC=new("0a1119");
    // A lacquered panel: a gold rim brightest at its middle round a chamfered box filled with the lacquer art (or a painting) cropped to its shape,
    // a sheen across its top, a hairline inside, and the gold maki-e ornament on two opposite corners, set a little out over the rim:
    // top right and bottom left when rising, else top left and bottom right.
    private void Lacquer(Rect2 rect,float cut,float ornament,bool rising,Texture2D? fill=null)
    {
        var rim=Octagon(rect,cut);DrawPolygon(rim,rim.Select(p=>{float t=1-Math.Abs(2*(p.X-rect.Position.X)/rect.Size.X-1);return GoldDark.Lerp(t<.6f?gold:GoldLight,t<.6f?t/.6f:(t-.6f)/.4f);}).ToArray());
        fill??=lacquer;var body=Octagon(rect.Grow(-1.5f),cut-1);var size=fill.GetSize();var shown=rect.Size/(size*Math.Max(rect.Size.X/size.X,rect.Size.Y/size.Y));var from=(Vector2.One-shown)/2;
        DrawPolygon(body,new[]{Colors.White},body.Select(p=>from+(p-rect.Position)/rect.Size*shown).ToArray(),fill);
        float sheen=rect.Size.Y*.35f;var glass=new Color(1,1,1,.06f);
        DrawPolygon(new Vector2[]{new(rect.Position.X+cut,rect.Position.Y+2),new(rect.End.X-cut,rect.Position.Y+2),new(rect.End.X-2,rect.Position.Y+sheen),new(rect.Position.X+2,rect.Position.Y+sheen)},new[]{glass,glass,Colors.Transparent,Colors.Transparent});
        var line=Octagon(rect.Grow(-8),cut-7);DrawPolyline(line.Append(line[0]).ToArray(),new Color(gold,.32f),1);
        var art=new Vector2(ornament*corner.GetWidth()/corner.GetHeight(),ornament);
        foreach(var (at,flip) in rising?new[]{(new Vector2(rect.End.X+12,rect.Position.Y-12),new Vector2(-1,1)),(new Vector2(rect.Position.X-12,rect.End.Y+12),new Vector2(1,-1))}
            :new[]{(rect.Position-new Vector2(12,12),Vector2.One),(rect.End+new Vector2(12,12),-Vector2.One)})
        {DrawSetTransform(at*scale,0,flip*scale);DrawTextureRect(corner,new Rect2(Vector2.Zero,art),false);}
        DrawSetTransform(Vector2.Zero,0,Vector2.One*scale);
    }
    // A card inside a lacquered panel: a dark box with its top-left and bottom-right corners cut, outlined in a gold hairline.
    private Vector2[] Card(Rect2 r,float cut=10,float fill=.28f)
    {
        var points=new Vector2[]{new(r.Position.X+cut,r.Position.Y),new(r.End.X,r.Position.Y),new(r.End.X,r.End.Y-cut),new(r.End.X-cut,r.End.Y),new(r.Position.X,r.End.Y),new(r.Position.X,r.Position.Y+cut)};
        DrawColoredPolygon(points,new Color(0,0,0,fill));DrawPolyline(points.Append(points[0]).ToArray(),new Color(gold,.35f),1);return points;
    }
    // Stardust has no icon art yet: it is a four-pointed star drawn here, which Mark() draws in place of an icon named StardustIcon.
    public const string StardustIcon="stardust";
    private void Star(Vector2 c,float r,Color color)
        =>DrawColoredPolygon(Enumerable.Range(0,8).Select(i=>c+Vector2.FromAngle(i*Mathf.Pi/4-Mathf.Pi/2)*(i%2==0?r:r*.36f)).ToArray(),color);
    private void Mark(string icon,Vector2 center,float size,Color c){if(icon==StardustIcon)Star(center,size*.45f,c);else DrawIcon(icon,center,size,c);}
    // A stardust amount, the star before the number, ending at x on the text's baseline y.
    private void Amount(int amount,float x,float y,int size,Color c,bool glow=false)
    {
        string text=amount.ToString();float width=serif.GetStringSize(text,fontSize:size).X;var star=new Vector2(x-width-size*.62f,y-size*.36f);
        if(glow){DrawCircle(star,size*.9f,new Color(gold,.1f));DrawCircle(star,size*.55f,new Color(gold,.2f));}
        Text(text,new(x-width,y),size,c,true);Star(star,size*.42f,gold);
    }
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
        if(Game.Mode is GameMode.Dialogue or GameMode.History){Dialogue();if(Game.Mode==GameMode.History)History();return;}
        if(Game.Blackout>0)DrawRect(new(0,0,w,h),new Color(0,0,0,Game.Blackout));
        if(Game.TintTime>0)DrawRect(new Rect2(0,0,w,h),new Color(Game.TintColor,Game.TintColor.A*Game.TintTime/Game.TintLength));
        Quest();if(!Game.InRoom)Minimap();Vitals();Wallet();
        var closest=Game.Locked??Game.Enemies.Where(e=>!e.Dead&&e.GlobalPosition.DistanceTo(Game.Player.GlobalPosition)<12).OrderBy(e=>e.GlobalPosition.DistanceTo(Game.Player.GlobalPosition)).FirstOrDefault();
        if(closest!=null)Target(closest);
        if(Game.Mode==GameMode.Playing)
        {
            var (icon,label)=Game.Interaction();
            if(label!="")
            {
                float mark=icon==""?0:38,width=font.GetStringSize(label,fontSize:18).X+70+mark,x=(w-width)/2;Panel(new(x,h-225,width,44),.92f);
                Keycap("E",new(x+14,h-214.4f));if(icon!="")Mark(icon,new(x+66,h-203),30,cream);Text(label,new(x+52+mark,h-196),18,cream);
            }
            if(!Game.Camera.IsPositionBehind(Game.ObjectivePosition))
            {
                var p=Game.Camera.UnprojectPosition(Game.ObjectivePosition+Vector3.Up*3.8f)/scale;
                if(p.X>30&&p.X<w-30&&p.Y>150&&p.Y<h-230)Diamond(p,9,gold);
            }
        }
        if(Game.ToastTime>0&&Game.Mode!=GameMode.Dialogue)
        {
            bool body=Game.ToastBody!="";Panel(new(w/2-280,174,560,body?83:56),.83f);string title=T(Game.ToastTitle).ToUpperInvariant();
            Center(title,body?208:211,23,gold,true);if(body)Center(T(Game.ToastBody),235,14,cream);
            if(Game.ToastIcon!="")Mark(Game.ToastIcon,new((w-serif.GetStringSize(title,fontSize:23).X)/2-28,202),34,cream);
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
        if(Game.Mode==GameMode.Inventory){DrawRect(new(0,0,w,h),new Color(.025f,.055f,.067f,.77f));Inventory();Vitals();Wallet();}
        if(settings){DrawRect(new(0,0,w,h),new Color(.025f,.055f,.067f,.77f));Settings();}
        if(Game.Mode==GameMode.Rest){DrawTextureRect(vignette,new(0,0,w,h),false);SavePoint();Vitals();}
    }
    // The objective on one line; the patrols it counts are crests, each dimmed as it is shut down.
    private void Quest()
    {
        string text=Game.ObjectiveText;float x=78+font.GetStringSize(text,fontSize:17).X;
        DrawTextureRect(questShade,new(0,24,x+(Game.QuestStage==Hoshikawa.Quest.Road?190:110),46),false);
        Diamond(new(44,47),6,gold);Text(text,new(62,53),17,cream);
        if(Game.QuestStage==Hoshikawa.Quest.Road)foreach(var patrol in Game.Patrols){DrawIcon("sirius",new(x+12,47),28,patrol.Dead?new Color(cream,.22f):crest);x+=28;}
        Text(F("{0} m",(int)Game.Player.GlobalPosition.DistanceTo(Game.ObjectivePosition)),new(x+8,53),12,muted);
    }
    // The nearest robot: its crest (smaller for a smaller robot), health and posture, and the TAB key until it is locked on.
    private void Target(Enemy e)
    {
        float x=w/2-175,bars=Game.Locked==e?286:236;Panel(new(x,70,350,44),.8f);
        DrawIcon("sirius",new(x+24,92),30*e.Kind.Size,crest);
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
        var face=new Vector2(84,h-85);DrawCircle(face,39,new Color(gold,.6f));Face("Mio",face,37);
        var mio=Game.Player;Bar(new(136,h-114),new(190,9),mio.Health/mio.MaxHealth,jade);
        Bar(new(136,h-101),new(190,5),mio.Stamina/mio.MaxStamina,mio.Winded?red:cream);
        Bar(new(136,h-92),new(190,4),mio.Posture/100,gold);
        float x=136;
        var bag=Game.Player.Inventory;
        for(int i=0;i<bag.Capacity("remedy");i++){DrawIcon("remedy",new(x+11,h-64),26,i<bag.Count("remedy")?cream:new Color(cream,.22f));x+=26;}
        Keycap("H",new(x+6,h-75.4f));
        Skill(new(398,h-86));
    }
    // The stardust Mio carries, bottom right, with what she last earned rising over it.
    private void Wallet()
    {
        Panel(new(w-196,h-82,160,44),.85f);Amount(Game.Player.Growth.Stardust,w-52,h-50,22,cream);
        if(Game.GainTime>0)Text($"+{Game.Gained}",new(w-110,h-94-(2.5f-Game.GainTime)*8),17,new Color(gold,Math.Min(1,Game.GainTime)));
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
    private Vector2 RestCorner=>new(w/2-520,h/2-300);
    private Rect2 StatCard(int i)=>new(RestCorner+new Vector2(420+i%2*298,186+i/2*120),282,106);
    private Rect2 TradeCard(int i)=>new(RestCorner+new Vector2(420,428+i*120),580,106);
    // The save point's screen: the Starwell's painting fills a lacquered panel, the well clear on the left under its name and darkening on the
    // right under the cards. There, the stardust Mio carries; her level and what the next point costs; a card per stat, its icon, value now and
    // after one more point; and a card per priced item with how many she carries and its price. The buttons are RefreshMenu's.
    private void SavePoint()
    {
        var o=RestCorner;var growth=Game.Player.Growth;var bag=Game.Player.Inventory;Lacquer(new(o,1040,600),24,190,false,starwell);
        Color shade=new(LacquerC,.86f),clear=new(LacquerC,0),dusk=new(LacquerC,.7f);
        DrawPolygon(new Vector2[]{o+new Vector2(330,2),o+new Vector2(560,2),o+new Vector2(1015,2),o+new Vector2(1038,25),o+new Vector2(1038,575),o+new Vector2(1015,598),o+new Vector2(560,598),o+new Vector2(330,598)},
            new[]{clear,shade,shade,shade,shade,shade,shade,clear});
        DrawPolygon(new Vector2[]{o+new Vector2(25,2),o+new Vector2(400,2),o+new Vector2(400,150),o+new Vector2(2,150),o+new Vector2(2,25)},new[]{dusk,clear,clear,clear,dusk});
        Text(T(World.SavePointName),o+new Vector2(66,90),44,gold,true);
        Amount(growth.Stardust,o.X+1000,o.Y+82,34,cream,glow:true);DrawTextureRect(divider,new Rect2(o+new Vector2(410,104),600,50),false);
        bool afford=growth.Stardust>=growth.NextCost;
        DrawIcon("level",o+new Vector2(438,164),30,gold);Text(growth.Level.ToString(),o+new Vector2(462,174),26,cream,true);
        Amount(growth.NextCost,o.X+1000,o.Y+172,18,afford?gold:red);DrawIcon("raise",o+new Vector2(1000-serif.GetStringSize(growth.NextCost.ToString(),fontSize:18).X-40,165),16,afford?gold:red);
        for(int i=0;i<Stats.All.Length;i++)
        {
            var stat=Stats.All[i];var card=StatCard(i);Card(card,10,.5f);var p=card.Position;
            DrawCircle(p+new Vector2(48,53),31,new Color(gold,.1f));DrawIcon(stat.Id,p+new Vector2(48,53),44,GoldLight);
            Text(T(stat.Short),p+new Vector2(94,38),17,gold,true);
            string value=$"{growth.Value(stat.Id):0}";Text(value,p+new Vector2(94,84),30,cream,true);
            Text($"→ {growth.Value(stat.Id)+stat.PerPoint:0}",p+new Vector2(102+serif.GetStringSize(value,fontSize:30).X,82),16,afford?cream:muted);
        }
        foreach(var (item,i) in Items.All.Where(it=>it.Price>0).Select((it,i)=>(it,i)))
        {
            var card=TradeCard(i);Card(card,10,.5f);var p=card.Position;int n=bag.Count(item.Id);var icon=p+new Vector2(54,53);
            DrawCircle(icon,44,new Color(gold,.08f));DrawCircle(icon,38,new Color(ink,.9f));DrawArc(icon,38,0,Mathf.Tau,48,new Color(gold,.7f),1.5f);DrawIcon(item.Icon,icon,56,cream);
            Text(T(item.Name),p+new Vector2(110,44),17,cream);Text($"{n} / {bag.Capacity(item.Id)}",p+new Vector2(110,72),13,muted);
            Amount(item.Price,card.End.X-80,p.Y+64,24,item.Price<=growth.Stardust?gold:red);
        }
        Keycap("ESC",o+new Vector2(46,541.6f));
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
        Diamond(center+Map(Game.ObjectivePosition).LimitLength(r-9),4,gold);
        var link=center+new Vector2(-60,60);DrawCircle(link,17,new Color(ink,.95f));DrawArc(link,17,0,Mathf.Tau,32,new Color(jade,.6f),1);DrawIcon("zero",link,30,jade);
    }
    private void Diamond(Vector2 p,float radius,Color c)
        =>DrawPolyline(new[]{p+Vector2.Up*radius,p+Vector2.Right*radius,p+Vector2.Down*radius,p+Vector2.Left*radius,p+Vector2.Up*radius},c,2);
    // Visual-novel scene: Mio stands on the left, the resident she is talking to on the right, and whoever is not speaking dims.
    // Zero has no figure; its voice is a pulsing link emblem beside Mio, because it speaks from her wrist terminal.
    private void Dialogue()
    {
        // A day's title card stands alone on black.
        if(Game.OnCard){DrawRect(new(0,0,w,h),Colors.Black);Center(Game.Dialogue,h/2+14,40,gold,true);return;}
        bool night=Game.NightFrom>=0&&Game.Beat>=Game.NightFrom;
        DrawRect(new(0,0,w,h),night?new Color(.012f,.02f,.06f,.8f):new Color(.015f,.035f,.04f,.3f));
        if(Game.Blackout>0)DrawRect(new(0,0,w,h),new Color(0,0,0,Game.Blackout));
        DrawTextureRect(sceneShade,new(0,h-470,w,470),false);
        string who=Game.Speaker;var script=Game.Script;int beat=Math.Clamp(Game.Beat,0,Math.Max(0,script.Count-1));
        bool Other(string s)=>s is not ("" or "Mio" or "Zero");
        string partner=script.Take(beat+1).Select(b=>b.Speaker).LastOrDefault(Other)??script.Select(b=>b.Speaker).FirstOrDefault(Other)??"";
        // Mio has no figure in narration alone, nor until she is awake.
        if(script.Any(b=>b.Speaker!="")&&Game.Player.Visual.Clip is not ("sleep" or "wake"))Figure("Mio",false,who=="Mio");
        if(partner!="")Figure(partner,true,who==partner);
        // A staged shot has its characters in the middle, so Zero's emblem moves up out of their way.
        if(script.Any(b=>b.Speaker=="Zero"))Link(new(w/2-44,Game.Staged?118:h-430),who=="Zero");
        var box=new Rect2(w/2-560,h-266,1120,208);Lacquer(box,20,128,true);
        // The speaker on a gold ribbon off the box's top-left corner, their role as an icon after the name, behind the gold ring of a crest holding
        // their face (Zero's link emblem, having no face), with a few grains of stardust drifting off the ribbon's end; narration has neither.
        if(who!="")
        {
            float t=Time.GetTicksMsec()/1000f;int nameSize=26;string name=T(who);string? role=RoleIcons.GetValueOrDefault(Game.SpeakerRole);
            float nameWidth=serif.GetStringSize(name,fontSize:nameSize).X,ribbon=40+nameWidth+(role!=null?14+26:0)+30;
            var rx=box.Position+new Vector2(104,-26);
            DrawPolygon(new Vector2[]{rx,rx+new Vector2(ribbon,0),rx+new Vector2(ribbon-14,44),rx+new Vector2(0,44)},new[]{gold,new Color("f4dc93"),new Color("f4dc93"),gold});
            Text(name,rx+new Vector2(40,32),nameSize,ink,true);if(role!=null)DrawIcon(role,rx+new Vector2(40+nameWidth+14+13,22),26,ink);
            var tail=rx+new Vector2(ribbon,0);
            foreach(var (dx,dy,r,phase) in new[]{(14f,10f,2.5f,0f),(34f,-4f,1.5f,1.3f),(52f,14f,2f,2.1f),(74f,2f,1.5f,.7f),(96f,-10f,2f,2.9f),(118f,16f,1.5f,1.8f),(150f,6f,2f,.4f),(188f,-6f,1.5f,2.4f)})
            {var p=tail+new Vector2(dx,dy+Mathf.Sin(t*.8f+phase)*3);float a=.5f+.5f*Mathf.Sin(t*2.2f+phase);DrawCircle(p,r*2.6f,new Color(gold,.22f*a));DrawCircle(p,r,new Color(gold,.6f+.4f*a));}
            var crest=box.Position+new Vector2(70,-14);
            DrawCircle(crest,62,new Color(gold,.08f));DrawCircle(crest,44,ink);
            if(who=="Zero")DrawIcon("zero",crest,60,jade);else Face(who,crest,42);
            DrawTextureRect(ring,new Rect2(crest-Vector2.One*58,Vector2.One*116),false);
        }
        int shown=(int)Game.Reveal;float y=box.Position.Y+66;string text=Game.Dialogue;
        foreach(var (start,end) in Wrap(text,1030,20))
        {
            if(shown<=start)break;string line=text[start..end].TrimEnd(),part=text[start..Math.Min(end,shown)];
            if(who=="")Text(part,new((w-serif.GetStringSize(line,fontSize:21).X)/2,y),21,new Color(cream,.86f),true);
            else Text(part,new(box.Position.X+52,y),20,cream);
            y+=32;
        }
        // Each key a scene takes before what it does, right to left from the box's corner: ESC skips the scene (chevrons to a bar), CTRL held
        // fast-forwards (chevrons, lit while held), L opens the history (ruled lines), and E goes on (a star that blinks once the line has typed out).
        float x=box.End.X-33,ky=box.End.Y-40,mid=ky+10.5f;var dim=new Color(gold,.8f);
        void Chevron(float cx,Color c)=>DrawColoredPolygon(new[]{new Vector2(cx-4,mid-6),new Vector2(cx+4,mid),new Vector2(cx-4,mid+6)},c);
        void Cap(string key,float glyph){x-=glyph+8+KeyWidth(key,11);Keycap(key,new(x,ky),11);x-=18;}
        Chevron(x-15,dim);Chevron(x-7,dim);DrawRect(new(x-2.5f,mid-6,2.5f,12),dim);Cap("ESC",20);
        var fast=Game.Fast?GoldLight:dim;Chevron(x-13,fast);Chevron(x-4,fast);Cap("CTRL",17);
        for(int i=-1;i<2;i++)DrawLine(new(x-14,mid+i*5),new(x,mid+i*5),dim,2);Cap("L",14);
        bool ready=Game.Reveal>=Game.Dialogue.Length;Star(new(x-7,mid),7,ready&&(Time.GetTicksMsec()/450)%2==0?gold:new Color(gold,.35f));Cap("E",14);
    }
    // The history's lines as last drawn, by their index in Game.Log, each in screen pixels, for clicks to replay.
    private readonly List<(int Line,Rect2 Rect)> shown=new();
    public IReadOnlyList<(int Line,Rect2 Rect)> Shown=>shown;
    public int? LineAt(Vector2 screen){foreach(var (line,rect) in shown)if(rect.HasPoint(screen))return line;return null;}
    private void Play(Vector2 c,Color color)=>DrawColoredPolygon(new[]{c+new Vector2(-4,-6),c+new Vector2(6,0),c+new Vector2(-4,6)},color);
    // The history over the dimmed scene: a lacquered panel of the lines heard, newest at the bottom, each speaker's face on a small crest and
    // their name in gold beside what they said, narration with neither, a hairline where a scene began. Chevrons show there is more above or below.
    // A voiced line has a play mark, ringed and lit while it replays; the bottom line, which E replays, sits on a card.
    private void History()
    {
        DrawTextureRect(vignette,new(0,0,w,h),false);shown.Clear();
        var o=new Vector2(w/2-520,h/2-330);Lacquer(new(o,1040,660),24,150,false);
        Text(T("History"),o+new Vector2(60,70),30,gold,true);DrawTextureRect(divider,new Rect2(o+new Vector2(400,40),600,50),false);
        float top=o.Y+108,y=o.Y+590;var log=Game.Log;int i=log.Count-1-Game.LogScroll;
        for(;i>=0;i--)
        {
            var (scene,beat,speaker,english,voice)=log[i];string text=Line(scene,beat,english);var lines=Wrap(text,750,18);float height=lines.Count*28;
            if(y-height<top)break;
            float at=y-height;var box=new Rect2(o.X+44,at-9,952,height+7);shown.Add((i,new Rect2(box.Position*scale,box.Size*scale)));
            if(i==log.Count-1-Game.LogScroll)Card(box,8,.3f);
            if(voice!=null){var c=new Vector2(o.X+198,at+13);bool playing=Game.Replaying==i;if(playing)DrawArc(c,11,0,Mathf.Tau,24,GoldLight,1.5f);Play(c,playing?GoldLight:new Color(gold,.55f));}
            if(speaker!="")
            {
                var c=new Vector2(o.X+78,at+12);DrawCircle(c,17,ink);
                if(speaker=="Zero")DrawIcon("zero",c,26,jade);else Face(speaker,c,16);
                DrawArc(c,17,0,Mathf.Tau,32,new Color(gold,.7f),1.5f);Text(T(speaker),new(o.X+104,at+20),18,gold,true);
            }
            for(int k=0;k<lines.Count;k++)
            {
                string part=text[lines[k].Start..lines[k].End].TrimEnd();var p=new Vector2(o.X+220,at+20+k*28);
                if(speaker=="")Text(part,p,18,new Color(cream,.7f),true);else Text(part,p,18,cream);
            }
            y=at-(beat==0?28:14);
            if(beat==0&&i>0&&y>top)DrawLine(new(o.X+60,y+14),new(o.X+980,y+14),new Color(gold,.22f),1);
        }
        var more=new Color(gold,.8f);
        if(i>=0)DrawColoredPolygon(new[]{new Vector2(o.X+990,top-14),new Vector2(o.X+998,top-4),new Vector2(o.X+982,top-4)},more);
        if(Game.LogScroll>0)DrawColoredPolygon(new[]{new Vector2(o.X+990,o.Y+632),new Vector2(o.X+998,o.Y+622),new Vector2(o.X+982,o.Y+622)},more);
        Keycap("ESC",o+new Vector2(60,618.6f),11);
        float ex=o.X+78+KeyWidth("ESC",11);Keycap("E",new(ex,o.Y+618.6f),11);
        Play(new(ex+KeyWidth("E",11)+14,o.Y+629),log.Count>0&&log[log.Count-1-Game.LogScroll].Voice!=null?new Color(gold,.8f):new Color(gold,.25f));
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
    private readonly Dictionary<(string Text,int Size,float Width,string Language),List<(int Start,int End)>> wrapped=new();
    // Line breaks come from the text server, which knows where CJK text may break; the ranges index into text. Kept per text, as the history wraps many.
    public List<(int Start,int End)> Wrap(string text,float width,int size)
    {
        if(wrapped.TryGetValue((text,size,width,Current),out var cached))return cached;
        var paragraph=new TextParagraph {Width=width};paragraph.AddString(text,font,size,Current);
        return wrapped[(text,size,width,Current)]=Enumerable.Range(0,paragraph.GetLineCount()).Select(i=>{var r=paragraph.GetLineRange(i);return (r.X,r.Y);}).ToList();
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
        if(Game.Mode==GameMode.Rest)
        {
            // Focus stays on the row last pressed, else the first that can be pressed, else Back.
            var o=RestCorner;var growth=Game.Player.Growth;var bag=Game.Player.Inventory;var rows=new List<(string Id,Button Button)>();
            for(int i=0;i<Stats.All.Length;i++)
            {
                string id=Stats.All[i].Id;var card=StatCard(i);var raise=IconButton("Raise",id,F("Raise {0}",T(Stats.All[i].Short)),new(card.End.X-58,card.Position.Y+12),()=>{pressed=id;Game.Raise(id);});
                raise.Disabled=growth.Stardust<growth.NextCost;rows.Add((id,raise));
            }
            foreach(var (item,i) in Items.All.Where(it=>it.Price>0).Select((it,i)=>(it,i)))
            {
                string id=item.Id;var card=TradeCard(i);var buy=IconButton("Buy",id,F("Buy {0}",T(item.Name)),new(card.End.X-58,card.Position.Y+33),()=>{pressed=id;Game.Buy(id);});
                buy.Disabled=item.Price>growth.Stardust||bag.Count(id)>=bag.Capacity(id);rows.Add((id,buy));
            }
            var back=Button("Back",new(o.X+92,o.Y+530),new(170,46),Game.LeaveRest,false,grab:false);
            (rows.FirstOrDefault(r=>r.Id==pressed&&!r.Button.Disabled).Button??rows.FirstOrDefault(r=>!r.Button.Disabled).Button??back).GrabFocus();
        }
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
                BorderColor=state=="disabled"?new Color(gold,.3f):gold,BorderWidthLeft=state=="focus"?2:1,BorderWidthRight=state=="focus"?2:1,BorderWidthTop=state=="focus"?2:1,BorderWidthBottom=state=="focus"?2:1,
                ContentMarginLeft=16,ContentMarginRight=16,CornerRadiusTopLeft=2,CornerRadiusBottomRight=2});
        b.Pressed+=()=>action();menu.AddChild(b);if(primary&&grab)b.GrabFocus();return b;
    }
    // A button showing only its verb's icon, named for the verb and its object (Raise hp) so tests and focus logs can tell it apart; the translated label (Raise HP) is its tooltip and accessible name.
    private Button IconButton(string verb,string id,string label,Vector2 pos,Action action)
    {
        var b=Button("",pos,new(46,40),action,false,grab:false);b.Name=$"{verb} {id}";b.TooltipText=b.AccessibilityName=label;b.Icon=Icon(verb.ToLowerInvariant());b.ExpandIcon=true;b.IconAlignment=HorizontalAlignment.Center;
        b.AddThemeConstantOverride("icon_max_width",22);foreach(var (state,c) in new[]{("normal",cream),("hover",gold),("pressed",gold),("focus",GoldLight),("disabled",new Color(cream,.3f))})b.AddThemeColorOverride($"icon_{state}_color",c);
        foreach(string state in new[]{"normal","hover","pressed","focus","disabled"})((StyleBoxFlat)b.GetThemeStylebox(state)).ContentMarginLeft=((StyleBoxFlat)b.GetThemeStylebox(state)).ContentMarginRight=8;
        return b;
    }
}
