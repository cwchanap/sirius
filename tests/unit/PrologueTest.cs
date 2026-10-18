using System.Text.Json;
using GdUnit4;
using static GdUnit4.Assertions;
namespace Hoshikawa.Tests;

[TestSuite]
public class PrologueTest
{
    // Every multi-beat scene keyed the way voice files and ja.json name a line: "Intro.1", "FirstMeeting.Chiyo.2".
    static Dictionary<string,(string Speaker,string Text)> Beats()=>
        Prologue.SceneNames.SelectMany(s=>s.Key.Select((beat,i)=>(Key:$"{s.Value}.{i}",beat))).ToDictionary(b=>b.Key,b=>b.beat);
    [TestCase]
    public void EveryVoiceFileIsSpokenByItsSpeaker()
    {
        var beats=Beats();var files=Directory.GetFiles(Repo.Path("assets/audio/voice/ja"),"*.mp3",SearchOption.AllDirectories);
        AssertThat(files).IsNotEmpty();
        AssertThat(files.Select(f=>(Speaker:System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(f))!,Key:System.IO.Path.GetFileNameWithoutExtension(f)))
            .Where(f=>!beats.TryGetValue(f.Key,out var beat)||beat.Speaker!=f.Speaker).Select(f=>$"{f.Speaker}/{f.Key}.mp3")).IsEmpty();
    }
    [TestCase]
    public void JapaneseLinesStillMatchTheEnglishScript()
    {
        var beats=Beats();using var doc=JsonDocument.Parse(File.ReadAllText(Repo.Path("assets/dialogue/ja.json")));
        AssertThat(doc.RootElement.EnumerateObject().Where(l=>!beats.TryGetValue(l.Name,out var beat)||beat.Speaker!=l.Value.GetProperty("speaker").GetString()
            ||beat.Text!=l.Value.GetProperty("en").GetString()).Select(l=>l.Name)).IsEmpty();
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
    public void ZeroCountsTheRemainingPatrols(int left,string word)=>AssertThat(Prologue.RelayGuarded(left)).Contains($"I count {word} still");
}
