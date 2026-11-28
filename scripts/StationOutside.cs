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
    // The freight warehouses stand on their drawn southern loading bank, 1.35 m over the yard, its south fence the yard's. A trail cannot
    // raise it: the yard's deeper floor wins Carve()'s blend over the same footprint. This named local Height() feature is that bank alone,
    // a rectangle x 564..622, z -124..-108 rising from the 6 m yard to 7.35 over 60 cm, except Warehouse 2's north-door stair cut; the
    // bank's walls and the yard's own fences stand on it.
    public static float StationBank(float x,float z)
    {
        // Warehouse 2's stair cut into the bank's north face: a 2 m ramp (34 degrees) from the yard up to its north door, between the ends
        // of the bank's north wall; it is the one way onto the bank, so the warehouse's sleeping post stays reachable from the yard.
        if(x>=609&&x<=613&&z>=-124&&z<=-122)return Mathf.Lerp(6,7.35f,(z+124)/2);
        float d=Math.Min(Math.Min(x-564,622-x),Math.Min(z+124,-108-z));if(d<0)return 0;
        return Mathf.Lerp(6,7.35f,Mathf.SmoothStep(0,.6f,d));
    }
    // The anchors named by the approved drawings, in world (x, z). The parcels key is where the v9 first walk enters the old signal room
    // (local 188, 33.5) and the arcade pouch on its side loop's shop row (local 98, -88.6).
    public static readonly Vector2 StationPortal=new(377,-149.5f),Forecourt=new(508,-70),ParcelsDoor=new(558,-130),
        CraneCell=new(593.5f,-138.75f),CraneRampFoot=new(594,-126),ParcelsKey=new(618,-153.5f),ArcadePouch=new(528,-31.4f),GarageCell=new(565,-30);
    // The gantry's cell stands this far over the yard; its ramp climbs from the foot to the bridge's south edge in 10.75 m, 29.6 degrees,
    // its top a tenth over the deck so she steps down onto it rather than into the deck's face.
    public const float CraneRise=6,CraneRampRun=10.75f,CraneRampRise=6.1f;
    // The bridge deck's own level, where the crane's cell stands.
    public static float CraneDeck=>Height(CraneCell.X,CraneCell.Y)+CraneRise;
    public static readonly Vector2 CraneRampTop=new(594,-136.75f);
    // The warehouse's yard-facing stair: from the yard at (611,-124) up 1.35 m over 2 m (34 degrees) to its north door at (611,-122),
    // the only way onto the bank and to the v9 sleeper inside Warehouse 2. The stairwell's side walls keep the bank's apron out of reach.
    public static readonly (Vector2 Bottom,Vector2 Top) WarehouseStair=(new(611,-124),new(611,-122));
    // S1 is the 4 m aperture x 560..564 in the yard's south fence at z -108; S5 is the 5.2 m old-line gate at x 421, z -164.5.
    public static readonly (Vector2 At,float Span) S1Gate=(new(562,-108),4),S5Gate=(new(421,-164.5f),5.2f);
    public const string S1Name="Yard gate S1",S5Name="Old line gate S5";
    public enum DoorKind{Portal,Key}
    // A stable name, the world interaction point, and for a portal the landing on the other side with the heading Mio arrives in.
    // Heights are resolved at use and build time, never in this table.
    // Lift raises the follow camera's rig for this arrival alone (0 is the normal camera); Pitch is the reveal's own vertical aim,
    // separate from the player's CameraPitch. Game holds them through the dark part of the existing blackout fade and blends them out
    // into the normal follow camera; the player's aim is preserved and never overwritten.
    public record Door(string Name,DoorKind Kind,Vector2 At,Vector2? Landing,float Yaw,float CameraYaw,string Label,string Icon,string? Key=null,float Lift=0f,float Pitch=-.29f);
    public static readonly Door[] Doors={
        // From Tunnel Mountain's Signal yard the tunnel brings her out on the hillside above the station, facing east down the path.
        // The east arrival alone lifts the camera 10.65 m for the station reveal at a level pitch, held through the blackout's dark part
        // and eased back into the player's own aim; the return row keeps the normal camera.
        new("Station tunnel (Tunnel Mountain)",DoorKind.Portal,new(134,-176),new(381.5f,-149.5f),Mathf.Pi/2,-Mathf.Pi/2,"STATION TUNNEL","",Lift:10.65f,Pitch:0f),
        // And from the station's shelf it brings her back to the yard's mouth, facing west into Tunnel Mountain.
        new("Station tunnel (Station)",DoorKind.Portal,new(379,-149.5f),new(131,-176),-Mathf.Pi/2,Mathf.Pi/2,"STATION TUNNEL",""),
        // The parcels door on the station's east wall: a Key door, so no landing and no teleport. Its key unlocks it once (Game.Opened
        // keeps that); the door itself stays shut, and the room behind it is HPA-1253's inside work, whose drawing marks this same door but
        // leaves the wall cutout open, so nothing here cuts the shell.
        new("Parcels door",DoorKind.Key,ParcelsDoor,null,0,0,"PARCELS DOOR","",Key:"parcels_key"),
    };
    // The parcels door's row, the one Key door: Game's objective marker and its unlock read it.
    public static Door ParcelsDoorRow=>Doors.Single(d=>d.Kind==DoorKind.Key);
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
        new("Warehouse 2 north wall west",new(604.5f,-122),new(9,.8f),6),new("Warehouse 2 north wall east",new(617.5f,-122),new(9,.8f),6),
        new("Warehouse 2 south wall",new(611,-110),new(22,.8f),6),
        new("Warehouse 2 east wall",new(622,-116),new(.8f,12),6),new("Warehouse 2 west wall",new(600,-116),new(.8f,12),6),
        new("Signal room north wall",new(618,-159),new(12,.8f),4),new("Signal room west wall",new(612,-155),new(.8f,8),4),new("Signal room east wall",new(624,-155),new(.8f,8),4),
        new("Signal room south wall west",new(614,-151),new(4,.8f),4),new("Signal room south wall east",new(622,-151),new(4,.8f),4),
        new("B2 ramp",new(575,-156.5f),new(18,5),2.5f),new("Yard north-west strip",new(561,-157.3f),new(6,6.6f),2.2f),
        new("Yard north fence",new(599,-160.6f),new(70,.8f),2.2f),new("Yard east fence",new(628,-134.3f),new(.8f,52.6f),2.2f),
        new("Service road fence",new(566,-89),new(.8f,38),2.2f),new("Loading dock wall",new(564.4f,-136.5f),new(.8f,25),2.2f),
        // The parcels bay's fence: a 19.5 m fence 1.9 m off the station wall encloses the parcels door's approach, so that door is entered
        // from S1's yard side, as the v9 graph's S1 to door edge has it, never straight off the dock's north end. Its south end stops 4 m
        // short of S1 so the mouth opens diagonally (the 21 m S1 to door leg stays inside its band); its return sits 2.5 m north of the
        // door, so no position outside it is within the door's 2.2 m reach. The dock's corridor (x 561..564) stays clear.
        new("Parcels bay fence",new(560.2f,-122.75f),new(.6f,19.5f),1.8f),new("Parcels bay return fence",new(559.1f,-132.5f),new(2.4f,.6f),1.8f),
        // The warehouses' loading bank is walled on its yard faces; Warehouse 2's north-door stair is its one opening, and the apron west of
        // it stays sealed, so the bank is no route round the yard.
        new("Warehouse bank west wall",new(564,-116),new(.8f,16),1.5f),new("Warehouse bank north wall west",new(586.5f,-124),new(45,.8f),1.5f),new("Warehouse bank north wall east",new(617.5f,-124),new(9,.8f),1.5f),new("Warehouse bank east wall",new(622,-116),new(.8f,16),1.5f),
        new("Warehouse stair side west",new(609,-123),new(.8f,2),2.2f),new("Warehouse stair side east",new(613,-123),new(.8f,2),2.2f),
        new("Yard south fence west",new(559,-108),new(2,.8f),2.2f),new("Yard south fence",new(592.5f,-108),new(57,.8f),2.2f),new("Yard south fence east",new(627,-108),new(2,.8f),2.2f),
        // Its own 4 m span and fence line are S1Gate's, so the flood's footprint and the live leaf cannot drift apart.
        new(S1Name,S1Gate.At,new(S1Gate.Span,.8f),1.7f,true),
        new(S5Name,new(421,-164.5f),new(.8f,5.2f),2.6f),
        new("Signal cabin",new(433,-177),new(10,10),5),new("Hump",new(624,-138.75f),new(6,3.5f),1.2f),
        // The six stationary wagons on the drawn sidings, each 11 m by 3.2 m; the later hump trap is not built here.
        new("Wagon 1",new(577.5f,-128),new(11,3.2f),3.2f),new("Wagon 2",new(601.5f,-128),new(11,3.2f),3.2f),
        new("Wagon 3",new(579.5f,-142),new(11,3.2f),3.2f),new("Wagon 4",new(603.5f,-142),new(11,3.2f),3.2f),
        new("Wagon 5",new(583.5f,-149),new(11,3.2f),3.2f),new("Wagon 6",new(604.5f,-149),new(11,3.2f),3.2f),
        new("Gantry leg west",new(590,-138.5f),new(.7f,.7f),6),new("Gantry leg east",new(597,-138.5f),new(.7f,.7f),6),
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
    // The lowest and highest ground under a footprint's corners and centre: a solid's collider is seated on the lowest so no gap opens
    // beneath it and rises to the highest plus its own height, so a fence on a bank stays above a jump from either side.
    public static (float Low,float High) StationFootprint(Obstacle o)
    {
        float low=float.MaxValue,high=float.MinValue;
        for(int i=-1;i<=1;i++)for(int j=-1;j<=1;j++){float h=Height(o.At.X+i*o.Size.X/2,o.At.Y+j*o.Size.Y/2);low=Math.Min(low,h);high=Math.Max(high,h);}
        return (low,high);
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
        ("Freight yard south",10,0,new Vector3[]{new(568,6,-118),new(618,6,-118)}),("Freight yard",10,0,new Vector3[]{new(568,6,-132),new(618,6,-132)}),
        ("Freight yard north",10,0,new Vector3[]{new(568,6,-150),new(618,6,-150)}),("Freight yard east",4,0,new Vector3[]{new(624,6,-110),new(624,6,-158)}),
        ("Loading dock",3,0,new Vector3[]{new(561,6.25f,-124),new(561,6.25f,-154)}),
        ("Station shell south",13,0,new Vector3[]{new(443,6.5f,-97),new(545,6.5f,-97)}),("Station shell",13,0,new Vector3[]{new(443,6.5f,-121),new(545,6.5f,-121)}),
        ("Station shell north",13,0,new Vector3[]{new(443,6.5f,-145),new(545,6.5f,-145)}),
        ("Platform 1",3,0,new Vector3[]{new(428,7.4f,-159),new(560,7.4f,-159)}),("Platform 2",5,0,new Vector3[]{new(442,7.4f,-177),new(550,7.4f,-177)}),
        ("City line north",2.2f,1.1f,new Vector3[]{new(430,6.3f,-169.5f),new(562,6.3f,-169.5f)}),("City line south",2.2f,1.1f,new Vector3[]{new(430,6.3f,-184.5f),new(562,6.3f,-184.5f)}),
    };
    // The maze's trails and the station's share the carving rule and Tunnel Mountain's robot reach.
    public static IEnumerable<(string Name,float Half,float Paint,Vector3[] Points)> SiteTrails=>MazeTrails.Concat(StationTrails);
    // The four drawn sidings from x 570 to the hump at (624,-138.5), converging there, and the drawn continuation to (634,-147).
    public static readonly (string Name,Vector3[] Points)[] StationSidings={
        ("Siding 1",new Vector3[]{new(570,6,-128),new(612,6,-128),new(624,6,-138.5f)}),
        ("Siding 2",new Vector3[]{new(570,6,-135),new(612,6,-135),new(624,6,-138.5f)}),
        ("Siding 3",new Vector3[]{new(570,6,-142),new(612,6,-142),new(624,6,-138.5f)}),
        ("Siding 4",new Vector3[]{new(570,6,-149),new(612,6,-149),new(624,6,-138.5f)}),
        ("Hump line",new Vector3[]{new(624,6,-138.5f),new(634,6,-147)}),
    };
    // The station's own rolling loads, the same World.Parts rows and Traps.cs rules the maze's use: each parked at From, the head of its
    // run, until Mio comes within 2.4 m of To, its foot. v9 draws the wagon's run bent at local (182,15); the existing straight mechanic
    // keeps its approved head and foot, 44.14 m against the drawn 44.50, deviating up to 2.54 m from the drawn siding, so its foot is a
    // deliberate detour off the itinerary rather than a trap on the walk.
    public static readonly (string Name,string Kind,Vector2 From,Vector2 To)[] StationLoads={
        ("Station oil drums","barrels",new(386,-138),new(377,-132.5f)),("Hump wagon","wagon",new(624,-138.5f),new(580,-135))};
    // Hoshikawa Station's robots, in the same shape as the maze's and spawned by the same loop: three patrols, eight scouts (four dormant
    // until Mio comes near), six drones, at v9's posts. They carry Enemy.Maze too, so the day-one relay mission never counts them, but they
    // guard no lock-in yard: the station's rows have none, and Yard stays null. v9 gives the sleepers' posts and their sleeping state, not
    // the way each lies; the game needs an angle to lay a scout down, so that angle is the station's own.
    public static readonly (string Kind,Vector2 At,float? Sleeps)[] StationRobots={
        ("drone",new(384,-126),null),("scout",new(389,-90),null),("patrol",new(470,-54.8f),null),("drone",new(452,-64),null),("drone",new(526,-56),null),
        ("scout",new(462,-34),0),("scout",new(512,-34),0),("scout",new(590,-55),null),("scout",new(555,-37),null),("drone",new(608,-76),null),
        ("scout",new(619,-116),180),("patrol",new(582,-131.5f),null),("patrol",new(605,-145.5f),null),("scout",new(587,-146),-60),("scout",new(608,-131.5f),null),
        ("drone",new(595,-117),null),("drone",new(606,-156),null)};
    private void StationOutside()
    {
        var r=new RandomNumberGenerator{Seed=8103};
        Vector3 Ground(float x,float z)=>new(x,Height(x,z),z);
        void Pad(string name,Rect2 rect,Color color)=>Mesh(this,name,new PlaneMesh{Size=rect.Size},new(rect.GetCenter().X,Height(rect.GetCenter().X,rect.GetCenter().Y)+.03f,rect.GetCenter().Y),Mat(color));
        // A paving surface that follows Height() cell by cell, so a strip on sloped or stepped ground leaves no grass at its edges.
        MeshInstance3D Paving(string name,float x0,float x1,float z0,float z1,float step,Color color)
        {
            int nx=Mathf.CeilToInt((x1-x0)/step),nz=Mathf.CeilToInt((z1-z0)/step);
            var v=new Vector3[(nx+1)*(nz+1)];var n=new Vector3[v.Length];var uv=new Vector2[v.Length];var idx=new List<int>();
            for(int j=0;j<=nz;j++)for(int i=0;i<=nx;i++)
            {float x=Mathf.Lerp(x0,x1,i/(float)nx),z=Mathf.Lerp(z0,z1,j/(float)nz);int k=j*(nx+1)+i;v[k]=new(x,MeshHeight(x,z)+.10f,z);n[k]=Vector3.Up;uv[k]=new(x*.2f,z*.2f);}
            for(int j=0;j<nz;j++)for(int i=0;i<nx;i++){int a=j*(nx+1)+i,b=a+1,c=a+nx+1,d=c+1;idx.AddRange(new[]{a,b,d,a,d,c});}
            var arr=new Godot.Collections.Array();arr.Resize((int)Godot.Mesh.ArrayType.Max);
            arr[(int)Godot.Mesh.ArrayType.Vertex]=v;arr[(int)Godot.Mesh.ArrayType.Normal]=n;arr[(int)Godot.Mesh.ArrayType.TexUV]=uv;arr[(int)Godot.Mesh.ArrayType.Index]=idx.ToArray();
            var mesh=new ArrayMesh();mesh.AddSurfaceFromArrays(Godot.Mesh.PrimitiveType.Triangles,arr);
            return Mesh(this,name,mesh,Vector3.Zero,Mat(color,0,true));
        }
        void Roof(string name,float x0,float x1,float z0,float z1,float y,float high=1,Material? m=null)
        {
            var n=Box(this,name,new((x0+x1)/2,y+high/2,(z0+z1)/2),new(x1-x0,high,z1-z0),m??Mat(new("57584f")));
            CameraStop(n,new BoxShape3D{Size=new(x1-x0,high,z1-z0)});
        }
        // A track along `points`: sleepers at the ground and two rails built from shared 3D side points. Each waypoint's mitered across
        // offset is used by both segments meeting there, so each rail is continuous through the bends and across the track-bed join.
        void Track(string name,Vector3[] points)
        {
            var across=new Vector2[points.Length];
            for(int i=0;i<points.Length;i++)
            {
                var prev=i>0?new Vector2(points[i].X-points[i-1].X,points[i].Z-points[i-1].Z).Normalized():Vector2.Zero;
                var next=i<points.Length-1?new Vector2(points[i+1].X-points[i].X,points[i+1].Z-points[i].Z).Normalized():Vector2.Zero;
                var dir=prev+next;if(dir.LengthSquared()<1e-6f)dir=next+prev;if(dir.LengthSquared()<1e-6f)dir=Vector2.Right;dir=dir.Normalized();
                across[i]=new Vector2(-dir.Y,dir.X);
            }
            Vector3 Side(int i,float side){var p=points[i];return new Vector3(p.X+across[i].X*side*MainGauge,Height(p.X,p.Z)+.14f,p.Z+across[i].Y*side*MainGauge);}
            for(int i=0;i<points.Length-1;i++)
            {
                var a=points[i];var b=points[i+1];float length=new Vector2(b.X-a.X,b.Z-a.Z).Length(),yaw=Mathf.Atan2(b.X-a.X,b.Z-a.Z);
                for(float t=.4f;t<length;t+=.9f){var p=new Vector2(a.X,a.Z).Lerp(new(b.X,b.Z),t/length);Box(this,$"{name} sleeper {i} {(int)t}",Ground(p.X,p.Y)+Vector3.Up*.04f,new(MainGauge*3.8f,.12f,.24f),wood).Rotation=new(0,yaw,0);}
                foreach(float side in new[]{-1f,1f})
                {
                    var p0=Side(i,side);var p1=Side(i+1,side);var d=p1-p0;
                    var rail=Box(this,$"{name} rail {i} {side}",(p0+p1)/2,new(.07f,.1f,d.Length()),Mat(new("6f6a62")));
                    rail.Basis=Basis.LookingAt(d,Vector3.Up,true);
                }
            }
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
        int track=0;
        foreach(var (z,level) in new[]{(-169.5f,6.3f),(-184.5f,6.3f)})
        {
            track++;
            foreach(float side in new[]{-.72f,.72f})Box(this,$"Nagoya track {track} {(side<0?"north":"south")} rail",new(496,level+.16f,z+side),new(136,.1f,.07f),Mat(new("6f6a62")));
        }
        // The old local train, fenced in on track 1 beyond S5: a backdrop, not walked.
        Box(this,"Old local train",new(481,8.05f,-164.2f),new(98,3.3f,3.4f),Mat(new("8e9aa0")));
        foreach(float x in new[]{436,449,462,475,488,501,514,527})Box(this,"Train window",new(x,8.6f,-162.45f),new(8,.9f,.12f),Mat(new("354046")));
        Box(this,"Train skirt",new(481,6.7f,-164.2f),new(98,.5f,3.5f),Mat(new("3e4649")));
        // The old line's own rails and sleepers from the portal down its bends, through S5 and the track bed to its east end.
        Track("Old line",StationTrails.Single(t=>t.Name=="Old line").Points);
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
        Box(this,"Parcels door",new(x1+.65f,Height(x1+.6f,-130)+1.7f,-130),new(.3f,3.2f,3),dark);
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
        // The warehouse's yard-facing stair: a paved ramp on the bank's own cut, between the wall's ends and the stair's side walls.
        {
            var (bottom,top)=WarehouseStair;float low=Height(bottom.X,bottom.Y),high=Height(top.X,top.Y);float run=top.DistanceTo(bottom),rise=high-low;
            var warehouseRamp=new MeshInstance3D{Name="Warehouse stair",Mesh=new BoxMesh{Size=new(3,.12f,Mathf.Sqrt(run*run+rise*rise))},MaterialOverride=Mat(new("7f8983"))};
            warehouseRamp.Position=new((bottom.X+top.X)/2,(low+high)/2-.055f,(bottom.Y+top.Y)/2);warehouseRamp.Rotation=new(-Mathf.Atan2(rise,run),0,0);AddChild(warehouseRamp);
        }
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
        // The freight yard's four sidings, drawn from x 570 to the hump at (624,-138.5) and on to (634,-147), converging as v9 draws them.
        foreach(var siding in StationSidings)Track(siding.Name,siding.Points);
        // The station's two rolling loads, through the maze's own Load() and Traps.cs rules.
        foreach(var (n,k,from,to) in StationLoads)Load(n,k,from,to);
        // The gantry over the sidings: two legs, the bridge deck with its crane cell, and the ramp up from the yard's floor. The legs are
        // StationObstacles rows, so their colliders and the test's blocked cells are the same footprint; the deck and ramp are bodies of
        // their own, so the 2D flood only knows the ramp's foot.
        var steel=Mat(new("7f8983"));float deck=Height(593.5f,-138.75f)+CraneRise;
        var bridge=new StaticBody3D{Name="Gantry bridge",Position=new(593.5f,deck-.1f,-138.75f),CollisionMask=0};AddChild(bridge);
        bridge.AddChild(new CollisionShape3D{Shape=new BoxShape3D{Size=new(7,.2f,4)}});
        bridge.AddChild(new MeshInstance3D{Mesh=new BoxMesh{Size=new(7,.2f,4)},MaterialOverride=steel});
        // The south rail splits round the ramp, which enters through the gap; the north edge is open for the one-way drop. Each rail is
        // solid to 1.5 m over the deck: higher than her jump clears, so the ramp and the north edge are the only ways off the bridge.
        foreach(var (name,x) in new[]{("Gantry parapet south west",-2.3f),("Gantry parapet south east",2.3f)})Box(bridge,name,new(x,.75f,1.9f),new(2.2f,1.5f,.12f),steel,true);
        foreach(var (name,x) in new[]{("Gantry end rail west",-3.4f),("Gantry end rail east",3.4f)})Box(bridge,name,new(x,.75f,0),new(.12f,1.5f,4),steel,true);
        // The ramp's 6.1 m rise over 10.75 m: its top end meets the bridge's south edge at z -136.75, a tenth over the 12 m deck.
        float rampLength=Mathf.Sqrt(CraneRampRun*CraneRampRun+CraneRampRise*CraneRampRise);
        var ramp=new StaticBody3D{Name="Gantry ramp",Position=new(594,Height(594,CraneRampFoot.Y)+CraneRampRise/2,(CraneRampFoot.Y+CraneRampTop.Y)/2),CollisionMask=0};AddChild(ramp);
        ramp.AddChild(new CollisionShape3D{Shape=new BoxShape3D{Size=new(2.4f,.18f,rampLength)}});
        var rampMesh=new MeshInstance3D{Mesh=new BoxMesh{Size=new(2.4f,.18f,rampLength)},MaterialOverride=steel};ramp.AddChild(rampMesh);
        ramp.Basis=new Basis(Vector3.Right,Mathf.Atan2(CraneRampRise,CraneRampRun));
        // The tunnel's east portal in the hillside, its label, and the same label over the Signal yard's mouth.
        Piece("Station tunnel portal","tunnel_portal",new(377-3.93f,Height(377,-149.5f),-149.5f),90);
        Label(this,"STATION TUNNEL",new(373.2f,Height(377,-149.5f)+4.4f,-149.5f),Gold,27);
        Label(this,"STATION TUNNEL",new(133,Height(134,-176)+2.6f,-176),Gold,27);
        // The Forecourt Starwell, its well lit and rested at as the villages' are.
        Well("Forecourt well",Forecourt);
        // The parcels key where the drawn first walk enters the old signal room, and the arcade's remedy pouch on its barrel in the covered
        // way: two more of the Pickups, taken with E like the rest.
        var key=new Node3D{Name="Parcels key",Position=new(ParcelsKey.X,Height(ParcelsKey.X,ParcelsKey.Y),ParcelsKey.Y)};AddChild(key);
        key.AddChild(Combined("Key",Mat(Gold,.6f),(new(0,.03f,0),new(.3f,.04f,.05f)),(new(.13f,.08f,0),new(.05f,.12f,.05f)),(new(-.16f,.07f,0),new(.1f,.14f,.05f))));
        Pickups.Add(new("parcels_key",key));
        Pouch(ArcadePouch,"Arcade pouch","Arcade barrel");
        // The station's other two supplies, through the same Pickups: the garage's cell off its through way, and the gantry's on its bridge,
        // six metres over the yard, so only the way up the ramp reaches it.
        Cell(GarageCell);Cell(CraneCell,CraneDeck);
        // Every footprint is one collider of its own, seated on the lowest ground under it and reaching its own height over the highest,
        // and the one its test blocks. S5's leaf stays unmerged under its body; S1's is its gate Part's, below.
        foreach(var o in StationObstacles)
        {
            if(o.Gate)continue;
            var (low,high)=StationFootprint(o);float tall=(high-low)+o.High;
            var body=new StaticBody3D{Name="Station footprint "+o.Name,Position=new(o.At.X,low+tall/2,o.At.Y),CollisionMask=0};AddChild(body);
            body.AddChild(new CollisionShape3D{Shape=new BoxShape3D{Size=new(o.Size.X,tall,o.Size.Y)}});
            if(o.Name==S5Name)body.AddChild(new MeshInstance3D{Mesh=new BoxMesh{Size=new(o.Size.X,tall,o.Size.Y)},MaterialOverride=Mat(new("5c4a36"))});
            else if(o.Name!="Station building"&&o.Name!="Clock tower"&&!o.Name.StartsWith("Parcels bay"))Box(this,"Station "+o.Name,new(o.At.X,low+tall/2,o.At.Y),new(o.Size.X,tall,o.Size.Y),
                o.Name.StartsWith("Parcels bay")?wall:o.Name.Contains("fence")||o.Name.Contains("wall")||o.Name.Contains("strip")?Mat(new("6b625a")):o.Name.StartsWith("Dead bus")?Mat(new("7d5a44")):o.Name.StartsWith("Wagon")?Mat(new("5b4434")):o.Name.StartsWith("Gantry leg")?steel:stone);
        }
        // The parcels bay reads as a dock-side approach: a curb with an open railing on its long side, a paved strip that follows the
        // ground, a visible door under its awning, and a railing at the return (no wall-height slot and no false service-door panel).
        // The separating footprints are unchanged, so the graph walks, the 21 m S1-to-door leg, the north-side door reach and the
        // non-jumpable collision stay as tested.
        {
            var bay=StationObstacles.Single(o=>o.Name=="Parcels bay fence");var (bayLow,_)=StationFootprint(bay);
            var ret=StationObstacles.Single(o=>o.Name=="Parcels bay return fence");
            Paving("Parcels bay paving",557.9f,560.9f,-133.5f,-107.5f,.25f,new("aca08a"));
            Box(this,"Parcels bay curb",new(bay.At.X,bayLow+.25f,bay.At.Y),new(.6f,.5f,bay.Size.Y+.4f),Mat(new("8f8677")));
            Box(this,"Parcels return curb",new(ret.At.X,bayLow+.25f,ret.At.Y),new(ret.Size.X+.4f,.5f,.6f),Mat(new("8f8677")));
            for(int i=0;i<=10;i++)
            {
                float z=Mathf.Lerp(bay.At.Y-bay.Size.Y/2+.3f,bay.At.Y+bay.Size.Y/2-.3f,i/10f);
                Box(this,"Parcels bay railing post "+i,new(bay.At.X,bayLow+1.15f,z),new(.08f,1.3f,.08f),steel);
            }
            foreach(float railY in new[]{bayLow+1.15f,bayLow+1.7f})Box(this,"Parcels bay railing rail "+railY,new(bay.At.X,railY,bay.At.Y),new(.06f,.06f,bay.Size.Y+.2f),steel);
            for(int i=0;i<=3;i++)
            {
                float x=Mathf.Lerp(ret.At.X-ret.Size.X/2+.3f,ret.At.X+ret.Size.X/2-.3f,i/3f);
                Box(this,"Parcels return railing post "+i,new(x,bayLow+1.15f,ret.At.Y),new(.08f,1.3f,.08f),steel);
            }
            foreach(float railY in new[]{bayLow+1.15f,bayLow+1.7f})Box(this,"Parcels return railing rail "+railY,new(ret.At.X,railY,ret.At.Y),new(ret.Size.X+.2f,.06f,.06f),steel);
            Box(this,"Parcels wall plinth",new(558.55f,6.25f,-121),new(.5f,.5f,22.4f),Mat(new("b6ae9a")));
            Box(this,"Parcels door lintel",new(558.6f,9.55f,-130),new(.5f,.2f,3.4f),Mat(new("b6ae9a")));
            Box(this,"Parcels door jamb north",new(558.6f,7.85f,-128.4f),new(.5f,3.5f,.2f),Mat(new("b6ae9a")));
            Box(this,"Parcels door jamb south",new(558.6f,7.85f,-131.6f),new(.5f,3.5f,.2f),Mat(new("b6ae9a")));
            Box(this,"Parcels door threshold",new(558.6f,6.24f,-130),new(.5f,.18f,3.4f),Mat(new("8f8677")));
            Roof("Parcels door awning",557.6f,560.6f,-132.8f,-127.4f,Height(559,-130)+4.0f,.2f,Mat(new("57584f")));
            Box(this,"Parcels door lamp",new(558.7f,Height(559,-130)+3.8f,-130),new(.3f,.16f,.3f),Mat(new("ffe6b0"),1f));
        }
        // S1, the yard's 4 m opening x 560..564 in the z -108 fence: the Cut gate's own hinged leaf, hung on the fence's east stub and
        // barred on the service road's side. Opened from the freight yard, it swings out over the road, away from the parcels bay's mouth
        // and from the stance that works it, and lies against the fence's line open; rest, defeat and the tunnel's travel leave it open,
        // and Restart draws it shut.
        Gate(S1Name,S1Gate.At,new Vector2(0,1),S1Gate.Span/2,new Vector2(562,-109.6f),new Vector2(562,-106.4f),Mathf.Pi/2,2.2f);
        // The station's own scatter, on the hillsides beyond its floors and footprints, from its own seeded stream. The parcels bay
        // (x 557.5..561.5, z -133..-107) is kept clear, so no tree or boulder stands on the dock approach's paving.
        bool InParcelsBay(float x,float z)=>x>=557.5f&&x<=561.5f&&z>=-133&&z<=-107;
        // The lifted east arrival looks over the west hillside's shoulder to the station: keep the scatter's tall trees and boulders out of
        // that sightline corridor, so the reveal is not a view into a tree crown. The seeded stream is unchanged (the reject branch still
        // takes the same draws); only this local corridor's scatter is skipped.
        bool InPortalSightline(float x,float z)=>x>=385&&x<=435&&z>=-155&&z<=-120;
        for(int i=0;i<160;i++)
        {
            float x=r.RandfRange(374,632),z=r.RandfRange(-198,-22),s=r.RandfRange(.7f,1.3f);int kind=i%3;
            if(!InParcelsBay(x,z)&&!InPortalSightline(x,z)&&!StationBlocked(x,z)&&MazeEdge(x,z)>3.5f&&MazeEdge(x,z)<14&&RoadGap(x,z)>3.5f)Tree(Ground(x,z),s,kind==0?"sugi":"countryside",r);
            else for(int k=0;k<3;k++)r.Randf();
        }
        for(int i=0;i<90;i++)
        {
            float x=r.RandfRange(374,632),z=r.RandfRange(-198,-22),s=r.RandfRange(.4f,1.3f),turn=r.Randf()*Mathf.Tau;
            if(!InParcelsBay(x,z)&&!InPortalSightline(x,z)&&!StationBlocked(x,z)&&MazeEdge(x,z)>1.2f&&MazeEdge(x,z)<4.5f)Boulder("Station boulder",x,z,s,turn,false);
        }
    }
}
