using GdUnit4;
using Godot;
using static GdUnit4.Assertions;
namespace Hoshikawa.Tests;

[TestSuite]
public class MusicTest
{
    // Only the relay road has music of its own; a fight overrides every region's.
    [TestCase]
    public void EachRegionAndAFightPickTheirTrack()
    {
        foreach(var (z,track) in new[]{(30f,"valley"),(-100f,"relay"),(-210f,"valley"),(-250f,"valley")})
            AssertThat(Music.Track(World.RegionAt(new Vector3(0,0,z)),false)).IsEqual(track);
        AssertThat(Music.Track("Old Relay Road",true)).IsEqual("battle");
        AssertThat(Music.Track("Hoshikawa",true)).IsEqual("battle");
    }
    [TestCase]
    public void EveryTrackIsInTheRepository()
    {
        foreach(var (path,_) in Music.Tracks.Values)AssertThat(File.Exists(Repo.Path(path["res://".Length..]))).IsTrue();
    }
}
