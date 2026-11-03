using Godot;
namespace Hoshikawa;

public partial class ActorVisual : Node
{
    public AnimationPlayer? Animator { get; private set; }
    // The clip last asked for, as Play() names it.
    public string Clip { get; private set; } = "";
    private readonly Dictionary<string,string> clips=new();
    public static IEnumerable<Node> Descendants(Node root)
    {
        foreach(var child in root.GetChildren()) {yield return child;foreach(var next in Descendants(child))yield return next;}
    }
    public void Bind(Node3D model)
    {
        Animator=Descendants(model).OfType<AnimationPlayer>().FirstOrDefault();
        if(Animator==null) return;
        foreach(string name in Animator.GetAnimationList())
        {
            string key=name.Split('/').Last().Split('.').First();
            clips.TryAdd(key,name);
            var anim=Animator.GetAnimation(name);
            anim.LoopMode=key is "idle" or "walk" or "run" or "guard" or "sleep" or "sit" or "work" || key.EndsWith("_hold") ? Animation.LoopModeEnum.Linear : Animation.LoopModeEnum.None;
        }
    }
    // A gesture with a <clip>_hold loop (tools/cast_clips.py) goes on into it, breathing in the pose it ends on instead of freezing.
    public void Play(string clip,float speed=1,bool restart=false,float blend=.12f)
    {
        if(Animator==null||!clips.TryGetValue(clip,out var name))return;
        Clip=clip;
        bool held=clips.TryGetValue(clip+"_hold",out var hold);
        if(!restart&&(Animator.CurrentAnimation==name||held&&Animator.CurrentAnimation==hold)&&Animator.IsPlaying())return;
        Animator.ClearQueue();
        Animator.Play(name,blend,speed);
        if(held)Animator.Queue(hold);
    }
    public bool HasClip(string name)=>clips.ContainsKey(name);
}
