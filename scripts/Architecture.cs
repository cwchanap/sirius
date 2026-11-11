using Godot;
namespace Hoshikawa;

// Japanese rural buildings for Hoshikawa: the Tripo houses World.Hamlet places.
public partial class World
{
    private ShaderMaterial VillageAtlas(Vector2 cell,Vector2 repeats,Color? tint=null)
    {
        var m=Atlas(cell,repeats,"res://assets/art/village-atlas.png");if(tint is Color t)m.SetShaderParameter("tint",t);return m;
    }
    // Every Hoshikawa house is a Tripo model facing its own +X, so each turns a quarter to face its table's +Z. At is where the model's
    // origin stands, Walls the footprint its walls box fills (the front wall on its far edge) and Sign the shop sign's centre, its Z out
    // from that wall; the table's size.Y is the eave the sign's cords reach. The hall's porch steps and the clinic's engawa reach 2.1 m
    // out, past the pad's 1.6 m apron, so those two walls sit back from the table's front edge to keep every foot vertex on level ground.
    public static readonly Dictionary<string,(Vector2 At,Rect2 Walls,Vector3 Sign)> TripoHouses=new()
    {
        ["workshop"]=(new(-.02f,.3f),new(-5.83f,-2.89f,9.44f,6.39f),new(-.8f,3,.25f)),
        ["kura"]=(new(0,.025f),new(-3.1f,-1.835f,6.2f,3.67f),new(0,2.7f,.5f)),
        ["hall"]=(new(0,.22f),new(-5.41f,-3.7f,10.82f,6.75f),new(0,2.75f,.25f)),
        ["teahouse"]=(new(0,.23f),new(-4.85f,-3,9.7f,6.5f),new(0,3.2f,.25f)),
        ["clinic"]=(new(.05f,.18f),new(-5,-2.84f,10,4.94f),new(0,3,.25f)),
        ["farmhouse"]=(new(0,.05f),new(-5.45f,-3.55f,10.9f,7.1f),new(-1.2f,2.5f,.25f)),
    };
    // A Tripo house of the given style standing whole at `at`, its front to the frame's +Z: its model, a camera stop over all of it, its own
    // faces below the eaves as walls, and a box filling them to the eave.
    private Node3D TripoHouse(string name,Vector2 at,float eave,float yaw,string style)
    {
        var b=new Node3D{Name=name,Position=new(at.X,Height(at.X,at.Y),at.Y),RotationDegrees=new(0,yaw,0)};AddChild(b);
        var (to,walls,_)=TripoHouses[style];
        var mesh=ActorVisual.Descendants(Model(b,"Tripo "+style,style,new(to.X,0,to.Y),-90,new(1.3f,1.3f,1.3f))).OfType<MeshInstance3D>().Single();
        var shell=mesh.Mesh.CreateTrimeshShape();shell.BackfaceCollision=true;CameraStop(mesh,shell);
        // The player meets the model's own walls, posts, decks and steps below the eaves (not its ground faces), and a box fills the walls so doorways stay shut.
        var local=b.GlobalTransform.AffineInverse()*mesh.GlobalTransform;var faces=mesh.Mesh.GetFaces();var low=new List<Vector3>();
        for(int i=0;i<faces.Length;i+=3){var ys=faces[i..(i+3)].Select(v=>(local*v).Y).ToArray();if(ys.Min()<2.4f&&ys.Max()>.05f)low.AddRange(faces[i..(i+3)]);}
        var solid=new StaticBody3D();mesh.AddChild(solid);solid.AddChild(new CollisionShape3D{Shape=new ConcavePolygonShape3D{Data=low.ToArray(),BackfaceCollision=true}});
        var body=new StaticBody3D();b.AddChild(body);
        body.AddChild(new CollisionShape3D{Position=new(walls.GetCenter().X,eave/2,walls.GetCenter().Y),Shape=new BoxShape3D{Size=new(walls.Size.X,eave,walls.Size.Y)}});
        return b;
    }
    public Node3D Minka(string name,Vector2 at,Vector3 size,float yaw,string style)
    {
        var b=TripoHouse(name,at,size.Y,yaw,style);Buildings.Add(b);
        var (_,walls,sign)=TripoHouses[style];float eave=size.Y,front=walls.End.Y;
        var timber=Mat(new("3f3026"));var hang=new Vector3(sign.X,sign.Y,front+sign.Z);
        ShopSign(b,name,hang);
        foreach(float x in new[]{-1.1f,1.1f})Box(b,"Sign cord",new(sign.X+x,(sign.Y+.21f+eave)/2,hang.Z),new(.025f,eave-sign.Y-.21f,.025f),timber);
        if(style=="teahouse")
        {
            // The chochin hangs from the eave beside the sign, in front of the transom; a bench and a nodate umbrella stand out front.
            // Both are Tripo's: the bench's own middle stands .42 m to the model's -X of its origin, the parasol through its +X end.
            Model(b,"Chochin lantern","chochin",new(1.9f,2.6f,front+.35f),0,new(1.3f,1.3f,1.3f));
            Box(b,"Lantern cord",new(1.9f,(3.21f+eave)/2,front+.35f),new(.02f,eave-3.21f,.02f),timber);
            Model(b,"Tea bench","tea_bench",new(2.62f,0,front+1.5f),0,new(1.3f,1.3f,1.3f));
            var seat=new StaticBody3D{Position=new(2.2f,.25f,front+1.5f)};b.AddChild(seat);seat.AddChild(new CollisionShape3D{Shape=new BoxShape3D{Size=new(1.86f,.5f,.9f)}});
        }
        if(style=="clinic")
        {
            // The medic banner hangs just under the engawa's eave beam (3.16 m) at one end; herbs dry by the gable at the other.
            Box(b,"Medic banner",new(4.3f,2.37f,front+1.16f),new(.7f,1.54f,.04f),Mat(new("edf0d8")));
            Box(b,"Medic cross vertical",new(4.3f,2.5f,front+1.19f),new(.14f,.62f,.03f),Mat(new("638d75")));
            Box(b,"Medic cross horizontal",new(4.3f,2.5f,front+1.19f),new(.45f,.14f,.03f),Mat(new("638d75")));
            float hx=walls.Position.X-.75f,hz=walls.GetCenter().Y;
            Box(b,"Herb rack rail",new(hx,1.9f,hz),new(.08f,.08f,2.4f),timber);
            foreach(float z in new[]{-1.2f,1.2f})Box(b,"Herb rack post",new(hx,.95f,hz+z),new(.1f,1.9f,.1f),timber);
            for(int i=0;i<6;i++)Box(b,"Drying herbs",new(hx,1.62f,hz-1+i*.4f),new(.14f,.45f,.14f),Mat(new(i%2==0?"7f9a55":"a4a06a")));
        }
        return b;
    }
    private void ShopSign(Node3D b,string name,Vector3 at)
    {
        Box(b,"Hanging shop sign",at,new(2.6f,.42f,.08f),Mat(new("3f3026")));
        var sign=Label(b,name.ToUpperInvariant(),at+new Vector3(0,0,.06f),Gold,23);sign.Billboard=BaseMaterial3D.BillboardModeEnum.Disabled;sign.PixelSize=.0043f;
    }
}
