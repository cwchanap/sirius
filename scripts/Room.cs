using Godot;
namespace Hoshikawa;

// Mio's room, the prologue's first map: built when the opening takes her there and freed when she walks out of the door, while the
// valley is hidden. Its own frame has the tatami's surface at y 0 and the open shoji to the south (+Z), over a garden closed by a hedge,
// past which the rain-wet paddies she smells are Codex's painting (view.gdshader); it stands high over her house so nothing of the
// valley's ground or colliders is near it.
public partial class World
{
    public static readonly Vector3 RoomAt=new(31,300,7.5f);
    // Mio's hips on the futon, her feet toward the shoji; the door out in the east wall; the photograph of her mother on the north wall.
    public static readonly Vector3 Bed=new(-.6f,0,-.2f),Door=new(1.6f,0,.65f),Photo=new(.9f,0,-1.3f);
    public Node3D Room()
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
        // The low desk by the west wall with its lamp, and on the floor Renji's line telephone, opened up, among her tools: Hapadona's
        // Tripo Studio models (assets/models/), placed whole.
        Box(room,"Desk",new(-1.67f,.32f,-.9f),new(.55f,.05f,1),cedar,true);
        foreach(float x in new[]{-1.9f,-1.44f})foreach(float z in new[]{-1.35f,-.45f})Box(room,"Desk leg",new(x,.15f,z),new(.05f,.3f,.05f),dark);
        foreach(var (name,model,p,yaw) in new[]{("Desk lamp","desk_lamp",new Vector3(-1.75f,.345f,-1.15f),0f),("Telephone","telephone",new Vector3(.4f,0,.05f),-25f),
            ("Tools","tools",new Vector3(.45f,0,-.5f),0f),("Wire spool","spool",new Vector3(.78f,0,.32f),30f)})
        {var n=GD.Load<PackedScene>($"res://assets/models/{model}.glb").Instantiate<Node3D>();n.Name=name;n.Position=p;n.RotationDegrees=new(0,yaw,0);room.AddChild(n);}
        Box(room,"Photo frame",new(Photo.X,1.45f,-1.79f),new(.32f,.4f,.03f),dark);
        Mesh(room,"Photo",new QuadMesh {Size=new(.22f,.33f)},new(Photo.X,1.45f,-1.772f),new StandardMaterial3D {AlbedoTexture=GD.Load<Texture2D>("res://assets/art/room/photo.png"),Roughness=.6f});
        // The house stands on posts and stones half a metre over its garden: lawn on the valley's terrain shader, bare earth under the
        // eaves and along a path from the step stone, hedged at the sides and ending in a terrace's stone edge, past which the painted
        // paddies lie below it.
        const float lawn=-.5f;
        Material shade=Mat(new("2e2924")),granite=stone;
        Box(room,"Underfloor",new(0,(lawn-.1f)/2,0),new(4.15f,-.1f-lawn,3.75f),shade);
        foreach(float x in new[]{-2.15f,-.7f,.7f,2.15f})
        {Box(room,"Engawa foot",new(x,(lawn-.12f)/2,2.7f),new(.08f,-.12f-lawn,.08f),dark);Box(room,"Foundation stone",new(x,lawn+.04f,2.7f),new(.22f,.08f,.22f),granite);}
        Box(room,"Step stone",new(0,lawn+.09f,3.15f),new(.7f,.18f,.42f),granite);
        foreach(var (x,z) in new[]{(.1f,4.05f),(-.08f,4.85f)})Box(room,"Stepping stone",new(x,lawn+.02f,z),new(.46f,.05f,.38f),granite);
        foreach(float x in new[]{-6,6})Box(room,"Terrace edge",new(x,lawn-.08f,5.6f),new(11,.3f,.3f),granite);
        var eave=Box(room,"Eave",new(0,2.47f,2.65f),new(4.9f,.06f,1.9f),cedar,true);eave.Rotation=new(.14f,0,0);
        var st=new SurfaceTool();st.Begin(Godot.Mesh.PrimitiveType.Triangles);
        // UV2.y is the shader's road gap, earth under 2 and lawn over 3.3.
        void G(float x,float z)
        {
            float eaves=new Vector2(Math.Max(0,Math.Abs(x)-2.3f),Math.Max(0,Math.Max(-2-z,z-2.85f))).Length(),path=z>2.85f?Math.Max(0,Math.Abs(x)-.25f):9;
            st.SetNormal(Vector3.Up);st.SetUV2(new(9,2+1.3f*Math.Min(1,Math.Min(eaves/.8f,path/.3f))));st.AddVertex(new(x,lawn,z));
        }
        for(float z=-6;z<5.5f;z+=.5f)for(float x=-11.5f;x<11.5f;x+=.5f){G(x+.5f,z);G(x+.5f,z+.5f);G(x,z);G(x,z);G(x+.5f,z+.5f);G(x,z+.5f);}
        var earth=new ShaderMaterial{Shader=GD.Load<Shader>("res://assets/shaders/terrain.gdshader")};earth.SetShaderParameter("atlas",GD.Load<Texture2D>("res://assets/art/material-atlas.png"));
        Mesh(room,"Garden",st.Commit(),Vector3.Zero,earth);
        var leaves=new RandomNumberGenerator {Seed=4409};
        void Plant(float x,float z,float scale,string species="shrub"){var t=Tree(new(x,lawn,z),scale*leaves.RandfRange(.85f,1.15f),species,leaves);RemoveChild(t);t.Name=$"{species} {room.GetChildCount()}";room.AddChild(t);}
        foreach(float x in new[]{-10.6f,10.6f})for(float z=-1;z<5.2f;z+=1.2f)Plant(x,z,1.3f);
        foreach(float x in new[]{-3.4f,3.4f})Plant(x,3.6f,.9f);
        Plant(7.4f,3.6f,1,"kaki");
        // Past the hedge, Codex's paddies after the rain on a box round the house, which the shader paints by direction; it must not shade the sun.
        var view=new ShaderMaterial {Shader=GD.Load<Shader>("res://assets/shaders/view.gdshader")};view.SetShaderParameter("view",GD.Load<Texture2D>("res://assets/art/room/view.png"));
        Mesh(room,"View",new BoxMesh {Size=new(24,20,26)},new(0,2,1),view).CastShadow=GeometryInstance3D.ShadowCastingSetting.Off;
        room.AddChild(new DirectionalLight3D {Name="Morning sun",RotationDegrees=new(-24,20,0),LightColor=new("ffe2b8"),LightEnergy=1.3f,ShadowEnabled=true,DirectionalShadowMaxDistance=30,ShadowBias=.04f});
        room.AddChild(new OmniLight3D {Name="Room light",Position=new(0,2,0),LightColor=new("ffe7c9"),LightEnergy=.55f,OmniRange=5.5f});
        return room;
    }
}
