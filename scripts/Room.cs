using Godot;
namespace Hoshikawa;

// Mio's room, the prologue's first map: built when the opening takes her there and freed when she walks out of the door, while the
// valley is hidden. Its own frame has the tatami's surface at y 0 and the open shoji to the south (+Z), where the rain-wet paddies she
// smells are Codex's painting (view.gdshader); it stands high over her house so nothing of the valley's ground or colliders is near it.
public partial class World
{
    public static readonly Vector3 RoomAt=new(31,300,7.5f);
    // Mio's hips on the futon, her feet toward the shoji; the door out in the east wall; the photograph of her mother on the north wall.
    public static readonly Vector3 Bed=new(-.6f,0,-.2f),Door=new(1.6f,0,.65f),Photo=new(.9f,0,-1.3f);
    public static Node3D Room()
    {
        var room=new Node3D {Name="Mio's room",Position=RoomAt};
        // Codex's tiling art (assets/art/room/), laid triplanar in each box's own frame so it keeps its size on every face and box,
        // or on a model's own UVs in metres.
        static StandardMaterial3D Look(string art,float metres,Color tint,float glow=0,bool laid=false)
        {
            var texture=GD.Load<Texture2D>($"res://assets/art/room/{art}.png");
            return new() {AlbedoTexture=texture,AlbedoColor=tint,Uv1Triplanar=!laid,Uv1Scale=Vector3.One/metres,Roughness=.85f,EmissionEnabled=glow>0,Emission=Colors.Black,EmissionTexture=texture,EmissionEnergyMultiplier=glow};
        }
        // The cedar is the posts' dark wood; the boards are the same grain lifted.
        Material tatami=Look("tatami",.5f,Colors.White),edge=Mat(new("3d4a3c")),plaster=Look("plaster",1,Colors.White),cedar=Look("cedar",.625f,new(1.6f,1.6f,1.6f)),dark=Look("cedar",.625f,Colors.White),paper=Look("shoji",.8f,Colors.White,.15f);
        // Its grain runs up the texture, so a timber lying along X is a post turned on its side.
        MeshInstance3D Beam(string name,Vector3 p,Vector3 size,Material look,bool solid=false){var b=Box(room,name,p,new(size.Y,size.X,size.Z),look,solid);b.Rotation=new(0,0,Mathf.Pi/2);return b;}
        Box(room,"Tatami",new(0,-.05f,0),new(4,.1f,3.6f),tatami,true);
        foreach(float z in new[]{-.9f,.9f})Box(room,"Tatami edge",new(0,.003f,z),new(4,.008f,.06f),edge);
        Box(room,"Tatami edge",new(0,.003f,0),new(.06f,.008f,3.6f),edge);
        Beam("Engawa",new(0,-.07f,2.3f),new(4.4f,.1f,1),cedar,true);
        Beam("Engawa rail",new(0,.5f,2.76f),new(4.4f,.06f,.06f),dark);
        foreach(float x in new[]{-2.15f,-.7f,.7f,2.15f})Box(room,"Engawa post",new(x,.25f,2.76f),new(.08f,.56f,.08f),dark);
        // The rail, and the room's walls carried along the engawa's ends, keep Mio on the boards.
        foreach(var (p,size) in new[]{(new Vector3(0,1.4f,2.84f),new Vector3(4.6f,3,.1f)),(new Vector3(-2.25f,1.4f,2.3f),new Vector3(.1f,3,1.1f)),(new Vector3(2.25f,1.4f,2.3f),new Vector3(.1f,3,1.1f))})
        {var body=new StaticBody3D {Position=p,CollisionMask=0};room.AddChild(body);body.AddChild(new CollisionShape3D {Shape=new BoxShape3D {Size=size}});}
        Box(room,"North wall",new(0,1.2f,-1.85f),new(4.2f,2.4f,.1f),plaster,true);
        Box(room,"West wall",new(-2.05f,1.2f,0),new(.1f,2.4f,3.8f),plaster,true);
        Box(room,"East wall",new(2.05f,1.2f,-.875f),new(.1f,2.4f,2.05f),plaster,true);
        Box(room,"East wall",new(2.05f,1.2f,1.525f),new(.1f,2.4f,.75f),plaster,true);
        Box(room,"East wall",new(2.05f,2.15f,.65f),new(.1f,.5f,1),plaster,true);
        Box(room,"Door",new(2.02f,.95f,.65f),new(.05f,1.9f,1),dark,true);
        Box(room,"Door pull",new(1.99f,.95f,.3f),new(.02f,.14f,.05f),edge);
        Box(room,"Ceiling",new(0,2.45f,0),new(4.2f,.1f,3.8f),cedar,true);
        Beam("Beam",new(0,2.34f,0),new(4.1f,.14f,.16f),dark);
        foreach(float x in new[]{-2,2})foreach(float z in new[]{-1.8f,1.8f})Box(room,"Post",new(x,1.2f,z),new(.14f,2.4f,.14f),dark);
        Box(room,"Shoji lintel",new(0,2.15f,1.8f),new(4.2f,.5f,.12f),plaster);
        // The shoji stand slid open at either end of the south side.
        foreach(float x in new[]{-1.6f,1.6f})
        {
            Box(room,"Shoji",new(x,.95f,1.82f),new(.8f,1.9f,.03f),paper);
            foreach(float y in new[]{.02f,.5f,.95f,1.4f,1.88f})Beam("Shoji frame",new(x,y,1.8f),new(.82f,.04f,.05f),cedar);
            foreach(float dx in new[]{-.39f,-.13f,.13f,.39f})Box(room,"Shoji frame",new(x+dx,.95f,1.8f),new(.04f,1.9f,.05f),cedar);
        }
        // The bedding is Blender's (tools/build_bedding.py): the cover is cloth draped over her as she sleeps, with shape keys for her
        // sitting up under it and for folding it at the futon's foot, which Game.Cue() blends.
        var bedding=GD.Load<PackedScene>("res://assets/models/props/bedding.glb").Instantiate<Node3D>();
        foreach(var (part,look) in new[]{("Futon",Mat(new("dcd4c4"))),("Pillow",Mat(new("7f97a6"))),("Blanket",Look("futon",.5f,Colors.White,laid:true))})
            Mesh(room,part,((MeshInstance3D)bedding.FindChild(part)).Mesh,Bed,look);
        bedding.Free();
        // The low desk by the west wall, and on the floor the six tools and Renji's line telephone, opened up.
        Box(room,"Desk",new(-1.67f,.32f,-.9f),new(.55f,.05f,1),cedar,true);
        foreach(float x in new[]{-1.9f,-1.44f})foreach(float z in new[]{-1.35f,-.45f})Box(room,"Desk leg",new(x,.15f,z),new(.05f,.3f,.05f),dark);
        Cylinder(room,"Desk lamp",new(-1.75f,.45f,-1.2f),.05f,.22f,Mat(new("c8a35a"),.4f));
        Box(room,"Telephone",new(.35f,.06f,.05f),new(.22f,.12f,.18f),Mat(new("2f4a3a")));
        Box(room,"Telephone cover",new(.62f,.008f,-.12f),new(.22f,.012f,.18f),Mat(new("2f4a3a"))).Rotation=new(0,.5f,0);
        Box(room,"Handset",new(.2f,.03f,.32f),new(.2f,.05f,.05f),Mat(new("1c1c1c"))).Rotation=new(0,-.4f,0);
        Box(room,"Wire",new(.45f,.004f,.2f),new(.3f,.006f,.008f),Mat(new("b5452f"))).Rotation=new(0,.9f,0);
        Box(room,"Wire",new(.5f,.004f,.15f),new(.26f,.006f,.008f),Mat(new("d8b23a"))).Rotation=new(0,.3f,0);
        Cylinder(room,"Screwdriver",new(.05f,.012f,-.35f),.012f,.22f,Mat(new("b5452f"))).Rotation=new(0,.6f,Mathf.Pi/2);
        Cylinder(room,"Soldering iron",new(.75f,.015f,.45f),.015f,.26f,Mat(new("6b6f72"))).Rotation=new(0,-.8f,Mathf.Pi/2);
        Cylinder(room,"Wire spool",new(.85f,.02f,-.3f),.07f,.04f,Mat(new("b87333")));
        Cylinder(room,"Screw tin",new(.15f,.025f,.55f),.05f,.05f,Mat(new("9aa3a6")));
        Box(room,"Meter",new(.7f,.03f,.1f),new(.1f,.05f,.16f),Mat(new("d9b13a"))).Rotation=new(0,.2f,0);
        Box(room,"Pliers",new(.3f,.01f,-.55f),new(.16f,.015f,.03f),Mat(new("4f6a8a"))).Rotation=new(0,-.5f,0);
        Box(room,"Photo frame",new(Photo.X,1.45f,-1.79f),new(.32f,.4f,.03f),dark);
        Mesh(room,"Photo",new QuadMesh {Size=new(.22f,.33f)},new(Photo.X,1.45f,-1.772f),new StandardMaterial3D {AlbedoTexture=GD.Load<Texture2D>("res://assets/art/room/photo.png"),Roughness=.6f});
        // Outside, Codex's paddies after the rain on a box round the house, which the shader paints by direction; it must not shade the sun.
        var view=new ShaderMaterial {Shader=GD.Load<Shader>("res://assets/shaders/view.gdshader")};view.SetShaderParameter("view",GD.Load<Texture2D>("res://assets/art/room/view.png"));
        Mesh(room,"View",new BoxMesh {Size=new(24,20,26)},new(0,2,1),view).CastShadow=GeometryInstance3D.ShadowCastingSetting.Off;
        room.AddChild(new DirectionalLight3D {Name="Morning sun",RotationDegrees=new(-24,20,0),LightColor=new("ffe2b8"),LightEnergy=1.3f,ShadowEnabled=true,DirectionalShadowMaxDistance=30,ShadowBias=.04f});
        room.AddChild(new OmniLight3D {Name="Room light",Position=new(0,2,0),LightColor=new("ffe7c9"),LightEnergy=.55f,OmniRange=5.5f});
        return room;
    }
}
