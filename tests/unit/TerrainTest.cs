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
    // Komorebi's ground is level without pads; its meeting hall closes the road's north end, the other houses keep off it.
    [TestCase]
    public void KomorebiHousesStandOnLevelGroundOffTheRoad()
    {
        AssertThat(World.KomorebiHouses.Where(h=>Corners(h).Any(c=>Math.Abs(World.Height(c.X,c.Y)-World.Height(h.At.X,h.At.Y))>=.05f)).Select(h=>h.Name)).IsEmpty();
        AssertThat(World.KomorebiHouses.Where(h=>h.Style!="hall"&&Corners(h).Append(h.At).Any(c=>Math.Abs(c.X-World.RoadX(c.Y))<3.3f)).Select(h=>h.Name)).IsEmpty();
    }
    [TestCase]
    public void BrookBedStaysUnderTheWater()=>
        AssertThat(Span(54,-3,.25f).Where(z=>World.Height(World.BrookX(z),z)>World.WaterLevel-.04f)).IsEmpty();
    // Each paddy is one level from 30 cm inside its edge, where the aze's inner foot meets it.
    [TestCase]
    public void PaddyFloorsAreLevel()=>
        AssertThat(World.Paddies.Where(p=>Enumerable.Range(0,81).Select(i=>World.Height(p.Position.X+.3f+(p.Size.X-.6f)*(i%9)/8,p.Position.Y+.3f+(p.Size.Y-.6f)*(i/9)/8))
            .Any(h=>Math.Abs(h-World.Height(p.GetCenter().X,p.GetCenter().Y))>.001f))).IsEmpty();
    // terrain.gdshader paints the road from World.RoadGap() alone, which runs it unbroken from the shrine to Komorebi's hall.
    [TestCase]
    public void RoadPaintRunsTheWholeRoute()
    {
        AssertThat(File.ReadAllText(Repo.Path("assets/shaders/terrain.gdshader"))).Contains("smoothstep(2.0,3.3,UV2.y)");
        float hall=World.KomorebiHouses.Single(h=>h.Style=="hall").At.Y;
        var bare=Span(55,hall+6,1).Where(z=>Enumerable.Range(-64,129).All(x=>World.RoadGap(x,z)>1)).ToArray();
        AssertThat(bare).OverrideFailureMessage($"no road paint at z {string.Join(", ",bare)}").IsEmpty();
    }
    [TestCase]
    public void RegionsRunSouthToNorthInOrder()
    {
        var regions=Span(World.SouthEdge,World.NorthEdge,1).Select(z=>World.RegionAt(new Vector3(0,0,z))).Aggregate(new List<string>(),(seen,r)=>{if(seen.LastOrDefault()!=r)seen.Add(r);return seen;});
        AssertThat(regions).ContainsExactly("Hoshikawa","Old Relay Road","Cedar Pass","Komorebi");
        AssertThat(World.RegionAt(World.KomorebiCenter)).IsEqual("Komorebi");
        AssertThat(World.Hamlet.Select(h=>World.RegionAt(new Vector3(h.At.X,0,h.At.Y))).Distinct()).ContainsExactly("Hoshikawa");
        AssertThat(World.KomorebiHouses.Select(h=>World.RegionAt(new Vector3(h.At.X,0,h.At.Y))).Distinct()).ContainsExactly("Komorebi");
    }
    [TestCase]
    public void TripoResidentsHaveModelsAndLabelsAboveTheirHeads()
    {
        AssertThat(World.TripoResidents.Where(r=>!File.Exists(Repo.Path($"assets/models/{r.Value.Asset}.glb"))).Select(r=>r.Key)).IsEmpty();
        AssertThat(World.TripoResidents.Where(r=>r.Value.Label<=r.Value.Height).Select(r=>r.Key)).IsEmpty();
    }
}
