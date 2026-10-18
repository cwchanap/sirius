using System.Globalization;
using System.Text.RegularExpressions;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;
namespace Hoshikawa.Tests;

[TestSuite]
public class TerrainTest
{
    static IEnumerable<float> Span(float from,float to,float step){for(float z=from;z>=to;z-=step)yield return z;}
    static Vector2[] Corners((string Name,Vector2 At,Vector3 Size,float Yaw,string Style) house)=>
        new[]{-1f,1f}.SelectMany(sx=>new[]{-1f,1f}.Select(sz=>house.At+new Vector2(sx*house.Size.X/2,sz*house.Size.Z/2).Rotated(-Mathf.DegToRad(house.Yaw)))).ToArray();
    [TestCase]
    public void HousesSitOnLevelPads()=>
        AssertThat(World.Hamlet.Where(h=>Corners(h).Any(c=>Math.Abs(World.Height(c.X,c.Y)-World.Height(h.At.X,h.At.Y))>=.05f)).Select(h=>h.Name)).IsEmpty();
    // Clear of where terrain.gdshader's road paint fades out (3.3 m) and of the brook channel's bank (2.3 m).
    [TestCase]
    public void HousesKeepOffTheRoadAndTheBrook()=>
        AssertThat(World.Hamlet.Where(h=>Corners(h).Append(h.At).Any(c=>Math.Abs(c.X-World.RoadX(c.Y))<3.3f||Math.Abs(c.X-World.BrookX(c.Y))<2.3f)).Select(h=>h.Name)).IsEmpty();
    [TestCase]
    public void BrookBedStaysUnderTheWater()=>
        AssertThat(Span(54,-3,.25f).Where(z=>World.Height(World.BrookX(z),z)>World.WaterLevel-.04f)).IsEmpty();
    [TestCase]
    public void RoadMatchesTheTerrainShader()
    {
        // terrain.gdshader paints the same road World.RoadX() places things along; the two must not drift apart.
        string shader=File.ReadAllText(Repo.Path("assets/shaders/terrain.gdshader")),n=@"(-?[\d.]+)";
        var m=Regex.Match(shader,$@"float road=sin\(p\.y\*{n}\)\*{n};\s*road=mix\(road,{n}\*sin\(\(-p\.y-{n}\)\*{n}\),smoothstep\({n},{n},-p\.y-{n}\)\);\s*road\*=1\.0-smoothstep\({n},{n},-p\.y-{n}\);");
        AssertThat(m.Success).OverrideFailureMessage("terrain.gdshader's road no longer has the shape this test reads; update both together").IsTrue();
        var k=m.Groups.Values.Skip(1).Select(g=>float.Parse(g.Value,CultureInfo.InvariantCulture)).ToArray();
        float Painted(float z)=>Mathf.Lerp(Mathf.Sin(z*k[0])*k[1],k[2]*Mathf.Sin((-z-k[3])*k[4]),Mathf.SmoothStep(k[5],k[6],-z-k[7]))*(1-Mathf.SmoothStep(k[8],k[9],-z-k[10]));
        AssertThat(Span(World.SouthEdge,World.NorthEdge,.5f).Where(z=>Math.Abs(Painted(z)-World.RoadX(z))>.01f)).IsEmpty();
    }
    [TestCase]
    public void RegionsRunSouthToNorthInOrder()
    {
        var regions=Span(World.SouthEdge,World.NorthEdge,1).Select(z=>World.RegionAt(new Vector3(0,0,z))).Aggregate(new List<string>(),(seen,r)=>{if(seen.LastOrDefault()!=r)seen.Add(r);return seen;});
        AssertThat(regions).ContainsExactly("Hoshikawa","Old Relay Road","Cedar Pass","Komorebi");
        AssertThat(World.RegionAt(World.KomorebiCenter)).IsEqual("Komorebi");
        AssertThat(World.Hamlet.Select(h=>World.RegionAt(new Vector3(h.At.X,0,h.At.Y))).Distinct()).ContainsExactly("Hoshikawa");
    }
    [TestCase]
    public void TripoResidentsHaveModelsAndLabelsAboveTheirHeads()
    {
        AssertThat(World.TripoResidents.Where(r=>!File.Exists(Repo.Path($"assets/models/{r.Value.Asset}.glb"))).Select(r=>r.Key)).IsEmpty();
        AssertThat(World.TripoResidents.Where(r=>r.Value.Label<=r.Value.Height).Select(r=>r.Key)).IsEmpty();
    }
}
