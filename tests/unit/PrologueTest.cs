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
    // The cast's lines are voiced, except Unvoiced scenes and the Unrecorded lines still waiting for a take, which must have no file;
    // a speaker outside the cast has no voice folder.
    [TestCase]
    public void TheCastsLinesAreVoicedOrListedAsUnrecorded()
    {
        var spoken=Beats().Where(b=>Prologue.Cast.Contains(b.Value.Speaker)&&!Prologue.Unvoiced.Contains(b.Key[..b.Key.LastIndexOf('.')])).ToList();
        bool Voiced(KeyValuePair<string,(string Speaker,string Text)> b)=>File.Exists(Repo.Path($"assets/audio/voice/ja/{b.Value.Speaker}/{b.Key}.mp3"));
        AssertThat(spoken.Where(b=>Voiced(b)==Prologue.Unrecorded.Contains(b.Key)).Select(b=>b.Key)).IsEmpty();
        AssertThat(Prologue.Unrecorded.Where(k=>!spoken.Any(b=>b.Key==k))).IsEmpty();
        AssertThat(Directory.GetDirectories(Repo.Path("assets/audio/voice/ja")).Select(Path.GetFileName).Where(s=>!Prologue.Cast.Contains(s!))).IsEmpty();
    }
    [TestCase]
    public void EverySpeakerWithAFaceHasAPortrait()
    {
        var speakers=Beats().Values.Select(b=>b.Speaker).Where(s=>s!=""&&s!="Zero"&&!Prologue.Offstage.Contains(s)).Distinct();
        AssertThat(speakers.Where(s=>!File.Exists(Repo.Path($"assets/art/portraits/{s.ToLowerInvariant().Replace("-","")}.png")))).IsEmpty();
    }
    // Night falls on narration that says so, or on a scene's first line when the whole scene is at night.
    [TestCase]
    public void NightFallsOnTheNarrationThatSaysSo()
    {
        AssertThat(Prologue.Nightfall.Where(n=>Prologue.Named(n.Key) is var scene&&(n.Value>=scene.Length||n.Value>0&&scene[n.Value].Speaker!="")).Select(n=>n.Key)).IsEmpty();
        AssertThat(Prologue.Homecoming[Prologue.Nightfall["Homecoming"]].Text).StartsWith("Night falls");
        AssertThat(Prologue.Evening[Prologue.Nightfall["Evening"]].Text).StartsWith("The sun goes down");
    }
    // A title card is a scene's narration, and the scene it opens goes on past it.
    [TestCase]
    public void DayCardsAreNarrationThatOpenTheirScenes()
    {
        var beats=Beats();
        AssertThat(Prologue.Cards.Where(c=>!beats.TryGetValue(c,out var b)||b.Speaker!=""||!c.EndsWith(".0")||!beats.ContainsKey(c[..^1]+"1"))).IsEmpty();
    }
    [TestCase]
    public void OffstageSpeakersHaveNoPortraitAndDoSpeak()
    {
        AssertThat(Prologue.Offstage.Where(s=>File.Exists(Repo.Path($"assets/art/portraits/{s.ToLowerInvariant()}.png")))).IsEmpty();
        AssertThat(Prologue.Offstage.Where(s=>!Beats().Values.Any(b=>b.Speaker==s))).IsEmpty();
    }
    // Only Mio and Chiyo know Zero's name, and Chiyo never lets on: everyone else calls it her terminal, and Mio says it only with nobody
    // else in the stretch. A scene's night falls before she walks home alone. 零件 is "parts" in Chinese, not the name.
    [TestCase]
    public void NobodyButMioCallsZeroByName()
    {
        var ja=Localization.ParseLines(File.ReadAllText(Repo.Path("assets/dialogue/ja.json")),"ja");var zh=Localization.ParseLines(File.ReadAllText(Repo.Path("assets/dialogue/zh_TW.json")),"zh_TW");
        var named=new List<string>();
        foreach(var (scene,name) in Prologue.SceneNames)
        {
            int cut=Prologue.Nightfall.GetValueOrDefault(name,scene.Length);
            foreach(var (from,to) in new[]{(0,cut),(cut,scene.Length)})
            {
                bool company=scene[from..to].Any(b=>b.Speaker is not ("" or "Mio" or "Zero"));
                for(int i=from;i<to;i++)
                {
                    string key=$"{name}.{i}";
                    bool says=scene[i].Text.Contains("Zero")||ja.GetValueOrDefault(key,"").Contains("ゼロ")||System.Text.RegularExpressions.Regex.IsMatch(zh.GetValueOrDefault(key,""),"零(?!件)");
                    if(says&&(company||scene[i].Speaker is not ("Mio" or "Zero")))named.Add(key);
                }
            }
        }
        AssertThat(named).IsEmpty();
    }
    [TestCase(3,"three")]
    [TestCase(1,"one")]
    [TestCase(0,"none")]
    [TestCase(7,"three")]
    [TestCase(-2,"none")]
    public void ZeroCountsTheRemainingPatrols(int left,string word)=>AssertThat(Prologue.RelayGuardedFor(left)[0].Text).Contains($"I count {word} still");
}
