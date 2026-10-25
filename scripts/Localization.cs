using Godot;
using System.Text.Json;
namespace Hoshikawa;

// The game's text in English, Japanese and Traditional Chinese. Code, quest state, speaker and scene names stay English: only what is drawn is translated.
// UI text is keyed by its English wording (assets/dialogue/ui.<lang>.json) and also registered with the TranslationServer, so Label3D and Button follow a
// language switch by themselves. Dialogue is keyed by scene position, "Intro.1" (assets/dialogue/<lang>.json); the Japanese voice plays under every language.
public static class Localization
{
    public static readonly (string Code,string Name)[] Languages={("en","English"),("ja","日本語"),("zh_TW","繁體中文")};
    public static string Current="en";
    // English UI strings looked up under a language that has no entry for them. IntegrationCheck requires it to stay empty.
    public static readonly HashSet<string> Missing=new();
    private static readonly Dictionary<string,Dictionary<string,string>> lines=new(),strings=new();
    private static readonly Dictionary<string,FontFile[]> faces=new();
    public static Dictionary<string,string> ParseLines(string json,string lang)
    {
        using var doc=JsonDocument.Parse(json);var result=new Dictionary<string,string>();
        foreach(var line in doc.RootElement.EnumerateObject())
            if(line.Value.TryGetProperty(lang,out var text)&&text.GetString() is {Length:>0} s)result[line.Name]=s;
        return result;
    }
    public static Dictionary<string,string> ParseStrings(string json)=>JsonSerializer.Deserialize<Dictionary<string,string>>(json)!;
    public static string FromLocale(string locale)=>locale.StartsWith("ja")?"ja":locale.StartsWith("zh")?"zh_TW":"en";
    // --lang= beats the saved choice, which beats the operating system. The self-test stays English unless asked otherwise.
    public static string Choose(IEnumerable<string> args,string? saved,string osLocale)
    {
        var flag=args.FirstOrDefault(a=>a.StartsWith("--lang="))?[7..];
        if(flag!=null&&Languages.Any(l=>l.Code==flag))return flag;
        if(args.Contains("--self-test"))return "en";
        return saved!=null&&Languages.Any(l=>l.Code==saved)?saved:FromLocale(osLocale);
    }
    public const string Settings="user://settings.cfg";
    public static void Initialize(string[] args)
    {
        foreach(var (code,_) in Languages.Skip(1))
        {
            lines[code]=ParseLines(Read($"res://assets/dialogue/{code}.json"),code);strings[code]=ParseStrings(Read($"res://assets/dialogue/ui.{code}.json"));
            var translation=new Translation {Locale=code};
            foreach(var (english,text) in strings[code])translation.AddMessage(english,text);
            TranslationServer.AddTranslation(translation);
            faces[code]=new[]{GD.Load<FontFile>($"res://assets/fonts/NotoSans{Suffix(code)}-Regular.otf"),GD.Load<FontFile>($"res://assets/fonts/NotoSerif{Suffix(code)}-Regular.otf")};
        }
        var config=new ConfigFile();config.Load(Settings);
        Set(Choose(args,config.GetValue("game","language","").AsString() is {Length:>0} s?s:null,OS.GetLocale()));
    }
    private static string Suffix(string code)=>code=="ja"?"JP":"TC";
    private static string Read(string path)
    {
        using var file=Godot.FileAccess.Open(path,Godot.FileAccess.ModeFlags.Read);
        return file?.GetAsText()??throw new InvalidOperationException("cannot read "+path);
    }
    public static void Set(string code)
    {
        Current=code;TranslationServer.SetLocale(code);
        ThemeDB.FallbackFont.Fallbacks=faces.TryGetValue(code,out var f)?new(){f[0]}:new();
    }
    // A player's choice from the menu; --lang= does not touch the saved one.
    public static void Save()
    {
        var config=new ConfigFile();config.Load(Settings);config.SetValue("game","language",Current);config.Save(Settings);
    }
    // The CJK serif face for the title and name plates, which the Hud draws in Georgia or Noto Serif where those exist.
    public static Godot.Collections.Array<Font> SerifFallbacks()=>faces.TryGetValue(Current,out var f)?new(){f[1]}:new();
    // The language switcher shows each name in its own script whatever language is current.
    public static FontFile? NameFont(string code)=>faces.TryGetValue(code,out var f)?f[0]:null;
    // Typing speed in characters per second: a CJK character carries about two letters' worth of reading.
    public static float RevealSpeed=>Current=="en"?52:28;
    public static string Line(string? scene,int beat,string english)
        =>scene!=null&&lines.TryGetValue(Current,out var table)&&table.TryGetValue($"{scene}.{beat}",out var text)?text:english;
    public static string T(string english)
    {
        if(Current=="en"||english=="")return english;
        if(strings[Current].TryGetValue(english,out var text))return text;
        Missing.Add(english);return english;
    }
    public static bool IsTranslated(string english,string code)=>strings.TryGetValue(code,out var table)&&table.ContainsKey(english);
    public static string F(string template,params object[] args)=>string.Format(T(template),args);
}
