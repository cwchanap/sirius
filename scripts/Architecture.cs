using Godot;
namespace Hoshikawa;

// Japanese rural buildings for Hoshikawa: a curved-roof generator (the shrine's) and the Tripo houses World.Hamlet places.
public partial class World
{
    private ShaderMaterial VillageAtlas(Vector2 cell,Vector2 repeats,Color? tint=null)
    {
        var m=Atlas(cell,repeats,"res://assets/art/village-atlas.png");if(tint is Color t)m.SetShaderParameter("tint",t);return m;
    }
    // Top-surface height in the roof's frame: ridge along local X, underside meeting y=0 on the wall line.
    // Hip ends (irimoya/thatch, |x| beyond the tsuma line) are a separate face, so `hip` picks which surface to sample.
    private static float RoofY(string kind,float w,float d,float rise,float overhang,float thickness,float x,float z,bool hip=false)
    {
        float hz=d/2+overhang,sag=kind=="thatch"?0:.12f*rise;
        float Q(float e){float t=1-Math.Min(e,hz)/hz;return rise*(1-t)-sag*4*t*(1-t);}
        float a=hz-Math.Abs(z),b=w/2+overhang-Math.Abs(x),y=hip?Q(b):Q(a);
        if(kind!="thatch")y+=.25f*(1-Mathf.SmoothStep(0,1.2f,Math.Min(a,b)))*(1-Mathf.SmoothStep(0,2.5f,Math.Max(a,b)));
        return y-(Q(overhang)-thickness);
    }
    private static void Tri(SurfaceTool st,(Vector3 P,Vector3 N,Vector2 UV) a,(Vector3 P,Vector3 N,Vector2 UV) b,(Vector3 P,Vector3 N,Vector2 UV) c)
    {
        if((c.P-a.P).Cross(b.P-a.P).Dot(a.N+b.N+c.N)<0)(b,c)=(c,b);
        foreach(var v in new[]{a,b,c}){st.SetNormal(v.N);st.SetUV(v.UV);st.AddVertex(v.P);}
    }
    public MeshInstance3D Roof(Node3D parent,string name,float w,float d,float rise,float overhang,string kind,Material top,float thickness,Material? gable=null)
    {
        float hx=w/2+overhang,hz=d/2+overhang,b0=kind=="gable"?0:.45f*hz,uvs=.55f,slope=Mathf.Sqrt(1+rise*rise/(hz*hz));
        float Y(float x,float z,bool hip=false)=>RoofY(kind,w,d,rise,overhang,thickness,x,z,hip);
        var sts=new[]{new SurfaceTool(),new SurfaceTool(),new SurfaceTool()};foreach(var s in sts)s.Begin(Godot.Mesh.PrimitiveType.Triangles);
        void Patch(int ns,int nu,Func<float,float,Vector3> f,Func<float,float,Vector2> uv)
        {
            (Vector3,Vector3,Vector2) V(float s,float u,bool under)
            {
                const float e=.002f;
                var n=(f(s,Math.Min(u+e,1))-f(s,Math.Max(u-e,-1))).Cross(f(Math.Min(s+e,1),u)-f(Math.Max(s-e,0),u)).Normalized();
                if((n.Y<0)!=under)n=-n;
                return (f(s,u)+(under?Vector3.Down*thickness:Vector3.Zero),n,uv(s,u));
            }
            for(int i=0;i<ns;i++)for(int j=0;j<nu;j++)
            {
                float s0=(float)i/ns,s1=(float)(i+1)/ns,u0=-1+2f*j/nu,u1=-1+2f*(j+1)/nu;
                foreach(bool under in new[]{false,true})
                {
                    var st=sts[under?1:0];
                    Tri(st,V(s0,u0,under),V(s1,u0,under),V(s1,u1,under));Tri(st,V(s0,u0,under),V(s1,u1,under),V(s0,u1,under));
                }
            }
        }
        void Strip(int k,Vector3[] upper,Vector3[] lower,Vector3 outward,bool both)
        {
            for(int i=0;i+1<upper.Length;i++)foreach(var n in both?new[]{outward,-outward}:new[]{outward})
            {
                Vector2 U(Vector3 p)=>new((p.X+p.Z)*.5f,-p.Y*.5f);
                Tri(sts[k],(upper[i],n,U(upper[i])),(lower[i],n,U(lower[i])),(lower[i+1],n,U(lower[i+1])));
                Tri(sts[k],(upper[i],n,U(upper[i])),(lower[i+1],n,U(lower[i+1])),(upper[i+1],n,U(upper[i+1])));
            }
        }
        Vector3[] Line(int n,Func<float,Vector3> f)=>Enumerable.Range(0,n+1).Select(i=>f((float)i/n)).ToArray();
        Vector3[] Drop(Vector3[] line,float by)=>line.Select(p=>p+Vector3.Down*by).ToArray();
        int ns=(int)Math.Ceiling(hz/.35f),nu=(int)Math.Ceiling(2*hx/.5f);
        foreach(float sz in new[]{-1f,1f})
        {
            float X(float a)=>hx-Math.Min(a,b0);
            Vector3 F(float s,float u){float a=s*hz,x=u*X(a),z=sz*(hz-a);return new(x,Y(x,z),z);}
            Patch(ns,nu,F,(s,u)=>new(u*X(s*hz)*uvs*sz,(1-s)*hz*slope*uvs));
            var eave=Line(nu,t=>F(0,-1+2*t));Strip(1,eave,Drop(eave,thickness),new(0,0,sz),false);
            foreach(float su in new[]{-1f,1f})
            {
                var verge=Line(ns,t=>F(Mathf.Lerp(kind=="gable"?0:b0/hz,1,t),su));Strip(1,verge,Drop(verge,thickness),new(su,0,0),false);
            }
        }
        if(kind!="gable")foreach(float sx in new[]{-1f,1f})
        {
            int nb=(int)Math.Ceiling(b0/.35f),nz=(int)Math.Ceiling(2*hz/.5f);
            Vector3 H(float s,float u){float b=s*b0,x=sx*(hx-b),z=u*(hz-b);return new(x,Y(x,z,true),z);}
            Patch(nb,nz,H,(s,u)=>new(u*(hz-s*b0)*uvs*sx,(hz-s*b0)*slope*uvs));
            var eave=Line(nz,t=>H(0,-1+2*t));Strip(1,eave,Drop(eave,thickness),new(sx,0,0),false);
            // Tsuma: the vertical gable triangle standing on the hip, recessed under the upper gable's verge.
            float xt=sx*(hx-b0-.12f),zt=hz-b0-.12f;
            var upper=Line(12,t=>{float z=Mathf.Lerp(-zt,zt,t);return new(xt,Y(xt,z)-thickness,z);});
            Strip(2,upper,upper.Select(p=>new Vector3(p.X,Y(sx*(hx-b0),0,true)-.2f,p.Z)).ToArray(),new(sx,0,0),true);
        }
        else foreach(float sx in new[]{-1f,1f})
        {
            var upper=Line(12,t=>{float z=Mathf.Lerp(-d/2,d/2,t);return new(sx*w/2,Math.Max(0,Y(sx*w/2,z)-thickness),z);});
            Strip(2,upper,upper.Select(p=>new Vector3(p.X,0,p.Z)).ToArray(),new(sx,0,0),true);
        }
        var mesh=sts[0].Commit();sts[1].Commit(mesh);sts[2].Commit(mesh);
        mesh.SurfaceSetMaterial(0,top);mesh.SurfaceSetMaterial(1,wood);mesh.SurfaceSetMaterial(2,gable??VillageAtlas(new(1,0),new(1,1)));
        var roofNode=new MeshInstance3D{Name=name,Mesh=mesh};parent.AddChild(roofNode);
        var shell=mesh.CreateTrimeshShape();shell.BackfaceCollision=true;CameraStop(roofNode,shell);
        float ridge=kind=="gable"?2*hx+.05f:2*(hx-b0)+.3f;bool straw=kind=="thatch";
        Box(roofNode,"Roof ridge",new(0,Y(0,0)+(straw?.12f:.06f),0),new(ridge,straw?.4f:.22f,straw?.55f:.3f),straw?Mat(new("3b3a33")):Mat(new("555b60")));
        if(!straw)foreach(float sx in new[]{-1f,1f})Box(roofNode,"Onigawara",new(sx*ridge/2,Y(0,0)+.2f,0),new(.18f,.5f,.42f),Mat(new("4d5358")));
        return roofNode;
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
    public Node3D Minka(string name,Vector2 at,Vector3 size,float yaw,string style)
    {
        var b=new Node3D{Name=name,Position=new(at.X,Height(at.X,at.Y),at.Y),RotationDegrees=new(0,yaw,0)};AddChild(b);Buildings.Add(b);
        var (to,walls,sign)=TripoHouses[style];float eave=size.Y,front=walls.End.Y;
        var mesh=ActorVisual.Descendants(Model(b,"Tripo "+style,style,new(to.X,0,to.Y),-90,new(1.3f,1.3f,1.3f))).OfType<MeshInstance3D>().Single();
        var shell=mesh.Mesh.CreateTrimeshShape();shell.BackfaceCollision=true;CameraStop(mesh,shell);
        // The player meets the model's own walls, posts, decks and steps below the eaves (not its ground faces), and a box fills the walls so doorways stay shut.
        var local=b.GlobalTransform.AffineInverse()*mesh.GlobalTransform;var faces=mesh.Mesh.GetFaces();var low=new List<Vector3>();
        for(int i=0;i<faces.Length;i+=3){var ys=faces[i..(i+3)].Select(v=>(local*v).Y).ToArray();if(ys.Min()<2.4f&&ys.Max()>.05f)low.AddRange(faces[i..(i+3)]);}
        var solid=new StaticBody3D();mesh.AddChild(solid);solid.AddChild(new CollisionShape3D{Shape=new ConcavePolygonShape3D{Data=low.ToArray(),BackfaceCollision=true}});
        var body=new StaticBody3D();b.AddChild(body);
        body.AddChild(new CollisionShape3D{Position=new(walls.GetCenter().X,eave/2,walls.GetCenter().Y),Shape=new BoxShape3D{Size=new(walls.Size.X,eave,walls.Size.Y)}});
        var timber=Mat(new("3f3026"));var hang=new Vector3(sign.X,sign.Y,front+sign.Z);
        ShopSign(b,name,hang);
        foreach(float x in new[]{-1.1f,1.1f})Box(b,"Sign cord",new(sign.X+x,(sign.Y+.21f+eave)/2,hang.Z),new(.025f,eave-sign.Y-.21f,.025f),timber);
        if(style=="teahouse")
        {
            // The chochin hangs from the eave beside the sign, in front of the transom; a bench and a nodate umbrella stand out front.
            var red=Mat(new("b23a2e"));
            Sphere(b,"Chochin lantern",new(1.9f,2.9f,front+.35f),new(.2f,.3f,.2f),Mat(new("d4533a"),.45f));
            foreach(float y in new[]{2.62f,3.18f})Cylinder(b,"Chochin rim",new(1.9f,y,front+.35f),.11f,.06f,timber);
            Box(b,"Lantern cord",new(1.9f,(3.21f+eave)/2,front+.35f),new(.02f,eave-3.21f,.02f),timber);
            Box(b,"Tea bench",new(2.2f,.45f,front+1.5f),new(1.9f,.12f,.6f),red,true);
            foreach(float x in new[]{1.45f,2.95f})Box(b,"Bench leg",new(x,.2f,front+1.5f),new(.1f,.4f,.5f),timber);
            Cylinder(b,"Nodate umbrella pole",new(3.4f,1.3f,front+1.9f),.03f,2.6f,timber);
            Mesh(b,"Nodate umbrella",new CylinderMesh{TopRadius=.03f,BottomRadius=1.25f,Height=.45f,RadialSegments=16},new(3.4f,2.45f,front+1.9f),red);
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
