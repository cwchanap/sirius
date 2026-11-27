using Godot;
namespace Hoshikawa;

// Hoshikawa Station's inside, a scene of its own: built once, far from the valley at StationAt, and shown in the valley's place while Mio
// is in it (Game.ShowStation). Its layout is data in the station frame, x east and z north in metres from the building's south-west
// corner (the game's z is its -z), on two floors 5 m apart, B1 and 1F: rooms, the openings between them, the doors that shut them, the
// stairs and what stands in the rooms. StationCells rasterizes it once on a 20 cm grid that the builder and StationTest both read: a
// room less its walls is floor, and whatever no room covers is wall, or earth under the ground. Leg 1 only, the parcels door down to B1,
// round the underground mall and up through the south wing to the main doors; 2F, the platforms and B2 come with the later legs.
public partial class World
{
    public const string StationName="Hoshikawa Station";
    public const float Storey=5,StationCell=.2f,WallHalf=.4f,Ceiling=4.6f;
    public const int StationW=640,StationD=360,B1=-1,F1=0;
    // A property, not a field: SavePoints in another part of World reads it while the type is still initializing.
    public static Vector3 StationAt=>new(2000,300,-2000);
    public static Vector3 StationPoint(float x,float z,int floor)=>StationAt+new Vector3(x,floor*Storey,-z);
    public static Vector3 StationPoint(Vector2 at,int floor)=>StationPoint(at.X,at.Y,floor);
    public static bool InStation(float x,float z)=>x>StationAt.X-8&&x<StationAt.X+136&&z>StationAt.Z-44&&z<StationAt.Z+44;
    // The station frame's floor and (x, z) of a point in the game's.
    public static int StationFloor(Vector3 p)=>Mathf.FloorToInt((p.Y-StationAt.Y)/Storey+.5f);
    public static Vector2 StationFrame(Vector3 p)=>new(p.X-StationAt.X,StationAt.Z-p.Z);
    static Rect2 R(float x0,float z0,float x1,float z1)=>new(x0,z0,x1-x0,z1-z0);
    // Rooms abut, and each gives up WallHalf to the wall it shares; a room beyond leg 1 waits behind a door shut from this side or jammed.
    public static readonly (string Name,int Floor,Rect2 Area)[] StationRooms={
        ("Freight lift hall",B1,R(104,24,124,35)),("Lift corridor",B1,R(112,7,116,24)),("Cable gallery",B1,R(6,3,116,7)),("Pump room",B1,R(86,9,104,22)),
        ("Machine room",B1,R(66,9,84,22)),("Machine room passage",B1,R(73,22,77,26)),("Baggage tunnel",B1,R(66,26,104,30)),("Station arcade",B1,R(36,9,62,26)),
        ("Boiler room",B1,R(6,9,20,22)),("Archive stacks",B1,R(22,9,34,32)),("Coal store",B1,R(6,24,20,34)),
        // The underground mall under the south wing: its shops shuttered, only its alleys.
        ("Mall north alley",B1,R(4,-8,62,-4)),("Mall north alley",B1,R(66,-8,124,-4)),("Mall middle alley",B1,R(4,-21,34,-17)),("Mall middle alley",B1,R(82,-21,124,-17)),
        ("Mall south alley",B1,R(4,-34,124,-30)),("Mall cross alley",B1,R(4,-21,8,-4)),("Mall cross alley",B1,R(30,-34,34,-17)),("Mall cross alley",B1,R(82,-21,86,-4)),
        ("Mall cross alley",B1,R(108,-34,112,-17)),("Mall east alley",B1,R(120,-34,124,-4)),
        ("Waiting room",F1,R(36,2,50,26)),("Concourse",F1,R(50,2,104,26)),("Parcels room",F1,R(106,2,126,14)),("Freight office",F1,R(106,16,126,34)),
        ("South hall",F1,R(54,-22,80,-2)),("Entrance hall",F1,R(54,-34,80,-26)),("Kissaten",F1,R(26,-14,52,-2)),("Left luggage",F1,R(82,-18,102,-2)),
        ("Post office",F1,R(104,-18,126,-2)),("Food hall",F1,R(82,-34,126,-20)),
        ("Paid side",F1,R(56,26,84,35)),("Ticket office",F1,R(84,26,96,35)),("Staff stair landing",F1,R(32,20,36,28)),("Station master",F1,R(2,2,16,13)),
        ("Crew room",F1,R(18,2,32,13)),("West corridor",F1,R(2,15,36,19)),("Lockers",F1,R(2,21,16,34)),("Platform office",F1,R(18,21,32,34)),
        ("Station hotel",F1,R(2,-14,24,-2)),("Department store",F1,R(2,-34,52,-16))};
    public static readonly (int Floor,Rect2 Area)[] StationOpenings={
        (B1,R(112.6f,23.4f,115.4f,24.6f)),(B1,R(112.6f,6.4f,115.4f,7.6f)),(B1,R(99,6.4f,102,9.6f)),(B1,R(83.4f,9.6f,86.6f,12)),(B1,R(74.5f,6.4f,77.5f,9.6f)),
        (B1,R(73.6f,21.4f,76.4f,22.6f)),(B1,R(73.6f,25.4f,76.4f,26.6f)),(B1,R(103.4f,26.6f,104.6f,29.4f)),(B1,R(10,6.4f,14,9.6f)),(B1,R(19.4f,14,22.6f,18)),
        (B1,R(10,21.4f,14,24.6f)),(B1,R(33.4f,12,36.6f,16)),(B1,R(68,-4.6f,72,3.6f)),(B1,R(10,-4.6f,14,3.6f)),
        (F1,R(49.4f,2.6f,50.6f,25.4f)),(F1,R(107,13.4f,111,16.6f)),(F1,R(58,-2.6f,74,2.6f)),(F1,R(51.4f,-10,54.6f,-6)),(F1,R(79.4f,-8,82.6f,-4)),
        (F1,R(86,-20.6f,90,-17.4f)),(F1,R(118,-20.6f,122,-17.4f)),(F1,R(79.4f,-32,82.6f,-28)),(F1,R(101.4f,-12,104.6f,-8)),
        (F1,R(23,12.4f,27,15.6f)),(F1,R(15.4f,6,18.6f,9)),(F1,R(7.5f,12.4f,10.5f,15.6f)),(F1,R(7.5f,18.4f,10.5f,21.6f)),(F1,R(15.4f,27,18.6f,30)),
        (F1,R(83.4f,29,84.6f,32)),(F1,R(6,-2.6f,10,2.6f)),(F1,R(16,-16.6f,20,-13.4f))};
    // The doors that open once, World.Parts rows: the openings each stands in, the leaves that shut them (the cells the flood counts shut
    // until it opens), the side it is worked from (From) and its other side (To). S10, the steel plate over the gallery's collapse, is pushed
    // down from the west; S8, the parcels counter, opens from the concourse; S2, the main doors, from the entrance hall, and the fire shutter
    // to the south hall rises with them. S3, S7 and S12 open from their far sides, which later legs reach.
    public record StationDoor(string Name,string Kind,int Floor,Rect2[] Openings,Rect2[] Leaves,Vector2 From,Vector2 To);
    public static readonly StationDoor[] StationDoors={
        new("Steel plate","plate",B1,[R(60.8f,3.4f,66,6.6f)],[R(60.8f,3.4f,62,6.6f)],new(60,5),new(66.6f,5)),
        new("Parcels counter","gate",F1,[R(103.4f,5,106.6f,9)],[R(104.4f,5,105.6f,9)],new(102.6f,7),new(107.4f,7)),
        new("Main doors","shutter",F1,[R(58,-35.8f,74,-33.4f),R(58,-26.6f,76,-21.4f)],[R(58,-35.2f,74,-34.6f),R(58,-24.4f,76,-23.6f)],new(66,-32.8f),new(67,-20.6f)),
        new("Staff gate","gate",F1,[R(80.2f,25.4f,83.8f,26.6f)],[R(80.2f,25.4f,83.8f,26.6f)],new(82,27.4f),new(82,24.6f)),
        new("Department store door","gate",F1,[R(51.4f,-32,54.6f,-28)],[R(52.4f,-31.6f,53.6f,-28.4f)],new(50.8f,-30),new(55.2f,-30)),
        new("Staff stair door","gate",F1,[R(35.4f,22.4f,36.6f,25.4f)],[R(35.4f,22.4f,36.6f,25.4f)],new(34.6f,24),new(37.4f,24))};
    // Stairs, each a ramp from its foot on its floor up to its top on the floor above, Half its half width: walled round but for its top on
    // the floor above, where its well is open, and its foot on its own floor.
    public static readonly (string Name,int Floor,Vector2 Foot,Vector2 Top,float Half)[] StationStairs={
        ("Freight stair",B1,new(110.5f,32),new(110.5f,24),1.5f),("Arcade stair",B1,new(40,13.5f),new(40,21.5f),1.5f)};
    // What stands in the rooms, solid to its height: the collapses that cut the gallery and the north alley, the archive's zigzag of stacks,
    // B2's sealed lift and escalator, the barred grand stair up to 2F, the main doors' glass beyond their shutter.
    public static readonly (string Name,int Floor,Rect2 Area,float Tall)[] StationBlocks={
        ("Gallery collapse",B1,R(84,3,90,7),Ceiling),("Mall collapse",B1,R(62,-8,66,-4),Ceiling),
        ("Archive stack",B1,R(25.6f,9,26.4f,28),2.8f),("Archive stack",B1,R(29.6f,13,30.4f,32),2.8f),
        ("B2 lift seal",B1,R(116,26,122,32),Ceiling),("B2 escalator seal",B1,R(86,13,94,20),Ceiling),("B2 escalator seal",F1,R(86,13,94,20),Ceiling),
        ("Kiosk",F1,R(80,2,96,6),2.6f),("Food stalls",F1,R(82,-26.4f,118,-23.6f),2.2f),("Luggage counter",F1,R(82,-11.6f,96,-10.4f),1.6f),
        ("Store shelving",F1,R(10,-28.6f,52,-27.4f),2.2f),("Store shelving",F1,R(2,-22.6f,44,-21.4f),2.2f),
        ("Main door glass",F1,R(58,-36,74,-35.8f),Ceiling),("Grand stair",F1,R(96,10,104,24),Ceiling)};
    // Doors seen but never used: the waiting room's into the west wing, the freight office's onto platform 1, the kissaten's into the hotel,
    // the arcade's shutter, the mall's stair up to the forecourt, and the ticket gates to the paid side.
    public static readonly (string Name,int Floor,Rect2 Area)[] StationJammed={
        ("Waiting room side door",F1,R(35.4f,15.6f,36.6f,18.4f)),("Platform door",F1,R(113.6f,33.4f,118.4f,36)),("Hotel door",F1,R(23.4f,-10,26.6f,-6)),
        ("Arcade shutter",B1,R(43,6.4f,49,9.6f)),("Forecourt stair shutter",B1,R(92,-36,96,-33.4f)),("Ticket gates",F1,R(56.6f,25.4f,80.2f,26.6f))};
    public static readonly (string Name,int Floor,Rect2 Area)[] StationWater={("Pump room water",B1,R(86.4f,9.4f,103.6f,21.6f)),("East alley water",B1,R(120.4f,-33.6f,123.6f,-4.4f))};
    // The hanging signboards that point the way, there being no minimap inside: each arrow points Toward, in degrees clockwise from north.
    public static readonly (string Text,int Floor,Vector2 At,float Toward)[] StationSigns={
        ("PLATFORMS",F1,new(66,23.6f),0),("WAITING ROOM",F1,new(60,8),270),("2F",F1,new(94.6f,22.5f),0),("CONCOURSE",F1,new(76,-10),0),
        ("DEPARTMENT STORE",F1,new(57,-27.6f),270),("FORECOURT",B1,new(88,-6),180)};
    // Leg 1's 37 robots, 23 on B1 and 14 on 1F; a sleeping scout's facing in degrees.
    public static readonly (string Kind,int Floor,Vector2 At,float? Sleeps)[] StationRobots={
        ("drone",B1,new(107,26),null),("drone",B1,new(98,12),null),("drone",B1,new(101,18.5f),null),("patrol",B1,new(90,28),null),("scout",B1,new(100,-6),null),
        ("scout",B1,new(52,5),null),("scout",B1,new(24,5),90),("patrol",B1,new(13,14),null),("scout",B1,new(28,20),0),("scout",B1,new(32,28),180),
        ("scout",B1,new(45,16),null),("drone",B1,new(56,22.5f),null),("scout",B1,new(78,-6),null),("patrol",B1,new(97,-19),null),("drone",B1,new(116,-32),null),
        ("scout",B1,new(90,-32),null),("patrol",B1,new(56,-32),null),("scout",B1,new(42,-32),270),("drone",B1,new(122,-16),null),("scout",B1,new(20,-19),null),
        ("scout",B1,new(14,-32),90),("scout",B1,new(36,-6),0),("drone",B1,new(48,-6),null),
        ("scout",F1,new(62,7),45),("drone",F1,new(82,16),null),("scout",F1,new(45,8),null),("scout",F1,new(112,7),null),("scout",F1,new(122,8.5f),270),
        ("patrol",F1,new(116,27),null),("patrol",F1,new(72,-31),null),("drone",F1,new(60,-14),null),("scout",F1,new(33,-9),null),("scout",F1,new(100,-22),null),
        ("scout",F1,new(112,-31),90),("drone",F1,new(92,-30),null),("scout",F1,new(92,-14.5f),180),("scout",F1,new(113,-13),null)};
    public static readonly (string Item,int Floor,Vector2 At)[] StationPickups={
        ("pouch",B1,new(102,11)),("pouch",B1,new(122,-26)),("pouch",F1,new(46,-11)),
        ("cell",B1,new(17,31)),("cell",B1,new(24,29.4f)),("cell",B1,new(56,-6)),("cell",B1,new(6,-32)),("cell",F1,new(123,-4.5f))};
    // The rolling loads, parked at the head of their runs, which come at Mio as she nears the foot: the luggage cart down the baggage
    // tunnel as she comes in from the lift hall, the shopping trolley along the mall's south alley as she turns into it.
    public static readonly (string Name,string Kind,int Floor,Vector2 From,Vector2 To)[] StationLoads={
        ("Luggage cart","cart",B1,new(70,28),new(100,28)),("Shopping trolley","trolley",B1,new(66,-32),new(100,-32))};
    // The doors out to the Outside, where Mio stands to use each and faces on coming in, and the part that must be open first.
    public record StationExit(string Name,int Floor,Vector2 At,Vector2 Landing,float Facing,string? After);
    public static readonly StationExit[] StationExits={new("PARCELS DOOR",F1,new(124.6f,10),new(122,10),270,null),new("MAIN DOORS",F1,new(66,-34),new(66,-31.4f),0,"Main doors")};
    // The Machine room's Starwell, a property for SavePoints as StationAt is, and where the grand stair's bars stop leg 1.
    public static Vector2 MachineWell=>new(78.6f,13.2f);
    public static readonly Vector2 StairBars=new(100,9.4f);
    public const byte Rock=0,Open=1,Hole=2,Ramp=3,Blocked=4,Leaf=5;
    // The grid, [floor - B1][x / StationCell, (z + 36) / StationCell], over the building's 128 x 72 m.
    public static byte[][,] StationCells=>cells.Value;
    private static readonly Lazy<byte[][,]> cells=new(Rasterize);
    public static (int I0,int J0,int I1,int J1) Cells(Rect2 r)=>(Math.Clamp((int)MathF.Round(r.Position.X/StationCell),0,StationW),Math.Clamp((int)MathF.Round((r.Position.Y+36)/StationCell),0,StationD),
        Math.Clamp((int)MathF.Round(r.End.X/StationCell),0,StationW),Math.Clamp((int)MathF.Round((r.End.Y+36)/StationCell),0,StationD));
    public static Vector2I StationCellAt(Vector2 p)=>new(Math.Clamp((int)(p.X/StationCell),0,StationW-1),Math.Clamp((int)((p.Y+36)/StationCell),0,StationD-1));
    public static Vector2 StationCellMiddle(int i,int j)=>new((i+.5f)*StationCell,(j+.5f)*StationCell-36);
    // A stair's footprint, and the strips across its top and its foot.
    public static Rect2 StairWell((string Name,int Floor,Vector2 Foot,Vector2 Top,float Half) s)
    {
        var a=new Vector2(Math.Min(s.Foot.X,s.Top.X),Math.Min(s.Foot.Y,s.Top.Y));var b=new Vector2(Math.Max(s.Foot.X,s.Top.X),Math.Max(s.Foot.Y,s.Top.Y));
        var across=s.Foot.X==s.Top.X?new Vector2(s.Half,0):new Vector2(0,s.Half);return new Rect2(a-across,b-a+2*across);
    }
    static Rect2 End(Rect2 well,Vector2 end,Vector2 other)
    {
        var d=(end-other).Normalized()*WallHalf;var r=new Rect2(well.Position,well.Size);
        if(d.X>0)return R(r.End.X,r.Position.Y,r.End.X+d.X,r.End.Y);if(d.X<0)return R(r.Position.X+d.X,r.Position.Y,r.Position.X,r.End.Y);
        return d.Y>0?R(r.Position.X,r.End.Y,r.End.X,r.End.Y+d.Y):R(r.Position.X,r.Position.Y+d.Y,r.End.X,r.Position.Y);
    }
    private static byte[][,] Rasterize()
    {
        var c=new[]{new byte[StationW,StationD],new byte[StationW,StationD]};
        void Fill(int f,Rect2 r,byte k){var (i0,j0,i1,j1)=Cells(r);for(int i=i0;i<i1;i++)for(int j=j0;j<j1;j++)c[f-B1][i,j]=k;}
        foreach(var (_,f,r) in StationRooms)Fill(f,r.Grow(-WallHalf),Open);
        foreach(var (f,r) in StationOpenings)Fill(f,r,Open);
        foreach(var d in StationDoors){foreach(var r in d.Openings)Fill(d.Floor,r,Open);foreach(var r in d.Leaves)Fill(d.Floor,r,Leaf);}
        foreach(var (_,f,r,_) in StationBlocks)Fill(f,r,Blocked);
        foreach(var (_,f,r) in StationJammed)Fill(f,r,Blocked);
        foreach(var s in StationStairs)
        {
            var well=StairWell(s);
            Fill(s.Floor+1,well.Grow(WallHalf),Rock);Fill(s.Floor+1,End(well,s.Top,s.Foot),Open);Fill(s.Floor+1,well,Hole);
            Fill(s.Floor,well.Grow(WallHalf),Rock);Fill(s.Floor,End(well,s.Foot,s.Top),Open);Fill(s.Floor,well,Ramp);
        }
        return c;
    }
    // A floor's cells of one kind as few rectangles, in metres.
    public static List<Rect2> StationRects(byte[,] c,Func<byte,bool> kind)
    {
        var used=new bool[StationW,StationD];var rects=new List<Rect2>();
        for(int j=0;j<StationD;j++)for(int i=0;i<StationW;i++)
        {
            if(used[i,j]||!kind(c[i,j]))continue;
            int w=1;while(i+w<StationW&&!used[i+w,j]&&kind(c[i+w,j]))w++;
            int h=1;while(j+h<StationD&&Enumerable.Range(i,w).All(x=>!used[x,j+h]&&kind(c[x,j+h])))h++;
            for(int x=i;x<i+w;x++)for(int y=j;y<j+h;y++)used[x,y]=true;
            rects.Add(new Rect2(i*StationCell,j*StationCell-36,w*StationCell,h*StationCell));
        }
        return rects;
    }
    // Which room a place is in, and a robot's patrol loop scaled to it, as Room() scales Tunnel Mountain's to its trail.
    public static (string Name,int Floor,Rect2 Area)? StationRoomAt(Vector2 at,int floor)=>StationRooms.Where(r=>r.Floor==floor&&r.Area.HasPoint(at)).Cast<(string,int,Rect2)?>().FirstOrDefault();
    public static float StationReach(Vector2 at,int floor)=>StationRoomAt(at,floor) is {} r?Mathf.Clamp(Math.Min(r.Area.Size.X,r.Area.Size.Y)/2/3.5f,.35f,1):.35f;
    // The whole inside, under one plain Node3D at the origin so that its parts' From and To are the game's own coordinates; Game adds it and
    // then Merge() bakes it as it does the valley.
    public Node3D Station()
    {
        var root=new Node3D{Name="Hoshikawa Station",Visible=false};
        Material plaster=Mat(new("c9bfa9")),concrete=Mat(new("8e877b")),tiles=Mat(new("a69c88")),screed=Mat(new("6d685f")),ceiling=Mat(new("57534c")),
            steel=Mat(new("5d6468")),rust=Mat(new("7b5a44")),timber=Mat(new("6a5038")),shutter=Mat(new("77746b")),rubble=Mat(new("807a70")),
            city=Mat(new("4fd2ff"),1.6f),glass=Mat(new("d9e4dc"),.6f),sign=Mat(new("1d4f91"),.25f),white=Mat(new("f3eee4"),.4f);
        Vector3 At(Rect2 r,float y,int floor)=>StationPoint(r.GetCenter(),floor)+Vector3.Up*y;
        MeshInstance3D Slab(string name,Rect2 r,int floor,float y0,float y1,Material look,bool solid=true)=>Box(root,name,At(r,(y0+y1)/2,floor),new(r.Size.X,y1-y0,r.Size.Y),look,solid);
        for(int f=B1;f<=F1;f++)
        {
            var c=StationCells[f-B1];
            foreach(var r in StationRects(c,k=>k==Rock))Slab("Station wall",r,f,0,Ceiling,f==B1?concrete:plaster);
            foreach(var r in StationRects(c,k=>k!=Hole))Slab("Station floor",r,f,-.4f,0,f==B1?screed:tiles);
        }
        Slab("Station roof",R(0,-36,128,36),F1,Ceiling,Storey,ceiling);
        foreach(var s in StationStairs)
        {
            // A slab pitched from the floor above at its top down to its foot, its upper face the walk.
            Vector3 top=StationPoint(s.Top,s.Floor+1),foot=StationPoint(s.Foot,s.Floor);var d=top-foot;
            var ramp=Box(root,s.Name,(top+foot)/2+Vector3.Down*.2f,new(2*s.Half,.4f,d.Length()+.3f),concrete,true);ramp.Basis=Basis.LookingAt(d,Vector3.Up);
            for(int i=0;i<16;i++){float t=(i+.5f)/16;Box(root,"Stair tread",foot+d*t+Vector3.Up*.01f,new(2*s.Half-.1f,.03f,.12f),steel).Basis=ramp.Basis;}
        }
        foreach(var (name,f,r,tall) in StationBlocks)
        {
            if(name=="Grand stair")
            {
                // The way up to 2F, barred at its foot and along its open side until the next leg: its steps rise into the ceiling.
                for(int i=0;i<7;i++){float z0=r.Position.Y+i*2;Slab("Grand stair step",R(r.Position.X,z0,r.End.X,z0+2),f,0,.66f*(i+1),tiles);}
                var bars=new Node3D{Name="Grand stair bars"};root.AddChild(bars);
                foreach(var side in new[]{R(r.Position.X-.1f,r.Position.Y-.2f,r.End.X,r.Position.Y),R(r.Position.X-.2f,r.Position.Y,r.Position.X,r.End.Y)})
                {
                    var wall=new StaticBody3D{Position=At(side,Ceiling/2,f),CollisionMask=0};bars.AddChild(wall);wall.AddChild(new CollisionShape3D{Shape=new BoxShape3D{Size=new(side.Size.X,Ceiling,side.Size.Y)}});
                    bool along=side.Size.X>side.Size.Y;float length=along?side.Size.X:side.Size.Y;
                    for(float t=.15f;t<length;t+=.3f)Box(root,"Grand stair bar",StationPoint(along?side.Position.X+t:side.GetCenter().X,along?side.GetCenter().Y:side.Position.Y+t,f)+Vector3.Up*Ceiling/2,new(.06f,Ceiling,.06f),rust);
                    Slab("Grand stair rail",side,f,1.1f,1.2f,rust,false);Slab("Grand stair rail",side,f,3.4f,3.5f,rust,false);
                }
            }
            else if(name.StartsWith("B2"))
            {
                // City power, sealed: the lit shutter the occupied city keeps on its own doors.
                Slab(name,r,f,0,tall,steel);Slab(name+" glow",r.Grow(.03f),f,.4f,tall-.6f,city,false);
            }
            else if(name.EndsWith("collapse"))
            {
                Slab(name,r,f,0,tall,rubble);
                for(int i=0;i<5;i++){var p=At(r,.4f,f)+new Vector3((i%3-1)*r.Size.X*.3f,0,(i%2-.5f)*r.Size.Y*.6f);Box(root,"Rubble",p,new(1.2f,.8f+.2f*i,1),concrete).Rotation=new(0,i*.7f,.2f);}
            }
            else Slab(name,r,f,0,tall,name=="Main door glass"?glass:name.StartsWith("Archive")||name.StartsWith("Store")?timber:steel);
        }
        foreach(var (name,f,r) in StationJammed)
        {
            Slab(name,r,f,0,3,name=="Ticket gates"?steel:rust);Slab(name+" lintel",r,f,3,Ceiling,f==B1?concrete:plaster);
            if(name=="Ticket gates")for(float x=r.Position.X+.6f;x<r.End.X;x+=1.2f)Box(root,"Ticket gate lamp",StationPoint(x,r.GetCenter().Y,f)+Vector3.Up*1.05f,new(.3f,.06f,r.Size.Y+.04f),Mat(new("c25b4e"),.8f));
        }
        foreach(var (name,f,r) in StationWater)
            Box(root,name,At(r,.12f,f),new(r.Size.X,.04f,r.Size.Y),new StandardMaterial3D{AlbedoColor=new(.25f,.38f,.42f,.6f),Transparency=BaseMaterial3D.TransparencyEnum.Alpha,Roughness=.08f,Metallic=.3f});
        foreach(var d in StationDoors)StationDoorPart(root,d,steel,shutter,timber);
        // Lintels over the doorways, and over the gates, whose leaves stand 3 m tall.
        foreach(var (f,r) in StationOpenings.Where(o=>Math.Max(o.Area.Size.X,o.Area.Size.Y)<6).Concat(StationDoors.Where(d=>d.Kind=="gate").SelectMany(d=>d.Openings.Select(o=>(d.Floor,o)))))
            Slab("Lintel",r,f,3,Ceiling,f==B1?concrete:plaster);
        foreach(var (text,f,at,toward) in StationSigns)Signboard(root,text,StationPoint(at,f),toward,sign,white);
        foreach(var (item,f,at) in StationPickups){if(item=="pouch")Pouch(root,StationPoint(at,f),"Station pouch","Station pouch barrel");else Cell(root,StationPoint(at,f));}
        foreach(var (name,kind,f,a,b) in StationLoads)Load(root,name,kind,StationPoint(a,f),StationPoint(b,f));
        // The Machine room's Starwell, Tunnel Mountain's well model again, and the doors out, a parcels door in the east wall and the main
        // doors' glass beyond their shutter.
        Piece("Machine room well","court_starwell",StationPoint(MachineWell,B1),0,2.2f,root);
        Slab("Parcels door",R(125.5f,8.4f,126.4f,11.6f),F1,0,2.8f,timber);
        // A plain light, unshadowed, so the boxes read; the valley's sun is hidden with the valley.
        root.AddChild(new DirectionalLight3D{Name="Station light",RotationDegrees=new(-58,-30,0),LightColor=new("e8e2d6"),LightEnergy=.7f,ShadowEnabled=false});
        return root;
    }
    // A door's leaves under one moving body: a plate hinged at its foot that falls flat, a gate hinged at one end that swings about the
    // vertical, or shutters that rise into the ceiling.
    private void StationDoorPart(Node3D root,StationDoor d,Material steel,Material shutter,Material timber)
    {
        var from=StationPoint(d.From,d.Floor);var to=StationPoint(d.To,d.Floor);var leaf=d.Leaves[0];
        if(d.Kind=="shutter")
        {
            var body=new AnimatableBody3D{Name=d.Name,Position=StationPoint(0,0,d.Floor),CollisionMask=0};root.AddChild(body);
            foreach(var r in d.Leaves)
            {
                var size=new Vector3(r.Size.X,Ceiling,r.Size.Y);var at=StationPoint(r.GetCenter(),d.Floor)-body.Position+Vector3.Up*Ceiling/2;
                body.AddChild(new CollisionShape3D{Position=at,Shape=new BoxShape3D{Size=size}});body.AddChild(Combined("Shutter",shutter,(at,size)));
                for(float y=.3f;y<Ceiling;y+=.3f)body.AddChild(Combined("Shutter slat",steel,(new(at.X,y,at.Z),new(size.X+.02f,.04f,size.Z+.02f))));
            }
            var part=new Part(d.Name,d.Kind,body,from,to,0,Ceiling-.2f);part.Swing(0);Parts.Add(part);return;
        }
        bool alongZ=leaf.Size.Y>leaf.Size.X;float length=alongZ?leaf.Size.Y:leaf.Size.X;
        if(d.Kind=="plate")
        {
            // Hinged at its east foot, sunk so that it falls east to lie flush with the floor, a step Mio walks over without a stair.
            var hinge=StationPoint(leaf.End.X,leaf.GetCenter().Y,d.Floor)+Vector3.Down*.17f;var body=new AnimatableBody3D{Name=d.Name,Position=hinge,CollisionMask=0};root.AddChild(body);
            var size=new Vector3(length,Ceiling-.2f,.2f);var at=new Vector3(0,size.Y/2,-.1f);
            body.AddChild(new CollisionShape3D{Position=at,Shape=new BoxShape3D{Size=size}});body.AddChild(Combined("Plate",steel,(at,size),(at+new Vector3(0,0,-.12f),new(length-.4f,.12f,.06f))));
            var part=new Part(d.Name,d.Kind,body,from,to,0,Mathf.Pi/2,Mathf.Pi/2);part.Swing(0);Parts.Add(part);return;
        }
        // A gate hinged at its leaf's south or west end, swinging away from the side it is worked from.
        var hingeAt=alongZ?new Vector2(leaf.GetCenter().X,leaf.Position.Y):new Vector2(leaf.Position.X,leaf.GetCenter().Y);
        var gate=new AnimatableBody3D{Name=d.Name,Position=StationPoint(hingeAt,d.Floor),CollisionMask=0};root.AddChild(gate);
        var leafSize=new Vector3(.2f,3,length);var mid=new Vector3(0,1.5f,length/2);
        gate.AddChild(new CollisionShape3D{Position=mid,Shape=new BoxShape3D{Size=leafSize}});gate.AddChild(Combined("Gate leaf",d.Kind=="gate"&&d.Name=="Parcels counter"?timber:steel,(mid,leafSize)));
        float closed=alongZ?Mathf.Pi:Mathf.Pi/2,away=Mathf.Atan2(to.X-from.X,to.Z-from.Z);
        var row=new Part(d.Name,d.Kind,gate,from,to,0,Mathf.Wrap(away-closed,-Mathf.Pi,Mathf.Pi),closed);row.Swing(0);Parts.Add(row);
    }
    // A board hung from the ceiling on two rods, its local Z along the way it points: its words on both faces and its arrow through it.
    private static void Signboard(Node3D root,string text,Vector3 at,float toward,Material board,Material white)
    {
        var n=new Node3D{Name="Signboard "+text,Position=at+Vector3.Up*3.2f,Rotation=new(0,Mathf.Pi-Mathf.DegToRad(toward),0)};root.AddChild(n);
        Box(n,"Signboard",Vector3.Zero,new(.08f,.62f,2.6f),board);
        foreach(float s in new[]{-.9f,.9f})Box(n,"Signboard rod",new(0,.86f,s),new(.03f,1.1f,.03f),board);
        Box(n,"Arrow",new(0,-.15f,0),new(.1f,.07f,1),white);
        foreach(float s in new[]{-1f,1f})Box(n,"Arrow head",new(0,-.15f+s*.12f,.37f),new(.1f,.07f,.36f),white).Rotation=new(s*.75f,0,0);
        foreach(float s in new[]{-1f,1f})n.AddChild(new Label3D{Text=text,Position=new(s*.05f,.13f,0),Rotation=new(0,s*Mathf.Pi/2,0),FontSize=36,PixelSize=.0055f,Modulate=new("f3eee4"),OutlineSize=0,DoubleSided=false});
    }
}
