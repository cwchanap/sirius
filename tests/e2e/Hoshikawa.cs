using System.Text.Json;
using GodotE2E;
namespace Hoshikawa.E2E;

// The game as godot-e2e sees it: its own headless Godot process, reached through node paths and input events.
static class Hoshikawa
{
    public const string Root="/root/Hoshikawa",Mio=Root+"/Mio";
    public const int Title=0,Playing=1,Dialogue=2,Paused=3;
    const long Enter=4194309,Escape=4194305,E=69;
    static readonly string Project=FindProject(AppContext.BaseDirectory);
    public static E2ELaunchOptions Options()
    {
        // A failed test leaves a screenshot under test_output/; keep Godot from importing it into the project.
        Directory.CreateDirectory(Path.Combine(Project,"test_output"));File.WriteAllText(Path.Combine(Project,"test_output",".gdignore"),"");
        return new E2ELaunchOptions{ScenePath="res://scenes/Main.tscn",ProjectPath=Project,ExtraGodotArgs=["--headless","--fixed-fps","60"],Timeout=TimeSpan.FromSeconds(90)};
    }
    public static Task<int> Mode(E2EGame game)=>Get<int>(game,Root,"Mode");
    public static async Task<T> Get<T>(E2EGame game,string path,string property)=>(await game.GetPropertyAsync<T>(path,property))!;
    public static async Task<(double X,double Y,double Z)> Position(E2EGame game,string path)
    {var v=(await Send(game,"get_property",("path",path),("property","global_position"))).GetProperty("result");return (v.GetProperty("x").GetDouble(),v.GetProperty("y").GetDouble(),v.GetProperty("z").GetDouble());}
    public static async Task<string> Find(E2EGame game,string name)=>(await Send(game,"query_nodes",("pattern",name))).GetProperty("nodes")[0].GetString()!;
    public static Task Place(E2EGame game,string path,double x,double y,double z)=>
        game.SetPropertyAsync(path,"global_position",JsonSerializer.SerializeToElement(new Dictionary<string,object>{["_t"]="v3",["x"]=x,["y"]=y,["z"]=z}));
    public static Task Frames(E2EGame game,int count)=>Send(game,"wait_physics_frames",("count",count));
    public static Task PressEnter(E2EGame game)=>Key(game,Enter,false);
    public static Task PressEscape(E2EGame game)=>Key(game,Escape,false);
    public static Task PressE(E2EGame game)=>Key(game,E,true);
    // The title screen is skipped the way a player does it: Enter begins the journey, Escape skips the intro.
    public static async Task StartPlaying(E2EGame game)
    {
        await PressEnter(game);await game.WaitForPropertyAsync(Root,"Mode",Dialogue);
        await PressEscape(game);await game.WaitForPropertyAsync(Root,"Mode",Playing);
    }
    static async Task Key(E2EGame game,long keycode,bool physical)
    {foreach(bool pressed in new[]{true,false})await Send(game,"input_key",("keycode",keycode),("physical",physical),("pressed",pressed));}
    // The raw command seam, for what the typed facade does not cover: vectors, key events, frame waits and node queries.
    static async Task<JsonElement> Send(E2EGame game,string action,params (string Key,object Value)[] args)
    {
        var result=await game.SendCommandAsync(action,args.ToDictionary(a=>a.Key,a=>JsonSerializer.SerializeToElement(a.Value)));
        return result.Success?result.Value:throw new E2EException($"{action} failed: {result.Message}");
    }
    static string FindProject(string from)
    {
        for(var dir=new DirectoryInfo(from);dir!=null;dir=dir.Parent)if(File.Exists(Path.Combine(dir.FullName,"project.godot")))return dir.FullName;
        throw new InvalidOperationException("no project.godot above "+from);
    }
}
