using Godot;
namespace Hoshikawa;

// Background music: every track loops on its own player and only the one Game wants is heard. The others fade out and pause
// where they are, so the relay road's music picks up again after a fight where it left off.
public partial class Music : Node
{
    public static readonly Dictionary<string,(string Path,float Db)> Tracks=new()
    {["valley"]=("res://assets/audio/valley.wav",-16),["relay"]=("res://assets/audio/relay.wav",-17),["battle"]=("res://assets/audio/battle.wav",-16)};
    // Seconds a crossfade takes, and how long the battle music outlasts the last robot still after Mio.
    public const float Fade=1.5f,Calm=3;
    public static string Track(string region,bool danger)=>danger?"battle":region=="Old Relay Road"?"relay":"valley";
    public string Current {get;private set;}="valley";
    public readonly Dictionary<string,AudioStreamPlayer> Players=new();
    private readonly Dictionary<string,float> level=new();
    public override void _Ready()
    {
        foreach(var (name,(path,_)) in Tracks){var player=new AudioStreamPlayer {Name=name,Stream=GD.Load<AudioStream>(path),VolumeDb=-80};AddChild(player);player.Finished+=()=>player.Play();Players[name]=player;level[name]=0;}
        Want(Current);
    }
    public void Want(string track){Current=track;var player=Players[track];if(player.StreamPaused)player.StreamPaused=false;else if(!player.Playing)player.Play();}
    public override void _Process(double delta)
    {
        foreach(var (name,player) in Players)
        {
            float target=name==Current?1:0;if(level[name]==target)continue;
            level[name]=Mathf.MoveToward(level[name],target,(float)delta/Fade);
            player.VolumeDb=Tracks[name].Db+Mathf.LinearToDb(Math.Max(level[name],.0001f));
            if(level[name]==0)player.StreamPaused=true;
        }
    }
}
