using Godot;
namespace Hoshikawa;

public partial class Hud : Control
{
    public Game Game=null!;
    private readonly Font font=ThemeDB.FallbackFont;
    private readonly SystemFont serif=new() {FontNames=new[]{"Georgia","Noto Serif","DejaVu Serif"}};
    private Texture2D art=null!;
    private GradientTexture2D titleShade=null!;
    private Control menu=null!;
    private float scale=1,w=1440,h=900;
    private Color ink=new("12282f"),cream=new("f1ead5"),muted=new("aec0b4"),gold=World.Gold,jade=World.Jade;
    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);MouseFilter=MouseFilterEnum.Ignore;
        art=GD.Load<Texture2D>("res://assets/art/mio-keyart.png");
        titleShade=new GradientTexture2D {Width=512,Height=1,FillFrom=Vector2.Zero,FillTo=Vector2.Right,
            Gradient=new Gradient {Colors=new[]{new Color(.028f,.068f,.076f,.86f),new Color(.028f,.068f,.076f,.30f),new Color(.028f,.068f,.076f,0)},Offsets=new[]{0f,.45f,1f}}};
        menu=new Control {MouseFilter=MouseFilterEnum.Ignore};AddChild(menu);Resized+=RefreshMenu;
    }
    public override void _ExitTree() { serif.Dispose(); titleShade.Dispose(); }
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
        scale=Math.Min(GetViewportRect().Size.X/1440,GetViewportRect().Size.Y/900);
        w=GetViewportRect().Size.X/scale;h=GetViewportRect().Size.Y/scale;
        DrawSetTransform(Vector2.Zero,0,Vector2.One*scale);menu.Scale=Vector2.One*scale;
        if(Game.Mode==GameMode.Title){Title();return;}
        Panel(new(30,24,385,160),.78f);
        Text(Game.Region.ToUpperInvariant(),new(42,52),25,cream,true);
        Text(Game.Region switch { "Komorebi"=>"NORTHERN SETTLEMENT  /  LODGE & CLINIC", "Cedar Pass"=>"CEDAR RIVER  /  ORCHARDS & HIGHLANDS",
            "Old Relay Road"=>"MANUAL RELAY  /  PATROL ROAD", _=>"THE OFFLINE VALLEY  /  HUMAN SETTLEMENT" },new(44,77),11,gold);
        DrawLine(new(44,93),new(350,93),new Color(gold,.55f));
        Text("01   THE TRAIN WITH NO PASSENGERS",new(44,120),12,gold);
        Text(Game.Objective,new(44,145),17,cream);
        if(Game.QuestStage<4)Text($"{(int)Game.Player.GlobalPosition.DistanceTo(Game.ObjectivePosition)} m",new(44,169),12,muted);
        Minimap();Vitals();Controls();
        Text("2189  /  SHIKOKU HIGHLANDS",new(w-280,h-26),11,new Color(cream,.65f));
        var closest=Game.Locked??Game.Enemies.Where(e=>!e.Dead&&e.GlobalPosition.DistanceTo(Game.Player.GlobalPosition)<12).OrderBy(e=>e.GlobalPosition.DistanceTo(Game.Player.GlobalPosition)).FirstOrDefault();
        if(closest!=null)
        {
            float x=w/2-170;Text("AMATERASU  /  PATROL "+closest.Index.ToString("00"),new(x,80),13,cream);
            Bar(new(x,94),new(340,7),closest.Health/100,new("d48568"));
            Bar(new(x,106),new(340,4),closest.Posture/100,gold);
            Text(closest.Broken?"POSTURE BROKEN · ATTACK TO FINISH":closest.Perilous&&closest.State=="PERILOUS"?"◆ PERILOUS SWEEP · DODGE / JUMP":Game.Locked==closest?"TARGET LOCKED":"TAB   LOCK TARGET",new(x,131),11,closest.Perilous?new("ff9c83"):gold);
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
            Center(Game.ToastTitle,208,23,gold,true);Center(Game.ToastBody,235,14,cream);
        }
        if(Game.FeedbackTime>0)Center(Game.FeedbackText,h*.46f,21,Game.FeedbackColor);
        if(Game.Mode==GameMode.Dialogue)Dialogue();
        if(Game.Mode==GameMode.Paused||Game.Mode==GameMode.Defeated)
        {
            DrawRect(new(0,0,w,h),new Color(.025f,.055f,.067f,.77f));
            Center(Game.Mode==GameMode.Paused?"A moment of quiet":"The light has not gone out",h*.28f,39,cream,true);
            Center(Game.Mode==GameMode.Paused?"HOSHIKAWA  /  JOURNEY PAUSED":"Recover in the village. Disabled patrols stay disabled.",h*.28f+37,14,muted);
        }
    }
    private void Title()
    {
        DrawTextureRect(art,new(0,0,w,h),false);
        DrawTextureRect(titleShade,new(0,0,w,h),false);
        DrawRect(new(0,h-88,w,88),new Color(.018f,.04f,.05f,.6f));
        Text("A STORY OF TWO WORLDS",new(83,h*.25f-38),13,gold);
        Text("Hoshikawa",new(78,h*.25f+35),70,cream,true);
        Text("T H E   O F F L I N E   V A L L E Y",new(84,h*.25f+74),15,gold);
        DrawLine(new(85,h*.25f+101),new(445,h*.25f+101),new Color(gold,.65f));
        Text("Beyond the cities of machines,",new(85,h*.25f+143),20,cream,true);
        Text("a small village still keeps its lights on.",new(85,h*.25f+174),20,cream,true);
        Text("PROLOGUE  01",new(85,h*.25f+220),11,gold);
        Text("The train with no passengers",new(85,h*.25f+248),19,cream);
        Text("MIO  /  HUMAN SIDE",new(w-280,h-51),12,gold);
        Text("GODOT 4  ·  PLAYABLE PROLOGUE",new(83,h-36),11,new Color(cream,.75f));
    }
    private void Vitals()
    {
        Panel(new(36,h-159,376,119),.91f);
        DrawTextureRectRegion(art,new Rect2(46,h-148,77,96),new Rect2(1040,140,350,435));
        DrawRect(new Rect2(46,h-148,77,96),new Color(gold,.5f),false);
        Text("MIO",new(140,h-128),24,cream,true);
        Text("OFFLINE RUNNER",new(210,h-129),10,muted);
        Bar(new(140,h-110),new(250,9),Game.Player.Health/100,jade);
        Text($"VITALITY  {(int)Game.Player.Health} / 100",new(140,h-87),11,cream);
        Bar(new(140,h-72),new(250,5),Game.Player.Posture/100,gold);
        Text("POSTURE",new(140,h-54),10,gold);
        Text($"H   REMEDY  ×{Game.Player.Remedies}",new(274,h-54),10,cream);
        float sx=441;Panel(new(sx,h-124,236,84));
        DrawArc(new(sx+41,h-83),26,-Mathf.Pi/2,-Mathf.Pi/2+Mathf.Tau*(1-Game.Player.SkillCooldown/12),40,jade,2);
        Text("Q",new(sx+32,h-76),23,cream);
        Text("RESONANCE CUT",new(sx+79,h-93),12,jade);
        Text(Game.Player.SkillCooldown>0?$"{Game.Player.SkillCooldown:0.0}s  /  RECHARGING":"ZERO LINK  ·  READY",new(sx+79,h-69),10,cream);
    }
    private void Controls()
    {
        float x=w-410;Panel(new(x,h-166,370,122),.75f);
        Text("WASD  Move      SHIFT  Run      SPACE  Jump",new(x+18,h-139),13,cream);
        Text("LMB / J  Attack     RMB / K  Guard & deflect",new(x+18,h-112),13,cream);
        Text("CTRL / ALT  Dodge     TAB  Lock     Q  Skill",new(x+18,h-85),13,cream);
        Text("E  Interact    H  Remedy    ESC  Pause",new(x+18,h-59),12,gold);
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
        Text("SENSORY LINK  •",new(w-195,238),10,jade);
    }
    private void Diamond(Vector2 p,float radius,Color c)
        =>DrawPolyline(new[]{p+Vector2.Up*radius,p+Vector2.Right*radius,p+Vector2.Down*radius,p+Vector2.Left*radius,p+Vector2.Up*radius},c,2);
    private void Dialogue()
    {
        DrawRect(new(0,0,w,h),new Color(.015f,.035f,.04f,.15f));
        var rect=new Rect2(w/2-510,h-327,1020,219);Panel(rect,.97f);
        DrawLine(rect.Position+new Vector2(0,0),rect.Position+new Vector2(1020,0),gold,2);
        Text(Game.Speaker,new(rect.Position.X+30,rect.Position.Y+42),30,gold,true);
        Text(Game.SpeakerRole,new(rect.Position.X+230,rect.Position.Y+38),11,jade);
        var lines=Wrap(Game.Dialogue,945,18);float y=rect.Position.Y+79;
        foreach(string line in lines){Text(line,new(rect.Position.X+31,y),18,cream);y+=28;}
        Text("E / ENTER   Continue",new(rect.End.X-210,rect.End.Y-19),12,gold);
    }
    private List<string> Wrap(string text,float width,int size)
    {
        List<string> lines=new();string line="";
        foreach(string word in text.Split(' '))
        {string next=line==""?word:line+" "+word;if(font.GetStringSize(next,fontSize:size).X>width){lines.Add(line);line=word;}else line=next;}
        if(line!="")lines.Add(line);return lines;
    }
    public void RefreshMenu()
    {
        if(menu==null)return;
        foreach(var child in menu.GetChildren()){menu.RemoveChild(child);child.QueueFree();}
        float width=GetViewportRect().Size.X,height=GetViewportRect().Size.Y;
        scale=Math.Min(width/1440,height/900);w=width/scale;h=height/scale;
        if(Game.Mode==GameMode.Title)
        {
            Button("BEGIN JOURNEY     →",new(84,h*.25f+295),new(320,58),Game.Start,true);
            Button("Quit",new(84,h*.25f+367),new(110,40),()=>GetTree().Quit(),false);
        }
        if(Game.Mode==GameMode.Paused)
        {
            Button("Resume journey",new(w/2-155,h*.40f),new(310,52),()=>Game.SetPaused(false),true);
            Button("Recover at the village",new(w/2-155,h*.40f+65),new(310,48),Game.Retry,false);
            Button("Restart prologue",new(w/2-155,h*.40f+126),new(310,48),Game.Restart,false);
            Button("Quit game",new(w/2-155,h*.40f+187),new(310,48),()=>GetTree().Quit(),false);
        }
        if(Game.Mode==GameMode.Defeated)
            Button("R   Return to Hoshikawa",new(w/2-160,h*.43f),new(320,55),Game.Retry,true);
    }
    private void Button(string text,Vector2 pos,Vector2 size,Action action,bool primary)
    {
        var b=new Button {Text=text,Position=pos,Size=size,MouseDefaultCursorShape=CursorShape.PointingHand};
        b.AddThemeFontSizeOverride("font_size",17);
        foreach(string color in new[]{"font_color","font_hover_color","font_pressed_color","font_focus_color"})
            b.AddThemeColorOverride(color,primary?ink:cream);
        foreach(string state in new[]{"normal","hover","pressed","focus"})
            b.AddThemeStyleboxOverride(state,new StyleBoxFlat {BgColor=primary?(state=="hover"?cream:gold):new Color(ink,.9f),
                BorderColor=gold,BorderWidthLeft=1,BorderWidthRight=1,BorderWidthTop=1,BorderWidthBottom=1,
                ContentMarginLeft=16,ContentMarginRight=16,CornerRadiusTopLeft=2,CornerRadiusBottomRight=2});
        b.Pressed+=()=>action();menu.AddChild(b);if(primary)b.GrabFocus();
    }
}
