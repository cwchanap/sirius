using GdUnit4;
using Godot;
using static GdUnit4.Assertions;
namespace Hoshikawa.Tests;

// Hoshikawa Station's outside map, east of Tunnel Mountain: its ground joins the 1 m terrain, its trails and footprint data keep the
// intended first walk, the yard opens only by its fence gap until S1 opens, the tunnel's two doors connect the two halves and nothing
// else does. Distances are measured on the built ground with WildsTest.Walk, the layout with WildsTest.Reach; the station's own
// StationObstacles is the one blocked predicate, with S1 skipped only when the test opens it.
[TestSuite]
public class StationOutsideTest
{
    static readonly Rect2I Area=WildsTest.Area;
    static readonly Vector2 Portal=World.StationPortal,Forecourt=World.Forecourt,Parcels=World.ParcelsDoor,DoorApproach=new(559,-130);
    // The source's first walk enters the old signal room at (618,-153.5) for the parcels key. Its final straight return, (561,-114) to the
    // door, is replaced by the parcels bay's adjustable mouth at (559,-112); both are walked in the ordered itinerary below.
    static readonly Vector2 SignalKeyPoint=new(618,-153.5f),BayMouth=new(559,-112);
    // The first walk's waypoints as the v9 drawing gives them, world (x, z): the portal shelf down the lineman's path to the Forecourt,
    // then the long way round the depot and the yard fence to S1 and back to the parcels door.
    static readonly Vector2[] FirstWalk={
        new(377,-149.5f),new(384,-146),new(386,-138),new(376,-132),new(390,-125),new(382,-116),new(392,-104),new(386,-92),new(400,-80),new(418,-72),new(438,-68),
        new(458,-76),new(490,-79),new(508,-70),
        new(508,-58),new(522,-50),new(532,-46),new(539,-42),new(544,-38),new(558,-36),new(568,-36),new(582,-46),new(598,-64),new(612,-82),new(620,-96),new(623.5f,-107),
        new(624.5f,-114),new(625,-123),new(619,-131),new(616,-141),new(617.5f,-149),new(618,-153.5f),new(616,-150.2f),new(608,-152.4f),new(580,-152.4f),new(570,-152.4f),
        new(564,-150),new(561,-146),new(561,-132),new(562,-109),
        new(561,-114),new(559,-130)};
    // The actual southeast opening: the four free cells x 622..625 in the z -108 fence, not the fence either side of it.
    static bool Southeast(int x,int z)=>x>=622&&x<=625&&z==-108;
    static (bool[,] Seen,Rect2I Area) From(Vector2 start,bool s1=false,bool capSoutheast=false)
        => WildsTest.Reach(new((int)Mathf.Round(start.X),(int)Mathf.Round(start.Y)),(x,z)=>World.StationBlocked(x,z,s1)||(capSoutheast&&Southeast(x,z)));
    static bool Has((bool[,] Seen,Rect2I Area) reach,Vector2 p)=>reach.Seen[(int)Mathf.Round(p.X)-reach.Area.Position.X,(int)Mathf.Round(p.Y)-reach.Area.Position.Y];
    static IEnumerable<Vector2I> Cells((bool[,] Seen,Rect2I Area) reach)=>WildsTest.Cells(reach);
    static float Walk(Vector2 from,Vector2 to,bool s1=false,params (Vector2 A,Vector2 B)[] links)
        => WildsTest.Walk(from,to,(x,z)=>World.StationBlocked(x,z,s1),links);
    static float Polyline(IEnumerable<Vector2> points){var p=points.ToArray();return Enumerable.Range(0,p.Length-1).Sum(i=>p[i].DistanceTo(p[i+1]));}
    static float WalkPolyline(IEnumerable<Vector2> points,bool s1=false)
    {
        var p=points.ToArray();float sum=0;
        for(int i=0;i<p.Length-1;i++){float leg=Walk(p[i],p[i+1],s1);if(float.IsInfinity(leg))return leg;sum+=leg;}
        return sum;
    }
    static readonly (Vector2 A,Vector2 B) Tunnel=(World.Doors.Single(d=>d.At.X<200).Landing!.Value,World.Doors.Single(d=>d.At.X>200).Landing!.Value);
    static readonly Vector2 Court=World.SavePoints.Single(s=>s.Place=="Court").At,Halt=World.SavePoints.Single(s=>s.Place=="Halt").At;

    [TestCase]
    public void TheStationGroundJoinsTheFineAreasAtItsSeam()
    {
        AssertThat(World.StationFine).IsEqual(new Rect2I(372,-200,264,180));
        AssertThat(World.FineAreas).ContainsExactly(World.Fine,World.MazeFine,World.StationFine);
        AssertThat(World.Far.End.X).IsEqual(704);
        AssertThat(World.SquareArea.Position).IsEqual(new Vector2I(-112,-320));
        AssertThat(World.SquareArea.End.Y).IsEqual(32);
        AssertThat(World.SquareArea.Size.X).IsGreaterEqual(832);
        foreach(var (p,fine) in new[]{(new Vector2I(372,-100),true),(new Vector2I(500,-100),true),(new Vector2I(635,-21),true),(new Vector2I(368,-100),true),
            (new Vector2I(-105,-100),false),(new Vector2I(637,-100),false),(new Vector2I(500,-201),false),(new Vector2I(500,-19),false),(new Vector2I(100,-100),true),(new Vector2I(300,-100),true)})
            AssertThat(World.IsFine(p)).OverrideFailureMessage($"IsFine({p})").IsEqual(fine);
        // The far 4 m grid fans its cells along every StationFine edge to the 1 m ground's own vertices, so the two meshes share them.
        static bool OnFine(Vector2I p)=>World.FineAreas.Any(f=>p.X>=f.Position.X&&p.X<=f.End.X&&p.Y>=f.Position.Y&&p.Y<=f.End.Y);
        foreach(var cell in new[]{new Vector2I(636,-100),new Vector2I(500,-204),new Vector2I(500,-20)})
        {
            var sides=World.FineSides(cell,4);
            AssertThat(sides).OverrideFailureMessage($"no fine side for {cell}").IsNotEmpty();
            AssertThat(sides.SelectMany(s=>new[]{s.A,s.B}).All(OnFine)).OverrideFailureMessage($"a side of {cell} is not on the fine ground").IsTrue();
        }
    }

    [TestCase]
    public void TheStationRegionCoversTheLayoutAndLeavesTunnelMountainAlone()
    {
        foreach(var p in new[]{new Vector3(368,0,-100),new Vector3(371,0,-149.5f),new Vector3(500,0,-100),new Vector3(624,0,-120),new Vector3(508,0,-70)})
            AssertThat(World.RegionAt(p)).OverrideFailureMessage($"region at {p}").IsEqual(World.StationName);
        foreach(var p in new[]{new Vector3(360,0,-172.5f),new Vector3(347,0,-172),new Vector3(353,0,-176),new Vector3(365,0,-100),new Vector3(500,0,-19),new Vector3(500,0,-201)})
            AssertThat(World.RegionAt(p)).OverrideFailureMessage($"region at {p}").IsEqual(World.MazeName);
        AssertThat(World.RegionAt(new Vector3(500,0,-100)).ToUpperInvariant()).IsEqual(World.StationToast);
        AssertThat(World.StationToast).IsEqual(World.StationName.ToUpperInvariant());
        AssertThat(World.RegionAt(new Vector3(0,0,30))).IsEqual("Hoshikawa");
    }

    [TestCase]
    public void TheTwoPortalDoorsFaceEachOtherWithRoomToLand()
    {
        AssertThat(Parcels).IsEqual(new Vector2(558,-130));
        AssertThat(World.Doors.Length).IsEqual(2);
        AssertThat(World.Doors.All(d=>d.Kind==World.DoorKind.Portal&&d.Label=="STATION TUNNEL"&&d.Icon==""&&d.Key==null&&d.Landing!=null)).IsTrue();
        AssertThat(World.Doors.Select(d=>d.Name).Distinct().Count()).IsEqual(2);
        var west=World.Doors.Single(d=>d.At.X<200);var east=World.Doors.Single(d=>d.At.X>200);
        AssertThat(west.At).IsEqual(new Vector2(134,-176));
        AssertThat(east.At).IsEqual(new Vector2(379,-149.5f));
        AssertThat(west.Landing!.Value.DistanceTo(east.At)).IsGreater(2.2f);
        AssertThat(east.Landing!.Value.DistanceTo(west.At)).IsGreater(2.2f);
        // The west row's landing is on the station side, the east row's on the mountain side: each directly names the other's ground.
        var stationLanding=west.Landing!.Value;var mountainLanding=east.Landing!.Value;
        AssertThat(World.RegionAt(new Vector3(mountainLanding.X,0,mountainLanding.Y))).IsEqual(World.MazeName);
        AssertThat(World.MazeEdge(mountainLanding.X,mountainLanding.Y)).IsLess(0);
        AssertThat(World.RegionAt(new Vector3(stationLanding.X,0,stationLanding.Y))).IsEqual(World.StationName);
        AssertThat(World.StationBlocked(stationLanding.X,stationLanding.Y)).IsFalse();
        AssertThat(World.DoorPoint(west)).IsEqual(new Vector3(134,World.Height(134,-176),-176));
        AssertThat(mountainLanding.DistanceTo(west.At)).IsGreater(2.2f);
        AssertThat(stationLanding.DistanceTo(east.At)).IsGreater(2.2f);
        // Each faces out of the tunnel on arrival: the headings are opposites.
        AssertThat(Mathf.Abs(Mathf.Wrap(east.Yaw-west.Yaw,Mathf.Pi,Mathf.Tau))).IsGreater(1f);
    }

    [TestCase]
    public void TheStationArrivalLiftsTheCameraAndTheDockSeparatorsStand()
    {
        // The east-arrival row lifts the follow camera and sets the reveal's own level pitch; the return row keeps the normal camera.
        // Game holds the reveal through the blackout's dark part, eases it back to the player's aim, and Place()/Retry()/Restart() clear it.
        var arrival=World.Doors.Single(d=>d.At.X<200);var back=World.Doors.Single(d=>d.At.X>200);
        AssertThat(arrival.Lift).IsEqual(10.65f);
        AssertThat(arrival.Pitch).IsEqual(0f);
        AssertThat(back.Lift).IsEqual(0f);
        AssertThat(back.Pitch).IsEqual(-.29f);
        // The parcels separators keep their shared, non-jumpable footprints while their look changes to a curb and railing.
        var bay=World.StationObstacles.Single(o=>o.Name=="Parcels bay fence");
        AssertThat(bay.At).IsEqual(new Vector2(560.2f,-122.75f));
        AssertThat(bay.Size).IsEqual(new Vector2(.6f,19.5f));
        AssertThat(bay.High).IsEqual(1.8f);
        var ret=World.StationObstacles.Single(o=>o.Name=="Parcels bay return fence");
        AssertThat(ret.At).IsEqual(new Vector2(559.1f,-132.5f));
        AssertThat(ret.Size).IsEqual(new Vector2(2.4f,.6f));
        AssertThat(ret.High).IsEqual(1.8f);
        // The real parcels door's interaction anchor is untouched by the visual door move.
        AssertThat(World.ParcelsDoor).IsEqual(new Vector2(558,-130));
        AssertThat(World.S1Gate).IsEqual((new Vector2(562,-108),4f));
    }

    [TestCase]
    public void TheCheckpointsGainTheForecourtWithoutMovingTheOthers()
    {
        AssertThat(World.SavePoints.Length).IsEqual(5);
        AssertThat(World.SavePoints[0]).IsEqual(("Hoshikawa",new Vector2(-3.2f,24)));
        AssertThat(World.SavePoints[1]).IsEqual(("Komorebi",new Vector2(-4.8f,-249.6f)));
        AssertThat(World.SavePoints[2]).IsEqual(("Court",new Vector2(92,-156)));
        AssertThat(World.SavePoints[3]).IsEqual(("Halt",new Vector2(247,-173)));
        AssertThat(World.SavePoints[4]).IsEqual(("Forecourt",Forecourt));
        AssertThat(Math.Abs(World.Height(Forecourt.X,Forecourt.Y)-6)).IsLess(.01f);
        // Player.Respawn() stands her 2 m south of the well; that spot is clear of the station's footprints.
        AssertThat(World.StationBlocked(Forecourt.X,Forecourt.Y+2)).IsFalse();
        AssertThat(World.MazeEdge(Forecourt.X,Forecourt.Y+2)).IsLess(0);
        AssertThat(World.RegionAt(new Vector3(Forecourt.X,0,Forecourt.Y+2))).IsEqual(World.StationName);
    }

    [TestCase]
    public void TheYardOpensOnlyByItsFenceGapUntilS1Opens()
    {
        var gate=World.StationObstacles.Single(o=>o.Gate);
        AssertThat(gate.At).IsEqual(new Vector2(562,-108));
        AssertThat(gate.Size.X).IsEqual(4);
        AssertThat(World.S1Gate).IsEqual((new Vector2(562,-108),4f));
        AssertThat(World.StationBlocked(562,-108)).IsTrue();
        AssertThat(World.StationBlocked(562,-108,true)).IsFalse();
        AssertThat(World.StationBlocked(620,-108)).IsTrue();
        AssertThat(World.StationBlocked(623,-108)).IsFalse();
        // Landmarks inside the yard, the loading dock and the two route openings.
        var inside=new[]{new Vector2(616,-134),new Vector2(561,-146),new Vector2(559,-130),new Vector2(618,-155),World.CraneRampFoot};
        // With S1 shut and the fence gap blocked nothing in the yard is reached; the gap alone, and S1 alone once open, both reach it.
        var capped=From(Forecourt,false,true);
        AssertThat(inside.Where(p=>Has(capped,p))).OverrideFailureMessage($"S1 shut and the gap blocked, the yard is reached at {string.Join(", ",inside.Where(p=>Has(capped,p)))}").IsEmpty();
        var throughGap=From(Forecourt,false);
        AssertThat(inside.Where(p=>!Has(throughGap,p))).OverrideFailureMessage($"the fence gap does not reach the yard at {string.Join(", ",inside.Where(p=>!Has(throughGap,p)))}").IsEmpty();
        AssertThat(Has(throughGap,new Vector2(562,-109))).OverrideFailureMessage("the shut gate's yard side is reached from the fence gap").IsTrue();
        var throughS1=From(Forecourt,true,true);
        AssertThat(inside.Where(p=>!Has(throughS1,p))).OverrideFailureMessage("S1 opened does not reach the yard").IsEmpty();
    }

    [TestCase]
    public void TheYardBoundariesCloseTheNorthAndEastAndTheTrackBed()
    {
        var reach=From(Forecourt,false);
        var outside=new[]{new Vector2(628,-160),new Vector2(630,-140),new Vector2(600,-161),new Vector2(561,-157),new Vector2(600,-165),new Vector2(430,-164.5f),new Vector2(500,-170)};
        AssertThat(outside.Where(p=>Has(reach,p))).OverrideFailureMessage($"the station leaks to {string.Join(", ",outside.Where(p=>Has(reach,p)))}").IsEmpty();
        AssertThat(World.StationBlocked(628,-160)).IsTrue();
        AssertThat(World.StationBlocked(561,-157)).IsTrue();
        AssertThat(World.StationBlocked(600,-161)).IsTrue();
        AssertThat(World.StationBlocked(430,-164.5f)).IsFalse();// the bed itself is clear; the S5 gate is what keeps it off the station
        AssertThat(World.StationBlocked(421,-164.5f)).IsTrue();
        AssertThat(World.S5Gate).IsEqual((new Vector2(421,-164.5f),5.2f));
        // The 1 m ground's edge is never reached, and the map's own fine area holds every reached cell.
        var edge=Cells(reach).Where(c=>new Vector2I[]{new(1,0),new(-1,0),new(0,1),new(0,-1)}.Any(d=>!World.IsFine(c+d))).ToArray();
        AssertThat(edge).OverrideFailureMessage($"reached the 1 m ground's edge at {string.Join(", ",edge.Take(5))}").IsEmpty();
        AssertThat(Cells(reach).All(c=>c.X>=366&&c.X<=636&&c.Y>=-200&&c.Y<=-20)).IsTrue();
    }

    [TestCase]
    public void EveryStationFloorReturnsToTheForecourtStarwell()
    {
        var forward=From(Forecourt,false);
        var home=WildsTest.Reach(new((int)Forecourt.X,(int)Forecourt.Y),(x,z)=>World.StationBlocked(x,z),back:true).Seen;
        var stuck=Cells(forward).Where(c=>c.X>366&&World.MazeEdge(c.X,c.Y)<0&&!home[c.X-Area.Position.X,c.Y-Area.Position.Y]).ToArray();
        AssertThat(stuck).OverrideFailureMessage($"{stuck.Length} station floor cells with no walk back to the Forecourt, e.g. {string.Join(", ",stuck.Take(5))}").IsEmpty();
        // The portal shelf and the old line's dead end are the farthest station floors and still return.
        AssertThat(Has(forward,new Vector2(377,-149.5f))&&Has(forward,new Vector2(418,-162))).IsTrue();
        AssertThat(home[(int)Mathf.Round(Portal.X)-Area.Position.X,(int)Mathf.Round(Portal.Y)-Area.Position.Y]).IsTrue();
    }

    [TestCase]
    public void TheTunnelConnectsTheTwoHalvesAndTheRidgeKeepsThemApart()
    {
        var court=World.Decks.Select(d=>(d.A,d.B)).Concat(World.Stairs.Select(s=>(s.Top,s.Foot))).ToArray();
        var maze=WildsTest.Reach(new((int)Court.X,(int)Court.Y),(x,z)=>false,court).Seen;
        bool At(bool[,] seen,Vector2 p)=>seen[(int)Mathf.Round(p.X)-Area.Position.X,(int)Mathf.Round(p.Y)-Area.Position.Y];
        AssertThat(new[]{Forecourt,Portal,new Vector2(600,-140)}.Where(p=>At(maze,p))).OverrideFailureMessage("the maze reaches the station without the tunnel").IsEmpty();
        var station=From(Portal,false);
        AssertThat(new[]{Court,Halt,new Vector2(347,-172),new Vector2(360,-172.5f)}.Where(p=>Has(station,p))).OverrideFailureMessage("the station reaches the maze without the tunnel").IsEmpty();
        // With the tunnel link, both halves meet: the Court reaches the Forecourt, and the station landing reaches the Court.
        var through=WildsTest.Reach(new((int)Court.X,(int)Court.Y),(x,z)=>false,court.Append(Tunnel));
        AssertThat(At(through.Seen,Forecourt)).OverrideFailureMessage("the tunnel link does not reach the Forecourt").IsTrue();
        AssertThat(At(through.Seen,Portal)).IsTrue();
        var stationLandingPoint=World.Doors.Single(d=>d.At.X<200).Landing!.Value;
        var back=WildsTest.Reach(new((int)Mathf.Round(stationLandingPoint.X),(int)Mathf.Round(stationLandingPoint.Y)),
            (x,z)=>World.StationBlocked(x,z),new[]{Tunnel}.Concat(court));
        AssertThat(At(back.Seen,Court)).OverrideFailureMessage("the tunnel link does not reach the Court from the station").IsTrue();
        // The ridge between the East yard's buried path and the portal keeps them apart on its own.
        AssertThat(World.MazeEdge(360,-172.5f)).IsLess(0);
        AssertThat(World.MazeEdge(353,-176)).IsLess(0);
    }

    [TestCase]
    public void TheIntendedWalkKeepsTheGraphDistances()
    {
        // The itinerary's own polyline, as the v9 drawing gives it: portal to the Forecourt, the long way to S1, then the door.
        float portalToStarwell=Polyline(FirstWalk.Take(14)),starwellToS1=Polyline(FirstWalk.Skip(13).Take(27)),s1ToDoor=Polyline(FirstWalk.Skip(39));
        AssertThat(Math.Abs(portalToStarwell/216.8f-1)).OverrideFailureMessage($"portal to the Forecourt: {portalToStarwell:F1} m").IsLess(.1f);
        AssertThat(Math.Abs(starwellToS1/317.09f-1)).OverrideFailureMessage($"the Forecourt to S1: {starwellToS1:F1} m").IsLess(.1f);
        AssertThat(Math.Abs((starwellToS1+s1ToDoor)/338.6f-1)).OverrideFailureMessage($"the Forecourt to the door: {starwellToS1+s1ToDoor:F1} m").IsLess(.1f);
        AssertThat(Math.Abs((portalToStarwell+starwellToS1+s1ToDoor)/555.4f-1)).OverrideFailureMessage($"the first walk: {portalToStarwell+starwellToS1+s1ToDoor:F1} m").IsLess(.1f);
        // Every waypoint stands on the station's own floor and clear of its footprints, so the built ground holds the route.
        var off=FirstWalk.Where(p=>World.MazeEdge(p.X,p.Y)>-.1f||World.StationBlocked(Mathf.RoundToInt(p.X),Mathf.RoundToInt(p.Y))).ToArray();
        AssertThat(off).OverrideFailureMessage($"off the built floor: {string.Join(", ",off)}").IsEmpty();
        float measured=Walk(Portal,Forecourt);
        AssertThat(Math.Abs(measured/216.8f-1)).OverrideFailureMessage($"the built portal to the Forecourt walk: {measured:F0} m").IsLess(.1f);
        // The gate is first touched on the yard's side; the drawn service road reaches only its outside, a dead end. Opened, S1 is the
        // drawn 112 m shortcut to the parcels door.
        float approach=Walk(Forecourt,new Vector2(562,-107));
        AssertThat(approach).OverrideFailureMessage($"the service road does not reach the gate's outside ({approach:F0} m)").IsLess(150);
        AssertThat(Has(From(Forecourt,false),new Vector2(562,-107))).OverrideFailureMessage("the gate's outside is reached by the service road").IsTrue();
        AssertThat(World.StationBlocked(562,-107)).IsFalse();
        AssertThat(World.StationBlocked(562,-108)).IsTrue();
        // All three real physical walks against the graph, measured on the built ground with the station's own footprints. The parcels bay's
        // fence encloses the door's approach to the station wall, so the closed door is entered from S1's yard side as the graph's S1 to door
        // edge has it; its south end stops 4 m short of S1 to open the mouth diagonally, and its return sits 2.5 m north of the door.
        float s1=Walk(Forecourt,new Vector2(562,-109));
        AssertThat(Math.Abs(s1/311f-1)).OverrideFailureMessage($"the physical Forecourt to S1's yard side: {s1:F0} m, the graph's 311 m").IsLess(.1f);
        float shortcutNow=Walk(Forecourt,DoorApproach,true);
        AssertThat(Math.Abs(shortcutNow/112f-1)).OverrideFailureMessage($"the physical open door: {shortcutNow:F0} m, the graph's 112 m").IsLess(.1f);
        float physicalDoor=Walk(Forecourt,DoorApproach);
        AssertThat(Math.Abs(physicalDoor/332f-1)).OverrideFailureMessage($"the physical closed parcels door: {physicalDoor:F0} m, the graph's 332 m").IsLess(.1f);
        AssertThat(physicalDoor).OverrideFailureMessage($"the closed parcels door is {physicalDoor:F0} m but S1 is {s1:F0} m: the door is no longer entered past the gate").IsGreater(s1);
        // The graph's own S1 to door edge, walked directly on the built ground, not a difference of two unrelated shortest paths.
        float direct=Walk(new Vector2(562,-109),DoorApproach);
        AssertThat(Math.Abs(direct/21f-1)).OverrideFailureMessage($"the direct S1 yard side to parcels approach: {direct:F1} m, the graph's 21 m").IsLess(.1f);
        // The source's first walk, ordered and obstacle-aware: the portal, lineman's path, Forecourt, garage, yard, the old signal room's key
        // point (618,-153.5) and S1's yard side, then the bay's adjusted mouth (559,-112) where the drawn straight return (561,-114) to the
        // door is now fenced, and the parcels approach. Every leg is walked on the built ground, in this order.
        var itinerary=FirstWalk.Take(40).Concat(new[]{BayMouth,DoorApproach}).ToArray();
        float walkedRoute=WalkPolyline(itinerary);
        AssertThat(walkedRoute/555.4f).OverrideFailureMessage($"the ordered first walk on the built ground: {walkedRoute:F0} m, the itinerary's 555 m").IsGreater(.9f).IsLess(1.1f);
        AssertThat(FirstWalk.Contains(SignalKeyPoint)).OverrideFailureMessage("the source's key point is not in the first walk").IsTrue();
        AssertThat(FirstWalk[39]).IsEqual(new Vector2(562,-109));
        AssertThat(FirstWalk[40]).IsEqual(new Vector2(561,-114));
        AssertThat(FirstWalk[41]).IsEqual(DoorApproach);
        AssertThat(World.StationBlocked(560,-123)).OverrideFailureMessage("the bay's fence no longer blocks the source's straight final approach").IsTrue();
        AssertThat(itinerary[40]).IsEqual(BayMouth);
        AssertThat(itinerary[41]).IsEqual(DoorApproach);
        // The parcels bay's facts: the fence's 19.5 m run, its return, the clear dock corridor, the closed direct north crossing, and the
        // future key door's 2.2 m proximity. The bay's only mouth is on S1's yard side; every reachable cell within the door's reach is
        // inside it, and every reachable position north of its return is beyond that reach.
        var bayFence=World.StationObstacles.Single(o=>o.Name=="Parcels bay fence");
        var bayReturn=World.StationObstacles.Single(o=>o.Name=="Parcels bay return fence");
        AssertThat(bayFence.Size).IsEqual(new Vector2(.6f,19.5f));
        AssertThat(bayFence.High).OverrideFailureMessage("the parcels bay's fence is low enough to jump").IsGreater(1.24f);
        AssertThat(bayReturn.At).IsEqual(new Vector2(559.1f,-132.5f));
        AssertThat(World.StationBlocked(560,-120)).OverrideFailureMessage("the parcels bay's fence is missing").IsTrue();
        AssertThat(World.StationBlocked(561,-120)||World.StationBlocked(559,-120)).OverrideFailureMessage("the parcels bay's fence closes the dock's corridor or the door's approach").IsFalse();
        AssertThat(World.StationBlocked(BayMouth.X,BayMouth.Y)).OverrideFailureMessage("the bay's adjusted mouth is blocked").IsFalse();
        AssertThat(Walk(new Vector2(561,-130),DoorApproach)).OverrideFailureMessage("the parcels door is still reached straight off the dock's north end").IsGreater(30);
        var reach=From(Forecourt,false);
        var doorAt=World.ParcelsDoor;
        AssertThat(Has(reach,DoorApproach)).OverrideFailureMessage("the parcels approach is not reached with S1 shut").IsTrue();
        float northFace=bayReturn.At.Y-bayReturn.Size.Y/2;
        var reachable=Cells(reach).Select(c=>new Vector2(c.X,c.Y)).ToArray();
        var near=reachable.Where(p=>p.DistanceTo(doorAt)<=2.2f).ToArray();
        AssertThat(near).OverrideFailureMessage("no position within the door's reach is reached with S1 shut").IsNotEmpty();
        AssertThat(near.All(p=>p.X<=BayMouth.X&&p.Y>=northFace)).OverrideFailureMessage($"a door-range position lies outside the bay: {string.Join(", ",near.Where(p=>p.X>BayMouth.X||p.Y<northFace))}").IsTrue();
        AssertThat(reachable.Where(p=>p.Y<=northFace).Select(p=>p.DistanceTo(doorAt)).DefaultIfEmpty(0).Min()).OverrideFailureMessage("a reachable position north of the bay's return is within the door's 2.2 m reach").IsGreater(2.2f);
        AssertThat(new Vector2(doorAt.X+.3f,northFace-.3f).DistanceTo(doorAt)).OverrideFailureMessage("the return's north face leaves a stance within the door's 2.2 m reach").IsGreater(2.2f);
        // The checkpoint legs either side of the unwalked tunnel, summed: never the tunnel's straight 244 m.
        var mazeLinks=World.Decks.Select(d=>(d.A,d.B)).Concat(World.Stairs.Select(s=>(s.Top,s.Foot))).ToArray();
        var mountainLanding=World.Doors.Single(d=>d.At.X>200).Landing!.Value;var stationLanding=World.Doors.Single(d=>d.At.X<200).Landing!.Value;
        float west=WildsTest.Walk(Court,mountainLanding,(x,z)=>false,mazeLinks),east=Walk(stationLanding,Forecourt);
        AssertThat(Math.Abs((west+east)/277f-1)).OverrideFailureMessage($"the Court to the Forecourt: {west:F0} + {east:F0} m").IsLess(.1f);
    }

    [TestCase]
    public void TheWarehouseBankPreservesTheSleeperAndWallsTheYard()
    {
        var post=new Vector2(619,-116);
        AssertThat(World.StationBlocked(post.X,post.Y)).OverrideFailureMessage("the v9 sleeper is buried").IsFalse();
        AssertThat(Math.Abs(World.Height(post.X,post.Y)-7.35f)).IsLess(.01f);
        AssertThat(World.MazeEdge(post.X,post.Y)).IsLess(0);
        var walls=new[]{"Warehouse bank west wall","Warehouse bank north wall west","Warehouse bank north wall east","Warehouse bank east wall","Loading dock wall",
            "Warehouse stair side west","Warehouse stair side east","Warehouse 2 west wall"};
        AssertThat(walls.Where(n=>!World.StationObstacles.Any(o=>o.Name==n))).IsEmpty();
        // The yard-facing stair climbs the bank's north face through its wall opening to Warehouse 2's north door: the warehouse interior is
        // the bank's one approach, and the apron west of it stays sealed, so the bank is not a route round the yard.
        var (bottom,top)=World.WarehouseStair;
        AssertThat(new[]{bottom,top}).ContainsExactly(new Vector2(611,-124),new Vector2(611,-122));
        AssertThat(Math.Abs(World.Height(bottom.X,bottom.Y)-6)<.01f&&Math.Abs(World.Height(611,-123)-6.675f)<.01f&&Math.Abs(World.Height(top.X,top.Y)-7.35f)<.01f).IsTrue();
        AssertThat(World.StationBlocked(611,-123)).OverrideFailureMessage("the stairwell is blocked").IsFalse();
        AssertThat(World.StationBlocked(609,-123)&&World.StationBlocked(613,-123)).OverrideFailureMessage("the stair's side walls are open").IsTrue();
        AssertThat(Has(From(Forecourt,false),post)).OverrideFailureMessage("the sleeping scout is cut off from the yard with S1 shut").IsTrue();
        AssertThat(Has(From(Forecourt,false,true),post)).OverrideFailureMessage("the sleeping scout is reached without the southeast gap").IsFalse();
        AssertThat(Has(From(Forecourt,false),new Vector2(580,-116))).OverrideFailureMessage("the bank's apron is a route round the yard").IsFalse();
        AssertThat(Walk(Forecourt,post)).IsLess(300);
    }

    [TestCase]
    public void TheDrawnWagonsAndSidingsAreInPlace()
    {
        var wagons=World.StationObstacles.Where(o=>o.Name.StartsWith("Wagon")).ToArray();
        AssertThat(wagons.Length).IsEqual(6);
        AssertThat(wagons.Select(w=>w.At)).ContainsExactly(new Vector2(577.5f,-128),new Vector2(601.5f,-128),
            new Vector2(579.5f,-142),new Vector2(603.5f,-142),new Vector2(583.5f,-149),new Vector2(604.5f,-149));
        AssertThat(wagons.All(w=>w.Size==new Vector2(11,3.2f)&&w.High==3.2f)).IsTrue();
        AssertThat(World.StationSidings.Length).IsEqual(5);
        AssertThat(World.StationSidings.Where(s=>s.Points[^1]==new Vector3(624,6,-138.5f)).Count()).IsEqual(4);
        AssertThat(World.StationSidings.Last().Points[^1]).IsEqual(new Vector3(634,6,-147));
    }

    [TestCase]
    public void ThePerimeterStandsAboveAJumpFromOutside()
    {
        foreach(var (name,outside) in new[]{("Yard east fence",new Vector2(629,-140)),("Yard north fence",new Vector2(600,-162)),("Yard south fence",new Vector2(600,-107))})
        {
            var o=World.StationObstacles.Single(o=>o.Name==name);var (_,high)=World.StationFootprint(o);
            AssertThat(high+o.High-World.Height(outside.X,outside.Y)).OverrideFailureMessage($"{name}: top {high+o.High:F2} m, outside ground {World.Height(outside.X,outside.Y):F2} m").IsGreater(1.24f);
        }
    }

    [TestCase]
    public void TheS2MallAndB2OpeningsStayShut()
    {
        var shut=new[]{new Vector2(496,-84),new Vector2(496,-100),new Vector2(524.5f,-80),new Vector2(575,-156.5f)};
        AssertThat(shut.Where(p=>!World.StationBlocked(p.X,p.Y))).OverrideFailureMessage($"not solid: {string.Join(", ",shut.Where(p=>!World.StationBlocked(p.X,p.Y)))}").IsEmpty();
        var reach=From(Forecourt,false);
        AssertThat(shut.Where(p=>Has(reach,p))).OverrideFailureMessage($"entered: {string.Join(", ",shut.Where(p=>Has(reach,p)))}").IsEmpty();
    }

    [TestCase]
    public void TheStationGroundHasNoStepsAtItsJoins()
    {
        float H(Vector2 p)=>World.Height(p.X,p.Y);
        float Jump(Vector2 a,Vector2 b){for(int i=0;i<10;i++){var m=(a+b)/2;if(Math.Abs(H(m)-H(a))>Math.Abs(H(b)-H(m)))b=m;else a=m;}return Math.Abs(H(b)-H(a));}
        IEnumerable<string> Scan(Vector2 from,Vector2 stride,int count)
        {float h=H(from);for(int i=0;i<count;i++){var a=from+stride*i;float next=H(a+stride);if(Math.Abs(next-h)>.2f&&World.MazeEdge(a.X,a.Y)<3&&Jump(a,a+stride)>.05f)yield return $"({a.X:F2}, {a.Y:F2})";h=next;}}
        var lines=new List<(Vector2 From,Vector2 Stride,int Count)>();
        // The joins where two level rows meet: the portal, forecourt, depot, arcade gaps, garage, open ground, yard rows, dock and shell rows.
        foreach(var (x,z) in new[]{new Vector2(377,-149.5f),new Vector2(438,-68),new Vector2(534,-69),new Vector2(539,-42),new Vector2(441,-41),new Vector2(480,-41),
            new Vector2(544,-38),new Vector2(566,-36),new Vector2(568,-36),new Vector2(624,-108),new Vector2(600,-122),new Vector2(600,-140),
            new Vector2(561,-128),new Vector2(561,-150),new Vector2(561,-108),new Vector2(430,-164.5f),new Vector2(500,-108),new Vector2(500,-132)})
        {
            lines.Add((new(x-3,z),new(.05f,0),120));lines.Add((new(x,z-3),new(0,.05f),120));
        }
        // And each trail's own centreline, where its floor and paint run.
        foreach(var trail in World.StationTrails)
            for(int i=0;i<trail.Points.Length-1;i++)
            {
                var a=new Vector2(trail.Points[i].X,trail.Points[i].Z);var b=new Vector2(trail.Points[i+1].X,trail.Points[i+1].Z);
                int n=(int)Math.Ceiling(a.DistanceTo(b)/.05f);lines.Add((a,(b-a)/Math.Max(1,n),n));
            }
        var steps=lines.AsParallel().SelectMany(l=>Scan(l.From,l.Stride,l.Count).ToList()).ToList();
        AssertThat(steps).OverrideFailureMessage($"{steps.Count} steps at station joins, e.g. {string.Join(", ",steps.Take(5))}").IsEmpty();
    }

    [TestCase]
    public void TheServiceRoadFenceKeepsTheDepotWay()
    {
        // v9 draws the service road's fence at station x=136, z -50…-12: world x 566, z -70…-108, on its east side, between it and the open
        // ground. Without it the road runs east along the outside of the yard's south fence straight to the SE gap.
        var fence=World.StationObstacles.SingleOrDefault(o=>o.Name=="Service road fence");
        AssertThat(fence).IsNotNull();
        AssertThat(fence!.At).IsEqual(new Vector2(566,-89));
        AssertThat(fence.Size).IsEqual(new Vector2(.8f,38));
        AssertThat(new[]{new Vector2(566,-70),new Vector2(566,-90),new Vector2(566,-107)}.Where(p=>!World.StationBlocked(p.X,p.Y))).IsEmpty();
        float bypass=Walk(new Vector2(562,-106),new Vector2(624,-106));
        AssertThat(bypass).OverrideFailureMessage($"the service road reaches the SE gap in {bypass:F0} m, straight along the yard's outside").IsGreater(120);
    }

    [TestCase]
    public void TheCraneIsApproachedOnTheGroundButTheCraneCellIsNoFloodEndpoint()
    {
        AssertThat(World.CraneCell).IsEqual(new Vector2(593.5f,-138.75f));
        AssertThat(World.CraneRampFoot).IsEqual(new Vector2(594,-126));
        AssertThat(World.CraneRampTop).IsEqual(new Vector2(594,-136.75f));
        float slope=Mathf.RadToDeg(Mathf.Atan2(World.CraneRampRise,World.CraneRampRun));
        AssertThat(slope).OverrideFailureMessage($"the crane ramp climbs {slope:F1} degrees").IsLess(40);
        AssertThat(World.MazeEdge(World.CraneRampFoot.X,World.CraneRampFoot.Y)).IsLess(0);
        AssertThat(Has(From(Forecourt,false),World.CraneRampFoot)).IsTrue();
        // The crane cell stands over the yard's reachable floor: the 2D flood must not treat that ground cell as the platform.
        AssertThat(World.CraneRise).IsGreater(5);
        AssertThat(Math.Abs(World.CraneRampFoot.DistanceTo(World.CraneRampTop)-World.CraneRampRun)).IsLess(.01f);
        AssertThat(World.StationObstacles.Any(o=>o.Name.Contains("Crane",StringComparison.OrdinalIgnoreCase))).IsFalse();
        AssertThat(World.StationObstacles.Count(o=>o.Name.StartsWith("Gantry leg"))).IsEqual(2);
    }

    [TestCase]
    public void TheStationSignsAndDoorLabelsAreTranslated()
    {
        static Dictionary<string,string> Ui(string lang)=>Localization.ParseStrings(File.ReadAllText(Repo.Path($"assets/dialogue/ui.{lang}.json")));
        foreach(var lang in new[]{"ja","zh_TW"})
        {
            var table=Ui(lang);
            AssertThat(table.ContainsKey(World.StationName)).OverrideFailureMessage($"{lang} lacks the station's name").IsTrue();
            AssertThat(table.ContainsKey(World.StationName.ToUpperInvariant())).OverrideFailureMessage($"{lang} lacks the drawn toast key").IsTrue();
            AssertThat(table.ContainsKey("STATION TUNNEL")).OverrideFailureMessage($"{lang} lacks the portal label").IsTrue();
            AssertThat(World.Doors.Where(d=>!table.ContainsKey(d.Label)).Select(d=>d.Label)).IsEmpty();
            AssertThat(World.RegionAt(new Vector3(500,0,-100)).ToUpperInvariant()).IsEqual(World.StationToast);
        }
        // The toast the HUD draws in the station is the uppercase key above; the station's sign carries the same words.
        AssertThat(World.StationName).IsEqual("Hoshikawa Station");
        AssertThat(World.StationToast).IsEqual("HOSHIKAWA STATION");
    }
}
