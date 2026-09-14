using Godot;
namespace Hoshikawa;

public partial class ActorVisual : Node
{
    public AnimationPlayer? Animator { get; private set; }
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
            anim.LoopMode=key is "idle" or "walk" or "run" or "guard" ? Animation.LoopModeEnum.Linear : Animation.LoopModeEnum.None;
        }
    }
    public void Play(string clip,float speed=1,bool restart=false)
    {
        if(Animator==null||!clips.TryGetValue(clip,out var name))return;
        if(!restart&&Animator.CurrentAnimation==name&&Animator.IsPlaying())return;
        Animator.Play(name,.12,speed);
    }
    public bool HasClip(string name)=>clips.ContainsKey(name);
}
