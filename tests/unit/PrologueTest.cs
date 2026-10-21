using GdUnit4;
using static GdUnit4.Assertions;
namespace Hoshikawa.Tests;

[TestSuite]
public class PrologueTest
{
    // Every scene keyed the way voice files and the dialogue files name a line: "Intro.1", "FirstMeeting.Chiyo.2".
    // A resident's repeated first line shares its key with the first beat of the full scene, so the same key may appear twice with the same beat.
    internal static Dictionary<string,(string Speaker,string Text)> Beats()
    {
        var beats=new Dictionary<string,(string Speaker,string Text)>();
        foreach(var scene in Prologue.SceneNames)for(int i=0;i<scene.Key.Length;i++)beats[$"{scene.Value}.{i}"]=scene.Key[i];
        return beats;
    }
    [TestCase]
    public void EveryVoiceFileIsSpokenByItsSpeaker()
    {
        var beats=Beats();var files=Directory.GetFiles(Repo.Path("assets/audio/voice/ja"),"*.mp3",SearchOption.AllDirectories);
        AssertThat(files).IsNotEmpty();
        AssertThat(files.Select(f=>(Speaker:System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(f))!,Key:System.IO.Path.GetFileNameWithoutExtension(f)))
            .Where(f=>!beats.TryGetValue(f.Key,out var beat)||beat.Speaker!=f.Speaker).Select(f=>$"{f.Speaker}/{f.Key}.mp3")).IsEmpty();
    }
    [TestCase]
    public void EverySpeakerWithAFaceHasAPortrait()
    {
        var speakers=Beats().Values.Select(b=>b.Speaker).Where(s=>s!=""&&s!="Zero").Distinct();
        AssertThat(speakers.Where(s=>!File.Exists(Repo.Path($"assets/art/portraits/{s.ToLowerInvariant().Replace("-","")}.png")))).IsEmpty();
    }
    [TestCase]
    public void NightFallsOnTheNarrationThatSaysSo()
    {
        var (speaker,text)=Prologue.Homecoming[Prologue.NightFalls];
        AssertThat(speaker).IsEmpty();AssertThat(text).StartsWith("Night falls");
    }
    [TestCase(3,"three")]
    [TestCase(1,"one")]
    [TestCase(0,"none")]
    [TestCase(7,"three")]
    [TestCase(-2,"none")]
    public void ZeroCountsTheRemainingPatrols(int left,string word)=>AssertThat(Prologue.RelayGuardedFor(left)[0].Text).Contains($"I count {word} still");
}
