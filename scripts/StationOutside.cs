using Godot;
namespace Hoshikawa;

// Hoshikawa Station's outside map, east of Tunnel Mountain: the Station tunnel's east portal and its shelf on the hillside, the lineman's
// path down to the forecourt, the old line along the track bed, the station building's shell and platforms, and the freight yard behind
// its fences. One owner holds the 1 m ground's rect, the trails Height() cuts into the mountain, the footprints that are both the
// colliders and the tests' blocked predicate, the two portal doors and the Forecourt Starwell. The layout is v9's: station x east, z
// north, world x = 430 + x and world z = -120 - z, so the drawing's local (-53, 29.5) is (377, -149.5).
public partial class World
{
    public const string StationName="Hoshikawa Station",StationToast="HOSHIKAWA STATION";
    // West of this the station's region is Tunnel Mountain, where InMaze has always claimed it; the 4 m strip 368..372 is MazeFine's.
    public const int StationWest=366;
    // The 1 m ground over the station, its edges on the 4 m grid; Fine and MazeFine are unchanged, and the three are one cached table
    // so IsFine, the terrain chunks and the far grid's rim never disagree.
    public static readonly Rect2I StationFine=new(372,-200,264,180);
    private static Rect2I[]? fineAreas;
    public static Rect2I[] FineAreas=>fineAreas??=new[]{Fine,MazeFine,StationFine};
    public static bool InStation(float x,float z)=>x>=StationWest&&x<=StationFine.End.X&&z>=StationFine.Position.Y&&z<StationFine.End.Y;
    // The anchors named by the approved drawings, in world (x, z).
    public static readonly Vector2 StationPortal=new(377,-149.5f),Forecourt=new(508,-70),ParcelsDoor=new(558,-130),
        CraneCell=new(593.5f,-138.75f),CraneRampFoot=new(594,-126);
    // The gantry's cell stands this far over the yard; its ramp climbs from the foot to the bridge in 11.5 m, under 40 degrees.
    public const float CraneRise=6;
    // S1 is the 4 m aperture x 560..564 in the yard's south fence at z -108; S5 is the 5.2 m old-line gate at x 421, z -164.5.
    public static readonly (Vector2 At,float Span) S1Gate=(new(562,-108),4),S5Gate=(new(421,-164.5f),5.2f);
    public const string S1Name="Yard gate S1",S5Name="Old line gate S5";
    public enum DoorKind{Portal,Key}
    // A stable name, the world interaction point, and for a portal the landing on the other side with the heading Mio arrives in.
    // Heights are resolved at use and build time, never in this table.
    public record Door(string Name,DoorKind Kind,Vector2 At,Vector2? Landing,float Yaw,float CameraYaw,string Label,string Icon,string? Key=null);
    public static readonly Door[] Doors={
        // From Tunnel Mountain's Signal yard the tunnel brings her out on the hillside above the station, facing east down the path.
        new("Station tunnel (Tunnel Mountain)",DoorKind.Portal,new(134,-176),new(381.5f,-149.5f),Mathf.Pi/2,-Mathf.Pi/2,"STATION TUNNEL",""),
        // And from the station's shelf it brings her back to the yard's mouth, facing west into Tunnel Mountain.
        new("Station tunnel (Station)",DoorKind.Portal,new(379,-149.5f),new(131,-176),-Mathf.Pi/2,Mathf.Pi/2,"STATION TUNNEL",""),
    };
    public static Vector3 DoorPoint(Door door)=>new(door.At.X,Height(door.At.X,door.At.Y),door.At.Y);
    public static Vector3 DoorLanding(Door door){var p=door.Landing??door.At;return new(p.X,Height(p.X,p.Y)+.1f,p.Y);}
    // The station's solid footprints, the one table its colliders and its tests share. A Gate row is S1, present only while it is shut.
    public record Obstacle(string Name,Vector2 At,Vector2 Size,float High,bool Gate=false);
    public static readonly Obstacle[] StationObstacles={
        new("Station building",new(494,-120),new(128,72),8),
        new("Shop row north west",new(437,-41),new(2,6),4.5f),new("Shop row north",new(461,-41),new(34,6),4.5f),new("Shop row north east",new(508,-41),new(52,6),4.5f),
        new("Shop row south",new(485,-24.5f),new(98,9),4.5f),
        new("Mall stair",new(524.5f,-80),new(5,4),3),new("Clock tower",new(478,-63),new(5,5),9),new("Bus shelter",new(450,-49.5f),new(12,3),3),
        new("Garage north wall",new(555,-46),new(26,.8f),4),new("Garage south wall",new(555,-26),new(26,.8f),4),
        new("Garage west wall",new(542,-30),new(.8f,8),4),new("Garage west wall south",new(542,-44),new(.8f,4),4),
        new("Garage east wall",new(568,-29),new(.8f,6),4),new("Garage east wall north",new(568,-43),new(.8f,6),4),
        new("Depot wall",new(555,-24),new(38,2),2.5f),
        new("Dead bus 1",new(544,-65.5f),new(12,3),3),new("Dead bus 2",new(558,-67.5f),new(12,3),3),new("Dead bus 3",new(546,-60),new(12,3),3),
        new("Dead bus 4",new(560,-59.5f),new(12,3),3),new("Dead bus 5",new(544,-54.5f),new(12,3),3),new("Dead bus 6",new(558,-53),new(12,3),3),
        new("Lineman's hut",new(583,-64),new(14,8),4),new("Warehouse 1",new(580,-115),new(16,10),6),
        new("Warehouse 2 north wall",new(611,-122),new(22,.8f),6),new("Warehouse 2 south wall",new(611,-110),new(22,.8f),6),
        new("Warehouse 2 east wall",new(622,-116),new(.8f,12),6),new("Warehouse 2 west wall south",new(600,-112),new(.8f,4),6),new("Warehouse 2 west wall north",new(600,-120),new(.8f,4),6),
        new("Signal room north wall",new(618,-159),new(12,.8f),4),new("Signal room west wall",new(612,-155),new(.8f,8),4),new("Signal room east wall",new(624,-155),new(.8f,8),4),
        new("Signal room south wall west",new(614,-151),new(4,.8f),4),new("Signal room south wall east",new(622,-151),new(4,.8f),4),
        new("B2 ramp",new(575,-156.5f),new(18,5),2.5f),new("Yard north-west strip",new(561,-157.3f),new(6,6.6f),2.2f),
        new("Yard north fence",new(599,-160.6f),new(70,.8f),1.7f),new("Yard east fence",new(628,-134.3f),new(.8f,52.6f),1.7f),
        new("Yard south fence west",new(559,-108),new(2,.8f),1.7f),new("Yard south fence",new(592.5f,-108),new(57,.8f),1.7f),new("Yard south fence east",new(627,-108),new(2,.8f),1.7f),
        new(S1Name,new(562,-108),new(4,.8f),1.7f,true),
        new(S5Name,new(421,-164.5f),new(.8f,5.2f),2.6f),
        new("Signal cabin",new(433,-177),new(10,10),5),new("Hump",new(624,-138.75f),new(6,3.5f),1.2f),
    };
    // Whether a 1 m cell at (x, z) lies in a solid footprint. The square overlap, not its centre, so a fence thinner than a cell still
    // bars the cell it stands on; S1 is skipped when the test opens the gate.
    public static bool StationBlocked(float x,float z,bool s1Open=false)
    {
        foreach(var o in StationObstacles)
        {
            if(o.Gate&&s1Open)continue;
            if(x+.5f>o.At.X-o.Size.X/2&&x-.5f<o.At.X+o.Size.X/2&&z+.5f>o.At.Y-o.Size.Y/2&&z-.5f<o.At.Y+o.Size.Y/2)return true;
        }
        return false;
    }
    public static bool StationBlocked(string name,float x,float z)
    {
        var o=StationObstacles.Single(o=>o.Name==name);
        return x+.5f>o.At.X-o.Size.X/2&&x-.5f<o.At.X+o.Size.X/2&&z+.5f>o.At.Y-o.Size.Y/2&&z-.5f<o.At.Y+o.Size.Y/2;
    }
    // The station's trails, world (x, level, z), the half width of their level floors and of their road paint: the portal shelf, the
    // old line and the lineman's path down the hillside; the forecourt, its way and the arcade; the depot, garage and its drive; the
    // service road to S1; the open ground round the yard fence; the yard, its loading dock, the station shell, its platforms and track bed.
    public static readonly (string Name,float Half,float Paint,Vector3[] Points)[] StationTrails={
        ("Station portal",6,0,new Vector3[]{new(377,20,-149.5f),new(380,20,-149.5f)}),
        ("Old line",2.2f,1.1f,new Vector3[]{new(377,20,-149.5f),new(386,18.4f,-151.5f),new(394,16.6f,-154),new(406,13.4f,-159),new(416,10.4f,-162.5f),new(421,9.2f,-164.5f),new(430,6.3f,-164.5f),new(562,6.3f,-164.5f)}),
        ("Lineman's path",1.6f,.8f,new Vector3[]{new(377,20,-149.5f),new(384,19.2f,-146),new(386,17.8f,-138),new(376,16.5f,-132),new(390,15,-125),new(382,13.6f,-116),new(392,12.2f,-104),new(386,10.8f,-92),new(400,9.2f,-80),new(418,7.8f,-72),new(438,6,-68)}),
        ("Forecourt north",9,0,new Vector3[]{new(447,6,-53),new(527,6,-53)}),("Forecourt",9,0,new Vector3[]{new(447,6,-64),new(527,6,-64)}),
        ("Forecourt south",9,0,new Vector3[]{new(447,6,-75),new(527,6,-75)}),
        ("Forecourt way",6,0,new Vector3[]{new(508,6,-70),new(508,6,-58),new(522,6,-50),new(532,6,-46),new(539,6,-42),new(544,6,-38)}),
        ("Arcade",4.5f,0,new Vector3[]{new(443,6,-33.5f),new(531,6,-33.5f)}),
        ("Arcade gap west",3,0,new Vector3[]{new(441,6,-44),new(441,6,-38)}),("Arcade gap east",3,0,new Vector3[]{new(480,6,-44),new(480,6,-38)}),
        ("Bus depot north",12,0,new Vector3[]{new(548,6,-58),new(562,6,-58)}),("Bus depot",12,0,new Vector3[]{new(548,6,-34),new(562,6,-34)}),
        ("Service road",5,1.1f,new Vector3[]{new(536,6,-70),new(548,6,-76),new(562,6,-76),new(562,6,-108)}),
        ("Depot road",4,0,new Vector3[]{new(568,6,-36),new(582,6,-46),new(598,6,-64),new(612,6,-82),new(620,6,-96),new(624,6,-108)}),
        ("Freight yard south",10,0,new Vector3[]{new(568,6,-114),new(618,6,-114)}),("Freight yard",10,0,new Vector3[]{new(568,6,-132),new(618,6,-132)}),
        ("Freight yard north",10,0,new Vector3[]{new(568,6,-150),new(618,6,-150)}),("Freight yard east",4,0,new Vector3[]{new(624,6,-110),new(624,6,-158)}),
        ("Loading dock",3,0,new Vector3[]{new(561,6.25f,-124),new(561,6.25f,-154)}),
        ("Station shell south",13,0,new Vector3[]{new(443,6.5f,-97),new(545,6.5f,-97)}),("Station shell",13,0,new Vector3[]{new(443,6.5f,-121),new(545,6.5f,-121)}),
        ("Station shell north",13,0,new Vector3[]{new(443,6.5f,-145),new(545,6.5f,-145)}),
        ("Platform 1",3,0,new Vector3[]{new(428,7.4f,-159),new(560,7.4f,-159)}),("Platform 2",5,0,new Vector3[]{new(442,7.4f,-177),new(550,7.4f,-177)}),
        ("City line north",2.2f,1.1f,new Vector3[]{new(430,6.3f,-169.5f),new(562,6.3f,-169.5f)}),("City line south",2.2f,1.1f,new Vector3[]{new(430,6.3f,-184.5f),new(562,6.3f,-184.5f)}),
    };
    // The maze's trails and the station's share the carving rule and Tunnel Mountain's robot reach.
    public static IEnumerable<(string Name,float Half,float Paint,Vector3[] Points)> SiteTrails=>MazeTrails.Concat(StationTrails);
    private void StationOutside()
    {
        var r=new RandomNumberGenerator{Seed=8103};
        Vector3 Ground(float x,float z)=>new(x,Height(x,z),z);
        void Pad(string name,Rect2 rect,Color color)=>Mesh(this,name,new PlaneMesh{Size=rect.Size},new(rect.GetCenter().X,Height(rect.GetCenter().X,rect.GetCenter().Y)+.03f,rect.GetCenter().Y),Mat(color));
        void Roof(string name,float x0,float x1,float z0,float z1,float y,float high=1,Material? m=null)
        {
            var n=Box(this,name,new((x0+x1)/2,y+high/2,(z0+z1)/2),new(x1-x0,high,z1-z0),m??Mat(new("57584f")));
            CameraStop(n,new BoxShape3D{Size=new(x1-x0,high,z1-z0)});
        }
        // The paving, gravel and platforms: the terrain under each is the flat trail's, so one plane carries the surface's colour.
        Pad("Forecourt paving",new Rect2(438,-82,98,36),new("b3a88d"));
        Pad("Arcade paving",new Rect2(438,-38,98,9),new("a2947a"));
        Pad("Depot paving",new Rect2(536,-70,38,48),new("968f7d"));
        Pad("Freight yard gravel",new Rect2(558,-160,70,52),new("8a8377"));
        Pad("Loading dock paving",new Rect2(558,-154,6,30),new("aca08a"));
        Pad("Track bed ballast",new Rect2(426,-168,136,8),new("6d685e"));
        Box(this,"Platform 1 slab",new(494,7.25f,-159),new(136,.3f,6),stone);
        Box(this,"Platform 2 slab",new(496,7.25f,-177),new(112,.3f,10),stone);
        foreach(var (z,level) in new[]{(-164.5f,6.3f),(-169.5f,6.3f),(-184.5f,6.3f)})
            foreach(float side in new[]{-.72f,.72f})Box(this,"Running rail",new(496,level+.16f,z+side),new(136,.1f,.07f),Mat(new("6f6a62")));
        // The old local train, fenced in on track 1 beyond S5: a backdrop, not walked.
        Box(this,"Old local train",new(481,8.05f,-164.2f),new(98,3.3f,3.4f),Mat(new("8e9aa0")));
        foreach(float x in new[]{436,449,462,475,488,501,514,527})Box(this,"Train window",new(x,8.6f,-162.45f),new(8,.9f,.12f),Mat(new("354046")));
        Box(this,"Train skirt",new(481,6.7f,-164.2f),new(98,.5f,3.5f),Mat(new("3e4649")));
        // The station's shell: a solid footprint under four walls and a roof, its main doors barred from inside and the parcels door
        // on the yard's side, both shut this chapter.
        var shell=StationObstacles.Single(o=>o.Name=="Station building");float y=Height(shell.At.X,shell.At.Y);
        float x0=shell.At.X-shell.Size.X/2,x1=shell.At.X+shell.Size.X/2,z0=shell.At.Y-shell.Size.Y/2,z1=shell.At.Y+shell.Size.Y/2;
        var wall=Mat(new("c6beaa"));var dark=Mat(new("34383a"));
        Box(this,"Station south wall west",new((x0+488)/2,y+3.5f,z1),new(488-x0,7,1),wall);
        Box(this,"Station south wall east",new((504+x1)/2,y+3.5f,z1),new(x1-504,7,1),wall);
        Box(this,"Station north wall",new(shell.At.X,y+3.5f,z0),new(shell.Size.X,7,1),wall);
        Box(this,"Station west wall",new(x0,y+3.5f,(z0+z1)/2),new(1,7,shell.Size.Y),wall);
        Box(this,"Station east wall",new(x1,y+3.5f,(z0+z1)/2),new(1,7,shell.Size.Y),wall);
        Roof("Station roof",x0-1,x1+1,z0-1,z1+1,y+7,1.4f);
        Box(this,"Station main doors",new(496,y+2.2f,z1+.15f),new(16,4.4f,.3f),dark);
        Box(this,"Station door bar",new(496,y+2.4f,z1+.35f),new(15.4f,.2f,.2f),Mat(new("7d7568")));
        Roof("Station entrance canopy",x0+16,x1-16,z1-1,z1+5,y+4.6f,.25f,Mat(new("5d5f58")));
        var sign=Label(this,"HOSHIKAWA STATION",new(496,y+5.6f,z1+.4f),Gold,40);sign.Billboard=BaseMaterial3D.BillboardModeEnum.Disabled;
        Box(this,"Parcels door",new(x1+.15f,Height(x1+.6f,-130)+1.7f,-130),new(.3f,3.2f,3),dark);
        // The forecourt's clock tower and shelter, the shops and the arcade's canopy.
        var clock=StationObstacles.Single(o=>o.Name=="Clock tower");float clockY=Height(clock.At.X,clock.At.Y);
        Box(this,"Clock tower shaft",new(clock.At.X,clockY+4,clock.At.Y),new(5,8,5),wall);
        Roof("Clock tower cap",clock.At.X-3,clock.At.X+3,clock.At.Y-3,clock.At.Y+3,clockY+8,.8f);
        foreach(float side in new[]{-2.55f,2.55f})Cylinder(this,"Clock face",new(clock.At.X,clockY+6.6f,clock.At.Y+side),1.4f,.2f,Mat(new("efe7cf"))).Rotation=new(Mathf.Pi/2,0,0);
        foreach(var row in StationObstacles.Where(o=>o.Name.StartsWith("Shop row")))
        {
            float gy=Height(row.At.X,row.At.Y);
            Roof(row.Name+" roof",row.At.X-row.Size.X/2,row.At.X+row.Size.X/2,row.At.Y-row.Size.Y/2,row.At.Y+row.Size.Y/2,gy+4.5f,.5f);
        }
        Roof("Arcade canopy",438,536,-38.2f,-28.8f,Height(487,-33.5f)+3.6f,.25f,Mat(new("6b6353")));
        foreach(float x in new[]{441,459,477,495,513,531})foreach(float z in new[]{-37.6f,-29.4f})
            Box(this,"Arcade post",new(x,Height(x,z)+1.8f,z),new(.18f,3.6f,.18f),stone);
        // The depot's garage: its walls are their own footprints, open west and east for the through way, under a roof.
        Roof("Garage roof",542,568,-46,-26,Height(555,-36)+4,.6f);
        // The lineman's hut, the warehouses and the signal room, each a roof on walls that keep the route's openings.
        var hut=StationObstacles.Single(o=>o.Name=="Lineman's hut");Roof("Lineman's hut roof",hut.At.X-7,hut.At.X+7,hut.At.Y-4,hut.At.Y+4,Height(hut.At.X,hut.At.Y)+4,.7f);
        Roof("Warehouse 2 roof",600,622,-122,-110,Height(611,-116)+6,.8f);
        Roof("Signal room roof",612,624,-159,-151,Height(618,-155)+4,.7f);
        // The dead buses, each with its window band.
        foreach(var bus in StationObstacles.Where(o=>o.Name.StartsWith("Dead bus")))
        {
            float by=Height(bus.At.X,bus.At.Y);
            Box(this,"Dead bus window",new(bus.At.X,by+1.9f,bus.At.Y),new(bus.Size.X*.8f,1,bus.Size.Y+.06f),Mat(new("3a4145")));
        }
        // The freight yard's sidings, converging on the hump; their rails lie on the ground, not in its footprints.
        foreach(float z in new[]{-128f,-135f,-142f,-149f})
            foreach(float side in new[]{-.53f,.53f})Box(this,"Siding rail",new(591,6.16f,z+side),new(62,.08f,.07f),Mat(new("6f6a62")));
        Box(this,"Hump rail",new(630,6.3f,-141.5f),new(18,.1f,.07f),Mat(new("6f6a62")));
        // The gantry over the sidings: two legs, the bridge deck with its crane cell, and the ramp up from the yard's floor, 11.5 m for
        // its 6 m rise (27.5 degrees). The deck and ramp are bodies of their own, so the 2D flood only knows the ramp's foot.
        var steel=Mat(new("7f8983"));float deck=Height(593.5f,-138.75f)+CraneRise;
        foreach(float x in new[]{590,597})Box(this,"Gantry leg",new(x,Height(x,-138.5f)+3,-138.5f),new(.7f,6,.7f),steel,true);
        var bridge=new StaticBody3D{Name="Gantry bridge",Position=new(593.5f,deck-.1f,-138.75f),CollisionMask=0};AddChild(bridge);
        bridge.AddChild(new CollisionShape3D{Shape=new BoxShape3D{Size=new(7,.2f,4)}});
        bridge.AddChild(new MeshInstance3D{Mesh=new BoxMesh{Size=new(7,.2f,4)},MaterialOverride=steel});
        foreach(float z in new[]{-140.6f,-136.9f})Box(bridge,"Gantry parapet",new(0,.55f,z),new(7,.9f,.12f),steel);
        foreach(float x in new[]{-3.4f,3.4f})Box(bridge,"Gantry end rail",new(x,.55f,0),new(.12f,.9f,4),steel);
        var ramp=new StaticBody3D{Name="Gantry ramp",Position=new(594,Height(594,-126)+CraneRise/2,-131.25f),CollisionMask=0};AddChild(ramp);
        ramp.AddChild(new CollisionShape3D{Shape=new BoxShape3D{Size=new(2.4f,.18f,11.5f)}});
        var rampMesh=new MeshInstance3D{Mesh=new BoxMesh{Size=new(2.4f,.18f,11.5f)},MaterialOverride=steel};ramp.AddChild(rampMesh);
        ramp.Basis=new Basis(Vector3.Right,Mathf.Atan2(CraneRise,11.5f));
        // The tunnel's east portal in the hillside, its label, and the same label over the Signal yard's mouth.
        Piece("Station tunnel portal","tunnel_portal",new(377-3.93f,Height(377,-149.5f),-149.5f),90);
        Label(this,"STATION TUNNEL",new(376,Height(377,-149.5f)+3.4f,-149.5f),Gold,27);
        Label(this,"STATION TUNNEL",new(133,Height(134,-176)+2.6f,-176),Gold,27);
        // The Forecourt Starwell, its well lit and rested at as the villages' are.
        Well("Forecourt well",Forecourt);
        // Every footprint is a collider of its own, and the one its test blocks; the two gate leaves stay unmerged for the chapter that
        // makes them open. The station's shell is drawn above, so it only needs its body here.
        foreach(var o in StationObstacles)
        {
            float gy=Height(o.At.X,o.At.Y);
            var body=new StaticBody3D{Name="Station footprint "+o.Name,Position=new(o.At.X,gy+o.High/2,o.At.Y),CollisionMask=0};AddChild(body);
            body.AddChild(new CollisionShape3D{Shape=new BoxShape3D{Size=new(o.Size.X,o.High,o.Size.Y)}});
            if(o.Name is S1Name or S5Name)body.AddChild(new MeshInstance3D{Mesh=new BoxMesh{Size=new(o.Size.X,o.High,o.Size.Y)},MaterialOverride=Mat(o.Name==S1Name?new("4b3a2c"):new("5c4a36"))});
            else if(o.Name!="Station building"&&o.Name!="Clock tower")Box(this,"Station "+o.Name,new(o.At.X,gy+o.High/2,o.At.Y),new(o.Size.X,o.High,o.Size.Y),
                o.Name.Contains("fence")||o.Name.Contains("strip")?Mat(new("6b625a")):o.Name.StartsWith("Dead bus")?Mat(new("7d5a44")):o.Name.Contains("wall")?Mat(new("b9b19d")):stone);
        }
        // The station's own scatter, on the hillsides beyond its floors and footprints, from its own seeded stream.
        for(int i=0;i<160;i++)
        {
            float x=r.RandfRange(374,632),z=r.RandfRange(-198,-22),s=r.RandfRange(.7f,1.3f);int kind=i%3;
            if(!StationBlocked(x,z)&&MazeEdge(x,z)>3.5f&&MazeEdge(x,z)<14&&RoadGap(x,z)>3.5f)Tree(Ground(x,z),s,kind==0?"sugi":"countryside",r);
            else for(int k=0;k<3;k++)r.Randf();
        }
        for(int i=0;i<90;i++)
        {
            float x=r.RandfRange(374,632),z=r.RandfRange(-198,-22),s=r.RandfRange(.4f,1.3f),turn=r.Randf()*Mathf.Tau;
            if(!StationBlocked(x,z)&&MazeEdge(x,z)>1.2f&&MazeEdge(x,z)<4.5f)Boulder("Station boulder",x,z,s,turn,false);
        }
    }
}
