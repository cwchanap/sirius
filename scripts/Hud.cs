using Godot;
using static Hoshikawa.Localization;
namespace Hoshikawa;

public partial class Hud : Control
{
    public Game Game=null!;
    private readonly Font font=ThemeDB.FallbackFont;
    private readonly SystemFont serif=new() {FontNames=new[]{"Georgia","Noto Serif","DejaVu Serif"}};
    private Texture2D art=null!;
    private GradientTexture2D titleShade=null!,sceneShade=null!;
    private readonly Dictionary<string,Texture2D?> portraits=new();
    private Control menu=null!;
    private float scale=1,w=1440,h=900;
    public int Redraws;
    private Color ink=new("12282f"),cream=new("f1ead5"),muted=new("aec0b4"),gold=World.Gold,jade=World.Jade;
    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);MouseFilter=MouseFilterEnum.Ignore;
        art=GD.Load<Texture2D>("res://assets/art/mio-keyart.png");
        titleShade=new GradientTexture2D {Width=512,Height=1,FillFrom=Vector2.Zero,FillTo=Vector2.Right,
            Gradient=new Gradient {Colors=new[]{new Color(.028f,.068f,.076f,.86f),new Color(.028f,.068f,.076f,.30f),new Color(.028f,.068f,.076f,0)},Offsets=new[]{0f,.45f,1f}}};
        sceneShade=new GradientTexture2D {Width=1,Height=256,FillFrom=Vector2.Zero,FillTo=Vector2.Down,
            Gradient=new Gradient {Colors=new[]{new Color(.012f,.03f,.035f,0),new Color(.012f,.03f,.035f,.9f)},Offsets=new[]{0f,1f}}};
        menu=new Control {MouseFilter=MouseFilterEnum.Ignore};AddChild(menu);Resized+=RefreshMenu;serif.Fallbacks=SerifFallbacks();
    }
    public void ApplyLanguage(){serif.Fallbacks=SerifFallbacks();wrapped=("",0,"",new());RefreshMenu();}
    public override void _ExitTree() { serif.Dispose(); titleShade.Dispose(); sceneShade.Dispose(); }
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
        if(Game.Mode==GameMode.Title){Title();return;}
        if(Game.Mode==GameMode.Dialogue){Dialogue();return;}
        Panel(new(30,24,385,160),.78f);
        Text(T(Game.Region.ToUpperInvariant()),new(42,52),25,cream,true);
        Text(T(Game.Region switch { "Komorebi"=>"NORTHERN SETTLEMENT  /  LODGE & CLINIC", "Cedar Pass"=>"CEDAR RIVER  /  ORCHARDS & HIGHLANDS",
            "Old Relay Road"=>"MANUAL RELAY  /  PATROL ROAD", _=>"THE OFFLINE VALLEY  /  HUMAN SETTLEMENT" }),new(44,77),11,gold);
        DrawLine(new(44,93),new(350,93),new Color(gold,.55f));
        Text(T("01   THE TRAIN WITH NO PASSENGERS"),new(44,120),12,gold);
        Text(Game.ObjectiveText,new(44,145),17,cream);
        if(Game.QuestStage<4)Text(F("{0} m",(int)Game.Player.GlobalPosition.DistanceTo(Game.ObjectivePosition)),new(44,169),12,muted);
        Minimap();Vitals();Controls();
        Text(T("2147  /  SHIKOKU HIGHLANDS"),new(w-280,h-26),11,new Color(cream,.65f));
        var closest=Game.Locked??Game.Enemies.Where(e=>!e.Dead&&e.GlobalPosition.DistanceTo(Game.Player.GlobalPosition)<12).OrderBy(e=>e.GlobalPosition.DistanceTo(Game.Player.GlobalPosition)).FirstOrDefault();
        if(closest!=null)
        {
            float x=w/2-170;Text(F("SIRIUS  /  PATROL {0}",closest.Index.ToString("00")),new(x,80),13,cream);
            Bar(new(x,94),new(340,7),closest.Health/100,new("d48568"));
            Bar(new(x,106),new(340,4),closest.Posture/100,gold);
            Text(T(closest.Broken?"POSTURE BROKEN · ATTACK TO FINISH":closest.Perilous&&closest.State=="PERILOUS"?"◆ PERILOUS SWEEP · DODGE / JUMP":Game.Locked==closest?"TARGET LOCKED":"TAB   LOCK TARGET"),new(x,131),11,closest.Perilous?new("ff9c83"):gold);
            if(Game.Locked!=null&&!Game.Camera.IsPositionBehind(closest.GlobalPosition))
            {
                var p=Game.Camera.UnprojectPosition(closest.GlobalPosition+Vector3.Up*1.4f)/scale;
                DrawArc(p,15,0,Mathf.Tau,32,new Color(gold,.8f),1.5f);DrawCircle(p,3,gold);
            }
        }
        if(Game.Mode==GameMode.Playing)
        {
            string prompt=Game.InteractionPrompt();
            if(prompt!="")
            {
                var width=font.GetStringSize(prompt,fontSize:18).X+46;Panel(new((w-width)/2,h-225,width,44),.92f);
                Center(prompt,h-196,18,cream);
            }
            if(Game.QuestStage<4&&!Game.Camera.IsPositionBehind(Game.ObjectivePosition))
            {
                var p=Game.Camera.UnprojectPosition(Game.ObjectivePosition+Vector3.Up*3.8f)/scale;
                if(p.X>30&&p.X<w-30&&p.Y>150&&p.Y<h-230)Diamond(p,9,gold);
            }
        }
        if(Game.ToastTime>0&&Game.Mode!=GameMode.Dialogue)
        {
            Panel(new(w/2-280,174,560,83),.83f);
            Center(T(Game.ToastTitle),208,23,gold,true);Center(T(Game.ToastBody),235,14,cream);
        }
        if(Game.FeedbackTime>0)Center(T(Game.FeedbackText),h*.46f,21,Game.FeedbackColor);
        if(Game.Mode==GameMode.Paused||Game.Mode==GameMode.Defeated)
        {
            DrawRect(new(0,0,w,h),new Color(.025f,.055f,.067f,.77f));
            Center(T(Game.Mode==GameMode.Paused?"A moment of quiet":"The light has not gone out"),h*.28f,39,cream,true);
            Center(T(Game.Mode==GameMode.Paused?"HOSHIKAWA  /  JOURNEY PAUSED":"Recover in the village. Disabled patrols stay disabled."),h*.28f+37,14,muted);
        }
    }
    private void Title()
    {
        DrawTextureRect(art,new(0,0,w,h),false);
        DrawTextureRect(titleShade,new(0,0,w,h),false);
        DrawRect(new(0,h-88,w,88),new Color(.018f,.04f,.05f,.6f));
        Text(T("A STORY OF TWO WORLDS"),new(83,h*.25f-38),13,gold);
        Text(T("Hoshikawa"),new(78,h*.25f+35),70,cream,true);
        Text(T("T H E   O F F L I N E   V A L L E Y"),new(84,h*.25f+74),15,gold);
        DrawLine(new(85,h*.25f+101),new(445,h*.25f+101),new Color(gold,.65f));
        Text(T("Beyond the cities of machines,"),new(85,h*.25f+143),20,cream,true);
        Text(T("a small village still keeps its lights on."),new(85,h*.25f+174),20,cream,true);
        Text(T("PROLOGUE  01"),new(85,h*.25f+220),11,gold);
        Text(T("The train with no passengers"),new(85,h*.25f+248),19,cream);
        Text(T("MIO  /  HUMAN SIDE"),new(w-280,h-51),12,gold);
        Text(T("GODOT 4  ·  PLAYABLE PROLOGUE"),new(83,h-36),11,new Color(cream,.75f));
    }
    private void Vitals()
    {
        Panel(new(36,h-159,376,119),.91f);
        DrawTextureRectRegion(art,new Rect2(46,h-148,77,96),new Rect2(1040,140,350,435));
        DrawRect(new Rect2(46,h-148,77,96),new Color(gold,.5f),false);
        Text(T("Mio").ToUpperInvariant(),new(140,h-128),24,cream,true);
        Text(T("OFFLINE RUNNER"),new(Math.Max(210,150+serif.GetStringSize(T("Mio").ToUpperInvariant(),fontSize:24).X),h-129),10,muted);
        Bar(new(140,h-110),new(250,9),Game.Player.Health/100,jade);
        Text(F("VITALITY  {0} / 100",(int)Game.Player.Health),new(140,h-87),11,cream);
        Bar(new(140,h-72),new(250,5),Game.Player.Posture/100,gold);
        Text(T("POSTURE"),new(140,h-54),10,gold);
        Text(F("H   REMEDY  ×{0}",Game.Player.Remedies),new(274,h-54),10,cream);
        float sx=441;Panel(new(sx,h-124,236,84));
        DrawArc(new(sx+41,h-83),26,-Mathf.Pi/2,-Mathf.Pi/2+Mathf.Tau*(1-Game.Player.SkillCooldown/12),40,jade,2);
        Text("Q",new(sx+32,h-76),23,cream);
        Text(T("RESONANCE CUT"),new(sx+79,h-93),12,jade);
        Text(Game.Player.SkillCooldown>0?F("{0:0.0}s  /  RECHARGING",Game.Player.SkillCooldown):T("ZERO LINK  ·  READY"),new(sx+79,h-69),10,cream);
    }
    private void Controls()
    {
        float x=w-410;Panel(new(x,h-166,370,122),.75f);
        Text(T("WASD  Move      SHIFT  Run      SPACE  Jump"),new(x+18,h-139),13,cream);
        Text(T("LMB / J  Attack     RMB / K  Guard & deflect"),new(x+18,h-112),13,cream);
        Text(T("CTRL / ALT  Dodge     TAB  Lock     Q  Skill"),new(x+18,h-85),13,cream);
        Text(T("E  Interact    H  Remedy    ESC  Pause"),new(x+18,h-59),12,gold);
    }
    private void Minimap()
    {
        var center=new Vector2(w-130,131);float r=78;
        DrawCircle(center,r+4,new Color(ink,.93f));DrawArc(center,r,0,Mathf.Tau,64,new Color(gold,.7f),1);
        Text("N",center+new Vector2(-5,-90),11,gold);
        Vector2 Map(Vector3 p)=>new((p.X-Game.Player.GlobalPosition.X)*1.5f,(p.Z-Game.Player.GlobalPosition.Z)*1.5f);
        foreach(float riverZ in new[]{-8f,-111f})
        {
            float y=(riverZ-Game.Player.GlobalPosition.Z)*1.5f;
            if(Math.Abs(y)<r-3){float width=Mathf.Sqrt((r-3)*(r-3)-y*y);DrawLine(center+new Vector2(-width,y),center+new Vector2(width,y),new Color(.24f,.57f,.58f,.7f),5);}
        }
        for(float z=-6;z<56;z+=2)
        {var a=Map(new(World.BrookX(z),0,z));var b=Map(new(World.BrookX(z+2),0,z+2));if(a.Length()<r-3&&b.Length()<r-3)DrawLine(center+a,center+b,new Color(.24f,.57f,.58f,.7f),2);}
        for(float z=Game.Player.GlobalPosition.Z-52;z<Game.Player.GlobalPosition.Z+52;z+=2)
        {
            if(z<World.NorthEdge||z>World.SouthEdge-2)continue;
            var a=Map(new(World.RoadX(z),0,z));var b=Map(new(World.RoadX(z+2),0,z+2));
            if(a.Length()<r-2&&b.Length()<r-2)DrawLine(center+a,center+b,new Color(gold,.6f),2);
        }
        foreach(var b in Game.World.Buildings)
        {var p=Map(b.Position);if(p.Length()<r-6)DrawRect(new Rect2(center+p-new Vector2(4,3),new Vector2(8,6)),new("9a9b77"));}
        foreach(var n in Game.World.Residents)
        {var p=Map(n.Body.Position);if(p.Length()<r-3)DrawCircle(center+p,2,jade);}
        foreach(var e in Game.Enemies.Where(e=>!e.Dead))
        {var p=Map(e.GlobalPosition);if(p.Length()<r-4)DrawCircle(center+p,3,new("e58b66"));}
        DrawCircle(center,4,cream);DrawLine(center,center+new Vector2(-Mathf.Sin(Game.CameraYaw),-Mathf.Cos(Game.CameraYaw))*10,cream,2);
        if(Game.QuestStage<4)Diamond(center+Map(Game.ObjectivePosition).LimitLength(r-9),4,gold);
        Text(T("SENSORY LINK  •"),new(w-195,238),10,jade);
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
        string hint=T("E / ENTER   Next        ESC   Skip scene");float hintX=box.End.X-26-font.GetStringSize(hint,fontSize:12).X;
        Text(hint,new(hintX,box.End.Y-18),12,new Color(gold,.8f));
        if(Game.Reveal>=Game.Dialogue.Length&&(Time.GetTicksMsec()/450)%2==0)
        {var c=new Vector2(hintX-24,box.End.Y-23);DrawColoredPolygon(new[]{c+new Vector2(-6,-4),c+new Vector2(6,-4),c+new Vector2(0,4)},gold);}
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
        Text("0",new(c.X-serif.GetStringSize("0",fontSize:44).X/2,c.Y+15),44,new Color(cream,.5f+.5f*glow),true);
        bool voicing=speaking&&Game.Reveal<Game.Dialogue.Length;
        for(int i=0;i<9;i++)
        {
            float level=voicing?.35f+.65f*Mathf.Abs(Mathf.Sin(t*11+i*1.7f)*Mathf.Sin(t*4.3f+i)):.12f;
            DrawRect(new Rect2(c.X+72+i*9,c.Y-level*22,4,level*44),new Color(jade,.4f+.6f*glow));
        }
        Text(T("ZERO  ·  LINK"),new(c.X+72,c.Y+44),11,new Color(jade,.5f+.5f*glow));
    }
    private (string Text,int Size,string Language,List<(int Start,int End)> Lines) wrapped=("",0,"",new());
    // Line breaks come from the text server, which knows where CJK text may break; the ranges index into text.
    private List<(int Start,int End)> Wrap(string text,float width,int size)
    {
        if(wrapped.Text==text&&wrapped.Size==size&&wrapped.Language==Current)return wrapped.Lines;
        var paragraph=new TextParagraph {Width=width};paragraph.AddString(text,font,size,Current);
        var lines=Enumerable.Range(0,paragraph.GetLineCount()).Select(i=>{var r=paragraph.GetLineRange(i);return (r.X,r.Y);}).ToList();
        wrapped=(text,size,Current,lines);return lines;
    }
    public void RefreshMenu()
    {
        if(menu==null)return;
        foreach(var child in menu.GetChildren()){menu.RemoveChild(child);child.QueueFree();}
        float width=GetViewportRect().Size.X,height=GetViewportRect().Size.Y;
        scale=Math.Min(width/1440,height/900);w=width/scale;h=height/scale;
        if(Game.Mode==GameMode.Title)
        {
            Button("BEGIN JOURNEY     →",new(84,h*.25f+295),new(320,58),Game.Begin,true);
            Button("Quit",new(84,h*.25f+367),new(110,40),()=>GetTree().Quit(),false);
            LanguageButtons(214,h*.25f+367,104,40,8);
        }
        if(Game.Mode==GameMode.Paused)
        {
            Button("Resume journey",new(w/2-155,h*.40f),new(310,52),()=>Game.SetPaused(false),true);
            Button("Recover at the village",new(w/2-155,h*.40f+65),new(310,48),Game.Retry,false);
            Button("Restart prologue",new(w/2-155,h*.40f+126),new(310,48),Game.Restart,false);
            Button("Quit game",new(w/2-155,h*.40f+187),new(310,48),()=>GetTree().Quit(),false);
            LanguageButtons(w/2-155,h*.40f+250,98,40,8);
        }
        if(Game.Mode==GameMode.Defeated)
            Button("R   Return to Hoshikawa",new(w/2-160,h*.43f),new(320,55),Game.Retry,true);
    }
    // One button per language, each named in its own script; the current one is lit.
    private void LanguageButtons(float x,float y,float width,float height,float gap)
    {
        foreach(var (code,name) in Languages)
        {Button(name,new(x,y),new(width,height),()=>Game.SetLanguage(code),code==Current,NameFont(code),15);x+=width+gap;}
    }
    private void Button(string text,Vector2 pos,Vector2 size,Action action,bool primary,Font? face=null,int fontSize=17)
    {
        var b=new Button {Text=text,Position=pos,Size=size,MouseDefaultCursorShape=CursorShape.PointingHand};
        b.AddThemeFontSizeOverride("font_size",fontSize);if(face!=null)b.AddThemeFontOverride("font",face);
        foreach(string color in new[]{"font_color","font_hover_color","font_pressed_color","font_focus_color"})
            b.AddThemeColorOverride(color,primary?ink:cream);
        foreach(string state in new[]{"normal","hover","pressed","focus"})
            b.AddThemeStyleboxOverride(state,new StyleBoxFlat {BgColor=primary?(state=="hover"?cream:gold):new Color(ink,.9f),
                BorderColor=gold,BorderWidthLeft=1,BorderWidthRight=1,BorderWidthTop=1,BorderWidthBottom=1,
                ContentMarginLeft=16,ContentMarginRight=16,CornerRadiusTopLeft=2,CornerRadiusBottomRight=2});
        b.Pressed+=()=>action();menu.AddChild(b);if(primary)b.GrabFocus();
    }
}
