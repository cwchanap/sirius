using GdUnit4;
using Godot;
using static GdUnit4.Assertions;
namespace Hoshikawa.Tests;

// The occupied city is a distant silhouette on the north-west ridge, beyond Tunnel Mountain and the station: nine boxes on the far
// ground, clear of the maze and its overlooks, the opening's sky and city shots turned to it together with their framing and glide kept,
// and the scene camera's far reaching it where the old 320 m clipped it.
[TestSuite]
public class CityTest
{
    static readonly Vector2 City=World.City;
    static readonly Vector2 OpeningEye=new(40,24),Portal=World.StationPortal,ValleyOverlook=new(29,-226),StationOverlook=new(336.6f,-121.6f);
    // Compass bearings: 0 is north (-z), 90 east, so the portal's bearing to the city is the source's 120 degrees left of east.
    static float Bearing(Vector2 eye,Vector2 at)=>Mathf.PosMod(Mathf.RadToDeg(Mathf.Atan2(at.X-eye.X,eye.Y-at.Y)),360);
    static float ShotBearing(Vector3 eye,Vector3 at)=>Bearing(new Vector2(eye.X,eye.Z),new Vector2(at.X,at.Z));
    static float Pitch(Vector3 eye,Vector3 at)=>Mathf.RadToDeg(Mathf.Atan2(at.Y-eye.Y,new Vector2(at.X-eye.X,at.Z-eye.Z).Length()));
    static float LeftOfEast(float bearing)=>Mathf.PosMod(90-bearing,360);
    static World.Obstacle? Station(string name)=>World.StationObstacles.FirstOrDefault(o=>o.Name==name);

    [TestCase]
    public void TheCitySilhouettesStandOnTheNorthWestRidgeBeyondTheStation()
    {
        AssertThat(City).IsEqual(new Vector2(270,-332));
        // Nine 2 x 3 m columns three metres apart, 18, 24 or 30 m tall, each based 3 m under the far ground it stands on.
        var boxes=Enumerable.Range(0,9).Select(World.CityBox).ToArray();
        var sizes=Enumerable.Range(0,9).Select(World.CitySize).ToArray();
        AssertThat(boxes.Select(b=>b.X)).ContainsExactly(Enumerable.Range(0,9).Select(i=>258f+i*3).ToArray());
        AssertThat(boxes.All(b=>b.Z==City.Y)).IsTrue();
        AssertThat(sizes.Select(s=>s.Y)).ContainsExactly(new[]{18f,24,30,18,24,30,18,24,30});
        AssertThat(Enumerable.Range(0,9).All(i=>Math.Abs(boxes[i].Y-sizes[i].Y/2-(World.Height(boxes[i].X,boxes[i].Z)-3))<.001f)).OverrideFailureMessage("each column's base is 3 m under its own ground").IsTrue();
        // The far 4 m ground, north of MazeFine and west of the station: the ridge beyond both, not the site the station took over.
        AssertThat(World.InPlay(City.X,City.Y)||World.IsFine(new Vector2I((int)City.X,(int)City.Y))).IsFalse();
        AssertThat(City.Y).IsLess(World.MazeFine.Position.Y);
        AssertThat(City.X+12).IsLess(World.StationFine.Position.X);
        AssertThat(World.Far.HasPoint(new Vector2I((int)City.X,(int)City.Y))).IsTrue();
        // Clear of every maze trail, and 100 m from where the old eastern-ridge silhouette stood (x 392..416, z -120).
        AssertThat(Enumerable.Range(0,9).Min(i=>World.MazeEdge(boxes[i].X,boxes[i].Z))).IsGreater(20);
        AssertThat(Enumerable.Range(0,9).All(i=>Math.Abs(boxes[i].Z+120)>100&&boxes[i].X<292)).IsTrue();
        // The source's distances and bearings: the opening eye, the east portal, the Valley Overlook and the Station overlook.
        AssertThat(OpeningEye.DistanceTo(City)).IsEqualApprox(423.8f,.1f);
        AssertThat(Portal.DistanceTo(City)).IsEqualApprox(211.6f,.1f);
        AssertThat(ValleyOverlook.DistanceTo(City)).IsEqualApprox(263.3f,.1f);
        AssertThat(StationOverlook.DistanceTo(City)).IsEqualApprox(220.7f,.1f);
        AssertThat(Bearing(OpeningEye,City)).IsEqualApprox(32.9f,.1f);
        AssertThat(Bearing(Portal,City)).IsEqualApprox(329.6f,.1f);
        AssertThat(LeftOfEast(Bearing(Portal,City))).IsEqualApprox(120.4f,.1f);
        // The station's own mass spans 83..141 degrees from the portal: the city is over 100 degrees from its centre, so no forward view holds both.
        var station=Station("Station building")!;
        var stationBearing=Bearing(Portal,station.At);
        float apart=Mathf.Min(Mathf.PosMod(Bearing(Portal,City)-stationBearing,360),360-Mathf.PosMod(Bearing(Portal,City)-stationBearing,360));
        AssertThat(apart).OverrideFailureMessage($"city {Bearing(Portal,City):F1} deg, station {stationBearing:F1} deg from the portal").IsGreater(100);
        AssertThat(apart).IsLess(180);
    }

    [TestCase]
    public void TheOpeningShotsTurnToTheCityAndKeepTheirFraming()
    {
        var (skyEye,skyAt,skyGlide,skyRoom)=Game.Shots["sky"];
        var (cityEye,cityAt,cityGlide,cityRoom)=Game.Shots["city"];
        AssertThat(skyEye).IsEqual(new Vector3(40,44,24));
        AssertThat(cityEye).IsEqual(new Vector3(40,36,24));
        AssertThat(skyGlide).IsEqual(0f);
        AssertThat(cityGlide).IsEqual(7f);
        AssertThat(skyRoom||cityRoom).IsFalse();
        // Both look on the city's bearing: 32.87 degrees from the opening eye, no longer the old 39.8 degrees at (160,*,-120).
        AssertThat(ShotBearing(skyEye,skyAt)).IsEqualApprox(Bearing(OpeningEye,City),.01f);
        AssertThat(ShotBearing(cityEye,cityAt)).IsEqualApprox(Bearing(OpeningEye,City),.01f);
        AssertThat(Math.Abs(skyAt.X-cityAt.X)<.001f&&Math.Abs(skyAt.Z-cityAt.Z)<.001f).IsTrue();
        // Each keeps its old vertical framing and 187.446 m look-at range: the sky 56 m up over the old (160,*,-120), the city 14 m down.
        AssertThat(Pitch(skyEye,skyAt)).IsEqualApprox(Mathf.RadToDeg(Mathf.Atan2(56,new Vector2(120,144).Length())),.01f);
        AssertThat(Pitch(cityEye,cityAt)).IsEqualApprox(Mathf.RadToDeg(Mathf.Atan2(-14,new Vector2(120,144).Length())),.01f);
        AssertThat(new Vector2(skyAt.X-skyEye.X,skyAt.Z-skyEye.Z).Length()).IsEqualApprox(new Vector2(120,144).Length(),.01f);
        AssertThat(new Vector2(cityAt.X-cityEye.X,cityAt.Z-cityEye.Z).Length()).IsEqualApprox(new Vector2(120,144).Length(),.01f);
        // The scene camera's one 500 m far reaches the whole row; the old 320 m clipped it on the shot's forward depth. These corner
        // projections are a flat estimate; the engine self-test checks the built merged skyline against the real camera frustum.
        AssertThat(Game.SceneFar).IsEqual(500f);
        var dir=new Vector2(City.X-OpeningEye.X,City.Y-OpeningEye.Y).Normalized();
        float Forward(Vector2 p)=>new Vector2(p.X-OpeningEye.X,p.Y-OpeningEye.Y).Dot(dir);
        foreach(var corner in new[]{new Vector2(258,-330.5f),new Vector2(282,-333.5f)})AssertThat(Forward(corner)).IsGreater(320).IsLess(Game.SceneFar);
        // From the opening eye the bare far ground stays under the line to the city's own base, so the silhouette is not hidden by the ridge.
        float worst=-1e9f;
        for(int i=1;i<300;i++)
        {
            float t=i/300f;var p=OpeningEye.Lerp(City,t);float line=Mathf.Lerp(36,World.Height(City.X,City.Y),t);
            worst=Mathf.Max(worst,World.Height(p.X,p.Y)-line);
        }
        AssertThat(worst).OverrideFailureMessage($"terrain rises {worst:F2} m over the opening's line to the city's base").IsLess(0);
    }
}
