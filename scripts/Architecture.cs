using Godot;
namespace Hoshikawa;

// Japanese rural buildings for Hoshikawa: a curved-roof generator and a timber-frame kit driven by World.Hamlet.
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
        float ridge=kind=="gable"?2*hx+.05f:2*(hx-b0)+.3f;bool straw=kind=="thatch";
        Box(roofNode,"Roof ridge",new(0,Y(0,0)+(straw?.12f:.06f),0),new(ridge,straw?.4f:.22f,straw?.55f:.3f),straw?Mat(new("3b3a33")):Mat(new("555b60")));
        if(!straw)foreach(float sx in new[]{-1f,1f})Box(roofNode,"Onigawara",new(sx*ridge/2,Y(0,0)+.2f,0),new(.18f,.5f,.42f),Mat(new("4d5358")));
        return roofNode;
    }
    private static float DoorX(string style,float w)=>style switch{"workshop"=>-w*.18f,"farmhouse"=>-w*.2f,_=>0};
    public Node3D Minka(string name,Vector2 at,Vector3 size,float yaw,string style)
    {
        var b=new Node3D{Name=name,Position=new(at.X,Height(at.X,at.Y),at.Y),RotationDegrees=new(0,yaw,0)};AddChild(b);Buildings.Add(b);
        float w=size.X,h=size.Y,d=size.Z,f=.35f;
        bool kura=style=="kura",thatch=style=="farmhouse",irimoya=style is "hall" or "teahouse" or "farmhouse",engawa=style is "clinic" or "farmhouse";
        var timber=Mat(new("3f3026"));var paper=Mat(new("efe6cf"),.08f);var boards=VillageAtlas(new(1,0),new(9,2));
        Material plaster=style switch{"farmhouse" or "workshop"=>VillageAtlas(new(0,1),new(6,4)),"kura"=>Mat(new("ece6d6")),"clinic"=>Mat(new("e6e6d8")),_=>Mat(new("e9dfc6"))};
        Material tiles=style switch{"teahouse" or "clinic"=>VillageAtlas(new(1,1),Vector2.One,new(1,.62f,.52f)),"workshop"=>VillageAtlas(new(1,1),Vector2.One,new(.92f,.88f,.84f)),_=>VillageAtlas(new(1,1),Vector2.One)};
        Box(b,"Stone foundation",new(0,f/2,0),new(w+.5f,f,d+.5f),stone,true);
        Box(b,"Plaster walls",new(0,(f+h)/2,0),new(w,h-f,d),plaster,true);
        float overhang=kura?.5f:engawa?1.3f:irimoya?1.05f:.85f,thick=thatch?.45f:kura?.32f:.24f;
        float hz=d/2+overhang,rise=thatch?.95f*hz:kura?.5f*hz:.58f*hz;string kind=thatch?"thatch":irimoya?"irimoya":"gable";
        var roofNode=Roof(b,thatch?"Thatch roof":irimoya?"Irimoya roof":"Gable roof",w,d,rise,overhang,kind,thatch?VillageAtlas(new(0,0),Vector2.One):tiles,thick,kura?plaster:null);
        roofNode.Position=new(0,h,0);
        float Under(float x,float z)=>h+RoofY(kind,w,d,rise,overhang,thick,x,z)-thick;
        // Lower band: cedar wainscot, or the kura's namako tile panel with white diagonal battens.
        float band=kura?1.4f:.9f;
        foreach(float s in new[]{-1f,1f})
        {
            Box(b,"Wainscot",new(0,f+band/2,s*(d/2+.03f)),new(w+.08f,band,.05f),kura?Mat(new("40454b")):boards);
            Box(b,"Wainscot",new(s*(w/2+.03f),f+band/2,0),new(.05f,band,d+.08f),kura?Mat(new("40454b")):boards);
        }
        if(kura)
        {
            var batten=Mat(new("f1eee4"));
            foreach(float s in new[]{-1f,1f})for(float c=-w/2-band;c<w/2;c+=.42f)foreach(float dir in new[]{-1f,1f})
            {
                float x0=Math.Max(c,-w/2),x1=Math.Min(c+band,w/2);if(x1-x0<.05f)continue;
                float y0=dir>0?x0-c:band-(x0-c),y1=dir>0?x1-c:band-(x1-c);
                Box(b,"Namako batten",new((x0+x1)/2,f+(y0+y1)/2,s*(d/2+.065f)),new((x1-x0)*Mathf.Sqrt2,.045f,.03f),batten).Rotation=new(0,0,dir*Mathf.Pi/4);
            }
            Box(b,"Plaster band",new(0,h-.5f,0),new(w+.12f,.16f,d+.12f),plaster);
        }
        else
        {
            foreach(float s in new[]{-1f,1f})
            {
                for(int i=0,n=(int)Math.Ceiling(w/2);i<=n;i++)Box(b,"Timber post",new(-w/2+i*w/n,(f+h)/2,s*(d/2+.04f)),new(.2f,h-f,.1f),timber);
                for(int i=1,n=(int)Math.Ceiling(d/2);i<n;i++)Box(b,"Timber post",new(s*(w/2+.04f),(f+h)/2,-d/2+i*d/n),new(.1f,h-f,.2f),timber);
                foreach(float y in new[]{f+band,h-.3f})
                {
                    Box(b,"Nuki beam",new(0,y,s*(d/2+.06f)),new(w+.14f,.13f,.09f),timber);
                    Box(b,"Nuki beam",new(s*(w/2+.06f),y,0),new(.09f,.13f,d+.14f),timber);
                }
            }
        }
        void Lattice(Vector3 c,float width,float height,float z,bool shoji)
        {
            Box(b,shoji?"Shoji paper":"Window paper",c,new(width,height,.04f),paper);
            Box(b,"Window frame",c+new Vector3(0,height/2+.05f,z),new(width+.16f,.1f,.08f),timber);
            Box(b,"Window frame",c+new Vector3(0,-height/2-.05f,z),new(width+.16f,.1f,.08f),timber);
            int bars=(int)(width/(shoji?.3f:.13f));
            for(int i=1;i<bars;i++)Box(b,"Lattice bar",c+new Vector3(-width/2+i*width/bars,0,z),new(.035f,height,.03f),timber);
            if(shoji)for(int i=1;i<5;i++)Box(b,"Lattice bar",c+new Vector3(0,-height/2+i*height/5,z),new(width,.035f,.03f),timber);
        }
        float front=d/2+.04f,doorX=DoorX(style,w);
        if(kura)
        {
            Box(b,"Kura door surround",new(doorX,f+1.2f,front+.1f),new(1.9f,2.4f,.24f),plaster);
            Box(b,"Iron door",new(doorX,f+1.05f,front+.23f),new(1.2f,1.9f,.06f),Mat(new("2f3236")));
            foreach(float s in new[]{-1f,1f})Box(b,"Kannon shutter",new(doorX+s*1.22f,f+1.2f,front+.55f),new(.6f,2.2f,.12f),plaster).Rotation=new(0,s*.9f,0);
            foreach(float s in new[]{-1f,1f})
            {
                Box(b,"High window",new(s*(w/2+.05f),h-1.1f,0),new(.1f,.6f,.7f),Mat(new("2f3236")));
                Box(b,"High window hood",new(s*(w/2+.16f),h-.72f,0),new(.3f,.12f,1),plaster);
            }
        }
        else if(style is "workshop" or "farmhouse")
        {
            Box(b,"Open doorway",new(doorX,f+1.15f,front-.01f),new(2.3f,2.3f,.04f),Mat(new("1c1814")));
            Box(b,"Sliding board door",new(doorX-1.1f,f+1.15f,front+.06f),new(1.2f,2.3f,.06f),boards);
            Box(b,"Door lintel",new(doorX,f+2.36f,front+.05f),new(2.7f,.16f,.1f),timber);
            Lattice(new(w*.28f,f+1.55f,front+.02f),1.6f,1,.03f,false);
        }
        else
        {
            foreach(float s in new[]{-1f,1f})Lattice(new(doorX+s*.45f,f+1.05f,front+.02f),.88f,2,.03f,true);
            Box(b,"Door lintel",new(doorX,f+2.12f,front+.05f),new(2.1f,.14f,.1f),timber);
            foreach(float s in new[]{-1f,1f})Lattice(new(s*w*.32f,f+1.6f,front+.02f),1.3f,.9f,.03f,false);
        }
        if(!kura)foreach(float s in new[]{-1f,1f})
        {
            var side=new Node3D{Position=new(s*(w/2+.04f),0,0),RotationDegrees=new(0,s*90,0)};b.AddChild(side);
            Box(side,"Window paper",new(0,f+1.6f,0),new(1.1f,.8f,.04f),paper);
            for(int i=1;i<8;i++)Box(side,"Lattice bar",new(-.55f+i*1.1f/8,f+1.6f,.03f),new(.035f,.8f,.03f),timber);
        }
        Box(b,"Stepping stone",new(doorX,.06f,front+.55f),new(1.4f,.16f,.6f),stone);
        // The hall's sign sits lower to hang under its kohai porch roof rather than through it.
        float signY=Math.Min(h-.45f,f+(style=="hall"?2.5f:2.75f));
        Box(b,"Hanging shop sign",new(doorX,signY,front+.12f),new(2.6f,.42f,.08f),timber);
        var sign=Label(b,name.ToUpperInvariant(),new(doorX,signY,front+.18f),Gold,23);sign.Billboard=BaseMaterial3D.BillboardModeEnum.Disabled;sign.PixelSize=.0043f;
        for(float x=-w/2+.3f;x<=w/2-.3f;x+=.6f)foreach(float s in new[]{-1f,1f})
        {
            float z0=s*d/2,z1=s*(d/2+overhang-.08f),y0=Under(x,z0)-.05f,y1=Under(x,z1)-.05f;
            Box(b,"Rafter tail",new(x,(y0+y1)/2,(z0+z1)/2),new(.07f,.09f,Mathf.Sqrt((z1-z0)*(z1-z0)+(y1-y0)*(y1-y0))),timber).Rotation=new(-Mathf.Atan2(y1-y0,z1-z0),0,0);
        }
        if(engawa)
        {
            // The farmhouse keeps its doma door and the ground in front of it clear; the veranda runs along the living rooms.
            float deck=overhang-.25f,x0=thatch?doorX+1.5f:-w/2-.15f,x1=w/2+.15f,zp=d/2+deck-.1f;
            Box(b,"Engawa deck",new((x0+x1)/2,.48f,d/2+deck/2),new(x1-x0,.1f,deck),boards,true);
            for(int i=0;i<=4;i++)
            {
                float x=Mathf.Lerp(x0+.1f,x1-.1f,i/4f);
                Box(b,"Engawa support",new(x,.22f,zp),new(.12f,.44f,.12f),timber);
                if(i%4==0)Box(b,"Eave post",new(x,Under(x,zp)/2,zp),new(.16f,Under(x,zp),.16f),timber);
            }
        }
        switch(style)
        {
            case "workshop":
            {
                float xs=w/2+3.1f,low=2.5f,high=h-.4f;
                foreach(float z in new[]{-d/2+.4f,d/2-.4f})
                {
                    var g=b.ToGlobal(new(xs,0,z));float bottom=Height(g.X,g.Z)-b.Position.Y;
                    Box(b,"Shed post",new(xs,(bottom+low)/2,z),new(.16f,low-bottom,.16f),timber);
                }
                Box(b,"Lean-to roof",new(w/2+1.6f,(low+high)/2+.1f,0),new(Mathf.Sqrt(3.4f*3.4f+(high-low)*(high-low)),.1f,d+.3f),boards).Rotation=new(0,0,-Mathf.Atan2(high-low,3.4f));
                Box(b,"Worktable",new(w/2+1.3f,1,d/4),new(2.2f,.12f,1.1f),boards,true);
                foreach(float x in new[]{w/2+.5f,w/2+2.1f})
                {
                    var g=b.ToGlobal(new(x,0,d/4));float bottom=Height(g.X,g.Z)-b.Position.Y-.02f;
                    Box(b,"Worktable trestle",new(x,(bottom+.94f)/2,d/4),new(.12f,.94f-bottom,.8f),timber);
                }
                for(int i=0;i<3;i++)Cylinder(b,"Salvaged cell",new(w/2+.7f+i*.5f,1.35f,d/4),.16f,.58f,Mat(i==1?Jade:new("79918d")));
                Cylinder(b,"Stovepipe",new(w/2+.45f,h/2+.6f,-d/2+1.1f),.1f,h+1.2f,Mat(new("33373a")));
                Cylinder(b,"Stovepipe cap",new(w/2+.45f,h+1.25f,-d/2+1.1f),.2f,.1f,Mat(new("33373a")),.05f);
                float cz=d/4,cy=Under(-w*.15f,cz)+thick+.08f,tilt=Mathf.Atan2(Under(-w*.15f,cz-.1f)-Under(-w*.15f,cz+.1f),.2f);
                Box(b,"Offline solar collector",new(-w*.15f,cy,cz),new(2.2f,.08f,1.5f),Mat(new("2f5a66"),.05f)).Rotation=new(tilt,0,0);
                break;
            }
            case "hall":
            {
                var porch=new Node3D{Position=new(0,f+2.7f,d/2+.9f),RotationDegrees=new(0,90,0)};b.AddChild(porch);
                Roof(porch,"Kohai porch roof",2.2f,3,.95f,.35f,"gable",VillageAtlas(new(1,1),Vector2.One),.18f);
                foreach(float s in new[]{-1.35f,1.35f})Box(b,"Porch post",new(s,(f+2.7f)/2,d/2+1.75f),new(.18f,f+2.7f,.18f),timber);
                Box(b,"Porch beam",new(0,f+2.66f,d/2+1.75f),new(3,.16f,.16f),timber);
                Box(b,"Wide stone step",new(0,.1f,d/2+1.1f),new(3.2f,.2f,1.4f),stone);
                break;
            }
            case "teahouse":
            {
                var indigo=Mat(new("2c3f66"));
                for(int i=-1;i<=1;i++)Box(b,"Noren",new(i*.6f,f+1.72f,front+.1f),new(.56f,.72f,.02f),indigo);
                Box(b,"Noren rod",new(0,f+2.1f,front+.1f),new(1.9f,.04f,.04f),timber);
                var red=Mat(new("b23a2e"));
                Box(b,"Tea bench",new(2.2f,.45f,d/2+1.5f),new(1.9f,.12f,.6f),red,true);
                foreach(float x in new[]{1.45f,2.95f})Box(b,"Bench leg",new(x,.2f,d/2+1.5f),new(.1f,.4f,.5f),timber);
                Cylinder(b,"Nodate umbrella pole",new(3.4f,1.3f,d/2+1.9f),.03f,2.6f,timber);
                Mesh(b,"Nodate umbrella",new CylinderMesh{TopRadius=.03f,BottomRadius=1.25f,Height=.45f,RadialSegments=16},new(3.4f,2.45f,d/2+1.9f),red);
                Sphere(b,"Chochin lantern",new(1.35f,f+2.05f,front+.3f),new(.2f,.3f,.2f),Mat(new("d4533a"),.45f));
                break;
            }
            case "clinic":
            {
                Box(b,"Medic banner",new(w/2-.4f,2.1f,d/2+overhang-.15f),new(.7f,1.5f,.04f),Mat(new("edf0d8")));
                Box(b,"Medic cross vertical",new(w/2-.4f,2.2f,d/2+overhang-.12f),new(.14f,.62f,.03f),Mat(new("638d75")));
                Box(b,"Medic cross horizontal",new(w/2-.4f,2.2f,d/2+overhang-.12f),new(.45f,.14f,.03f),Mat(new("638d75")));
                Box(b,"Herb rack rail",new(-w/2-.9f,1.9f,0),new(.08f,.08f,2.4f),timber);
                foreach(float z in new[]{-1.2f,1.2f})Box(b,"Herb rack post",new(-w/2-.9f,.95f,z),new(.1f,1.9f,.1f),timber);
                for(int i=0;i<6;i++)Box(b,"Drying herbs",new(-w/2-.9f,1.62f,-1+i*.4f),new(.14f,.45f,.14f),Mat(new(i%2==0?"7f9a55":"a4a06a")));
                break;
            }
        }
        return b;
    }
}
