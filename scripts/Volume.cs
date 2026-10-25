using Godot;
namespace Hoshikawa;

// The voice (dialogue lines and Mio's shouts) and the music each play on a mixer bus under Master; the sound effects stay on Master.
// A volume is a whole number of steps, 0 (silent) to Steps, which the settings screen's sliders set and settings.cfg keeps beside the language.
public static class Volume
{
    public static readonly string[] Buses={"Voice","Music"};
    public const int Steps=10;
    private const string Section="audio";
    private static readonly Dictionary<string,int> levels=Buses.ToDictionary(b=>b,_=>Steps);
    public static int Level(string bus)=>levels[bus];
    public static float Db(int level)=>level<=0?-80:Mathf.LinearToDb((float)level/Steps);
    // --self-test ignores the saved volumes, as it ignores the saved language, so a run never depends on the machine's settings.
    public static void Initialize(string[] args)
    {
        var config=new ConfigFile();config.Load(Localization.Settings);if(!args.Contains("--self-test"))Read(config);
        foreach(string bus in Buses)
        {
            if(AudioServer.GetBusIndex(bus)<0){AudioServer.AddBus();AudioServer.SetBusName(AudioServer.BusCount-1,bus);AudioServer.SetBusSend(AudioServer.BusCount-1,"Master");}
            Apply(bus);
        }
    }
    public static void Read(ConfigFile config){foreach(string bus in Buses)levels[bus]=Math.Clamp(config.GetValue(Section,bus,Steps).AsInt32(),0,Steps);}
    public static void Write(ConfigFile config){foreach(string bus in Buses)config.SetValue(Section,bus,levels[bus]);}
    // A player's choice from the settings screen; the self-test does not save.
    public static void Set(string bus,int level,bool save)
    {
        levels[bus]=Math.Clamp(level,0,Steps);Apply(bus);
        if(!save)return;
        var config=new ConfigFile();config.Load(Localization.Settings);Write(config);config.Save(Localization.Settings);
    }
    private static void Apply(string bus)=>AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex(bus),Db(levels[bus]));
}
