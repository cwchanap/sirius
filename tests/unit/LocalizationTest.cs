using System.Text.Json;
using System.Text.RegularExpressions;
using GdUnit4;
using static GdUnit4.Assertions;
namespace Hoshikawa.Tests;

[TestSuite]
public class LocalizationTest
{
    static Dictionary<string,string> Ui(string lang)=>Localization.ParseStrings(File.ReadAllText(Repo.Path($"assets/dialogue/ui.{lang}.json")));
    static string Dialogue(string lang)=>File.ReadAllText(Repo.Path($"assets/dialogue/{lang}.json"));
    // Every string literal in the game's code, as the string it spells.
    static HashSet<string> Literals()=>Directory.GetFiles(Repo.Path("scripts"),"*.cs").Where(f=>!f.EndsWith("IntegrationCheck.cs"))
        .SelectMany(f=>Regex.Matches(File.ReadAllText(f),"\"((?:[^\"\\\\]|\\\\.)*)\"").Select(m=>m.Groups[1].Value.Replace("\\n","\n").Replace("\\\"","\"").Replace("\\\\","\\"))).SelectMany(l=>new[]{l,l.ToUpperInvariant()}).ToHashSet();
    // The text of each call that puts a string on screen: translated UI text goes through T/F, Toast, Feedback, labels and residents.
    static readonly Regex Calls=new(@"(?<![\w.])(?:T|F|Toast|Feedback|FloatingNumber|Label|Signpost|Resident|House)\(|marker\.Text=");
    static IEnumerable<string> Shown()
    {
        foreach(var file in Directory.GetFiles(Repo.Path("scripts"),"*.cs").Where(f=>!f.EndsWith("IntegrationCheck.cs")&&!f.EndsWith("Localization.cs")))
        {
            string source=File.ReadAllText(file);
            foreach(Match call in Calls.Matches(source))
            {
                int depth=call.Value.EndsWith("(")?1:0,i=call.Index+call.Length;string args;
                for(;i<source.Length&&(depth>0||call.Value.EndsWith("=")&&source[i]!=';');i++)
                {
                    char c=source[i];
                    if(c=='"')for(i++;source[i]!='"';i++){if(source[i]=='\\')i++;}
                    else if(c=='\'')for(i+=source[i+1]=='\\'?3:2;source[i]!='\'';i++){}
                    else if(c=='(')depth++;else if(c==')')depth--;
                }
                args=source[(call.Index+call.Length)..i];
                foreach(Match lit in Regex.Matches(args,"\"((?:[^\"\\\\]|\\\\.)*)\""))
                {
                    string before=args[..lit.Index].TrimEnd(),after=args[(lit.Index+lit.Length)..].TrimStart(),text=lit.Groups[1].Value.Replace("\\n","\n");
                    if(before.EndsWith("==")||before.EndsWith("new(")||after.StartsWith("=>")||after.StartsWith("==")||!text.Any(char.IsLetter))continue;
                    // Building names are drawn in capitals on their signs.
                    yield return call.Value.StartsWith("House")?text.ToUpperInvariant():text;
                }
            }
        }
        foreach(Match name in Regex.Matches(File.ReadAllText(Repo.Path("scripts/World.cs")),@"\(""([A-Z][^""]+)"",new\(-?\d"))yield return name.Groups[1].Value.ToUpperInvariant();
    }
    [TestCase("ja")]
    [TestCase("zh_TW")]
    public void EveryStringTheCodeShowsHasATranslation(string lang)
    {
        var shown=Shown().Distinct().ToList();AssertThat(shown.Count).IsGreater(80);
        AssertThat(string.Join(" | ",shown.Where(s=>!Ui(lang).ContainsKey(s)))).IsEmpty();
    }
    [TestCase("ja")]
    [TestCase("zh_TW")]
    public void EveryBeatIsTranslatedAndStillMatchesTheEnglishScript(string lang)
    {
        var beats=PrologueTest.Beats();using var doc=JsonDocument.Parse(Dialogue(lang));
        AssertThat(doc.RootElement.EnumerateObject().Where(l=>!beats.TryGetValue(l.Name,out var beat)||beat.Speaker!=l.Value.GetProperty("speaker").GetString()
            ||beat.Text!=l.Value.GetProperty("en").GetString()).Select(l=>l.Name)).IsEmpty();
        var lines=Localization.ParseLines(Dialogue(lang),lang);
        AssertThat(beats.Keys.Where(k=>!lines.ContainsKey(k))).IsEmpty();
    }
    [TestCase]
    public void BothLanguagesTranslateTheSameStrings()
    {
        AssertThat(Ui("ja").Keys.OrderBy(k=>k)).IsEqual(Ui("zh_TW").Keys.OrderBy(k=>k));
        AssertThat(Localization.ParseLines(Dialogue("ja"),"ja").Keys.OrderBy(k=>k)).IsEqual(Localization.ParseLines(Dialogue("zh_TW"),"zh_TW").Keys.OrderBy(k=>k));
    }
    [TestCase("ja")]
    [TestCase("zh_TW")]
    public void TranslationsKeepTheirFormatPlaceholders(string lang)
    {
        static string Holes(string s)=>string.Join(",",Regex.Matches(s,@"\{\d+(:[^}]*)?\}").Select(m=>m.Value).OrderBy(h=>h));
        AssertThat(Ui(lang).Where(p=>p.Value==""||Holes(p.Key)!=Holes(p.Value)).Select(p=>p.Key)).IsEmpty();
    }
    [TestCase("ja")]
    [TestCase("zh_TW")]
    public void NoTranslationIsLeftOverFromRemovedCode(string lang)
    {
        var literals=Literals();
        AssertThat(Ui(lang).Keys.Where(k=>!literals.Contains(k))).IsEmpty();
    }
    [TestCase]
    public void LanguageComesFromTheFlagThenTheSavedChoiceThenTheSystem()
    {
        AssertThat(Localization.Choose(new[]{"--lang=ja"},"zh_TW","en_US")).IsEqual("ja");
        AssertThat(Localization.Choose(new string[0],"zh_TW","ja_JP")).IsEqual("zh_TW");
        AssertThat(Localization.Choose(new string[0],null,"ja_JP")).IsEqual("ja");
        AssertThat(Localization.Choose(new string[0],null,"zh_HK")).IsEqual("zh_TW");
        AssertThat(Localization.Choose(new string[0],null,"fr_FR")).IsEqual("en");
        AssertThat(Localization.Choose(new string[0],"klingon","en_US")).IsEqual("en");
        AssertThat(Localization.Choose(new[]{"--lang=klingon"},"ja","en_US")).IsEqual("ja");
    }
    [TestCase]
    public void TheSelfTestStaysEnglishUnlessAskedOtherwise()
    {
        AssertThat(Localization.Choose(new[]{"--self-test"},"ja","ja_JP")).IsEqual("en");
        AssertThat(Localization.Choose(new[]{"--self-test","--lang=zh_TW"},null,"en_US")).IsEqual("zh_TW");
    }
    [TestCase]
    public void ParsingSkipsLinesWithoutTheLanguage()
    {
        var lines=Localization.ParseLines("{\"A.0\":{\"speaker\":\"Mio\",\"en\":\"x\",\"ja\":\"い\"},\"A.1\":{\"speaker\":\"Mio\",\"en\":\"y\"},\"A.2\":{\"speaker\":\"Mio\",\"en\":\"z\",\"ja\":\"\"}}","ja");
        AssertThat(lines.Keys).IsEqual(new[]{"A.0"});AssertThat(lines["A.0"]).IsEqual("い");
    }
}
