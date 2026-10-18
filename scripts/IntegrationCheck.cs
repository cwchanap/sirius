using Godot;
namespace Hoshikawa;

// A single runnable, in-engine check exercises the real controller, physics and mission flow.
public static class IntegrationCheck
{
    public static async Task Run(Game g)
    {
        void Check(bool ok,string message){if(!ok)throw new Exception(message);GD.Print("CHECK "+message);}
        var held=new HashSet<string>();
        void Hold(string action){held.Add(action);Input.ActionPress(action);}
        void Release(string action){held.Remove(action);Input.ActionRelease(action);}
        async Task Frames(int count)
        {
            for(int i=0;i<count;i++)
            {
                // OS focus changes release injected actions too; keep the test's intended holds alive.
                foreach(string action in held)if(!Input.IsActionPressed(action))
                {GD.Print("TEST_INPUT_RESTORE "+action);Input.ActionPress(action);}
                await g.ToSignal(g.GetTree(),SceneTree.SignalName.PhysicsFrame);
            }
        }
        async Task Press(string action,int frames=1)
        {Hold(action);await Frames(frames);Release(action);await Frames(2);}
        void Place(Player p,Vector3 pos){p.GlobalPosition=pos;p.Velocity=Vector3.Zero;}
        Check(g.World.Buildings.Count(b=>b.Position.Z>-78)==6&&g.World.Buildings.Count(b=>b.Position.Z<-136)==5,
            "six Hoshikawa buildings and five Komorebi buildings");
        foreach(var (name,at,size,yaw,_) in Hoshikawa.World.Hamlet)
        {
            var b=g.World.Buildings.Single(n=>n.Name==name);
            Check(new[]{-1f,1f}.SelectMany(sx=>new[]{-1f,1f}.Select(sz=>b.ToGlobal(new Vector3(sx*size.X/2,0,sz*size.Z/2))))
                .All(c=>Math.Abs(Hoshikawa.World.Height(c.X,c.Z)-b.GlobalPosition.Y)<.05f),name+" sits on a level pad");
        }
        foreach(var (name,_,_,_,style) in Hoshikawa.World.Hamlet)
            Check(g.World.Buildings.Single(n=>n.Name==name).FindChild(style=="farmhouse"?"Thatch roof":style is "kura" or "workshop" or "clinic"?"Gable roof":"Irimoya roof",false,false)!=null,name+" uses its "+style+" archetype");
        Check(g.World.Buildings.Where(b=>b.Position.Z<-136).All(b=>b.FindChild("Ceramic tile roof",false,false)!=null),"Komorebi keeps its buildings");
        foreach(var (name,_,_,_,_) in Hoshikawa.World.Hamlet)
        {
            // Casting up from the sign's top edge, the first roof face met must be an underside (normal down), not the slab's top.
            // No roof overhead fails too: these signs hang from the eaves, and a corner poked right through the roof also meets nothing.
            var b=g.World.Buildings.Single(n=>n.Name==name);var sign=b.GetNode<MeshInstance3D>("Hanging shop sign");var size=((BoxMesh)sign.Mesh).Size;
            var faces=ActorVisual.Descendants(b).OfType<MeshInstance3D>().Where(m=>m.Mesh is ArrayMesh&&m.Name.ToString().Contains("roof")).SelectMany(m=>m.Mesh.GetFaces().Select(v=>m.GlobalTransform*v)).ToArray();
            Check(new[]{-1f,0,1f}.All(s=>{var p=sign.GlobalTransform*new Vector3(s*size.X/2,size.Y/2,0);float best=float.MaxValue;bool under=false;
                for(int i=0;i<faces.Length;i+=3)if(Geometry3D.RayIntersectsTriangle(p,Vector3.Up,faces[i],faces[i+1],faces[i+2]) is {VariantType:Variant.Type.Vector3} hit&&hit.AsVector3().Y-p.Y<best)
                {best=hit.AsVector3().Y-p.Y;under=(faces[i+2]-faces[i]).Cross(faces[i+1]-faces[i]).Y<0;}
                return under;}),name+" sign hangs under its eaves, clear of the roof");
        }
        var decks=ActorVisual.Descendants(g.World).OfType<MeshInstance3D>().Where(m=>m.Name=="Engawa deck"||m.Name=="Wide stone step").ToArray();
        var pots=g.World.GetChildren().OfType<MeshInstance3D>().Where(n=>n.Name.ToString().StartsWith("Plant pot")).ToArray();
        Check(pots.Length==4&&decks.Length==3,"hall and clinic have their potted plants, decks and steps");
        foreach(var pot in pots)
            Check(decks.All(d=>{var l=d.ToLocal(pot.GlobalPosition);var s=((BoxMesh)d.Mesh).Size;return Math.Abs(l.X)>s.X/2+.2f||Math.Abs(l.Z)>s.Z/2+.2f||l.Y-.16f>=s.Y/2-.01f;}),"plant pot stands clear of decks and steps: "+pot.Name);
        Check(Hoshikawa.World.Height(Hoshikawa.World.BrookX(30),30)<-.8f,"brook channel is carved");
        Vector3 MeshVertex(int x,int z)=>new(x,Hoshikawa.World.Height(x,z),z);
        float steepest=0;
        for(int z=2;z<54;z++)for(int x=(int)Hoshikawa.World.BrookX(z)-3;x<=(int)Hoshikawa.World.BrookX(z)+3;x++)
            foreach(var (a,b,c) in new[]{(MeshVertex(x,z),MeshVertex(x+1,z),MeshVertex(x,z+1)),(MeshVertex(x+1,z),MeshVertex(x+1,z+1),MeshVertex(x,z+1))})
                steepest=Math.Max(steepest,Mathf.RadToDeg((c-a).Cross(b-a).AngleTo(Vector3.Up)));
        Check(steepest<=40,$"brook banks stay climbable (steepest mesh face {steepest:F1} degrees)");
        Check(Math.Abs(Hoshikawa.World.Height(-1,63)-Hoshikawa.World.ShrineLevel)<.05f,"shrine plateau is level");
        float mound=0;
        for(int z=46;z<80;z++)for(int x=-18;x<17;x++)if(new Vector2(x,z).DistanceTo(Hoshikawa.World.ShrineCenter)<16)
            foreach(var (a,b,c) in new[]{(MeshVertex(x,z),MeshVertex(x+1,z),MeshVertex(x,z+1)),(MeshVertex(x+1,z),MeshVertex(x+1,z+1),MeshVertex(x,z+1))})
                mound=Math.Max(mound,Mathf.RadToDeg((c-a).Cross(b-a).AngleTo(Vector3.Up)));
        Check(mound<=25,$"shrine mound ramps stay under 25 degrees (steepest mesh face {mound:F1})");
        // The rendered terrain is 1 m triangles, so the water's edge is compared against the chords, not Height() itself.
        float MeshHeight(float x,float z)
        {
            int x0=Mathf.FloorToInt(x),z0=Mathf.FloorToInt(z);float fx=x-x0,fz=z-z0;
            return fx+fz<=1?MeshVertex(x0,z0).Y+fx*(MeshVertex(x0+1,z0).Y-MeshVertex(x0,z0).Y)+fz*(MeshVertex(x0,z0+1).Y-MeshVertex(x0,z0).Y)
                :MeshVertex(x0+1,z0+1).Y+(1-fx)*(MeshVertex(x0,z0+1).Y-MeshVertex(x0+1,z0+1).Y)+(1-fz)*(MeshVertex(x0+1,z0).Y-MeshVertex(x0+1,z0+1).Y);
        }
        var brookFaces=g.World.GetNode<MeshInstance3D>("Hoshikawa brook").Mesh.GetFaces();float hover=-9;Vector3 hoverAt=default;
        // The strip's sides are the lengthwise edges only one triangle uses; the rest lie over open water.
        var sides=Enumerable.Range(0,brookFaces.Length).Select(i=>(brookFaces[i],brookFaces[i/3*3+(i+1)%3])).Where(e=>Math.Abs(e.Item1.Z-e.Item2.Z)>=.5f)
            .Select(e=>e.Item1.Z<e.Item2.Z?e:(e.Item2,e.Item1)).GroupBy(e=>e).Where(e=>e.Count()==1).Select(e=>e.Key);
        foreach(var (a,b) in sides)for(float t=0;t<=1;t+=.05f){var p=a.Lerp(b,t);if(p.Y-MeshHeight(p.X,p.Z)>hover){hover=p.Y-MeshHeight(p.X,p.Z);hoverAt=p;}}
        // Both edge margins must beat the water shader's .035 m vertex wave, or a crest still lifts an edge through the bank.
        Check(hover<-.04f,$"brook water edges stay buried in the banks (highest edge {hover:F3} m at {hoverAt})");
        var river=g.World.GetNode<MeshInstance3D>("Hoshikawa river");var riverSize=((PlaneMesh)river.Mesh).Size;float riverHover=-9;Vector3 riverAt=default;
        // Where the brook's channel meets the river the brook strip carries on from the sheet's edge, so that stretch is open by design.
        for(float x=-riverSize.X/2;x<=riverSize.X/2;x+=.25f)foreach(float z in new[]{river.Position.Z-riverSize.Y/2,river.Position.Z+riverSize.Y/2})
            if((z<river.Position.Z||Math.Abs(x-Hoshikawa.World.BrookX(z))>2.1f)&&river.Position.Y-MeshHeight(x,z)>riverHover){riverHover=river.Position.Y-MeshHeight(x,z);riverAt=new(x,river.Position.Y,z);}
        Check(riverHover<-.04f,$"river water edges stay buried in the banks (highest edge {riverHover:F3} m at {riverAt})");
        float cell=riverSize.X/(((PlaneMesh)river.Mesh).SubdivideWidth+1);var mouth=brookFaces.Where(v=>v.Z==-4).ToArray();
        Check(mouth.Min(v=>v.X)<=Hoshikawa.World.BrookX(-4)-2.1f&&mouth.Max(v=>v.X)>=Hoshikawa.World.BrookX(-4)+2.1f&&mouth.All(v=>v.Y==river.Position.Y&&Math.Abs(Mathf.PosMod(v.X+riverSize.X/2+cell/2,cell)-cell/2)<.001f),"brook mouth sits on the river sheet's edge vertices at its level");
        float step=0;Vector2 stepAt=default;
        for(float x=-20;x<20;x+=.05f)for(float z=-8.9f;z<50;z+=.05f)
        {
            if(z<-3&&Math.Abs(x)<4.5f)continue;// the old bridge's causeway banks are steep by design
            float h=Hoshikawa.World.Height(x,z),d=Math.Max(Math.Abs(Hoshikawa.World.Height(x+.05f,z)-h),Math.Abs(Hoshikawa.World.Height(x,z+.05f)-h));
            if(d>step){step=d;stepAt=new(x,z);}
        }
        Check(step<.1f,$"hamlet terrain is continuous (largest 5 cm step {step:F3} at {stepAt})");
        Check(g.World.Residents.Count==10&&g.World.Residents.Select(r=>r.Name).Distinct().Count()==10,"ten named residents across two villages");
        Check(g.Enemies.Count==3,"three patrol robots");
        var merged=g.World.GetChildren().OfType<MeshInstance3D>().Where(n=>n.Name.ToString().StartsWith("Merged statics")).ToArray();
        var baked=g.World.FindChildren("*","MeshInstance3D",true,false).Cast<MeshInstance3D>().Where(n=>!n.Visible&&n.Mesh is PrimitiveMesh).ToArray();
        Vector3[] Corners(IEnumerable<MeshInstance3D> meshes)=>meshes.SelectMany(n=>n.Mesh.GetFaces().Select(v=>n.GlobalTransform*v)).ToArray();
        var (mergedCorners,bakedCorners)=(Corners(merged),Corners(baked));
        Aabb Extent(Vector3[] points)=>points.Aggregate(new Aabb(points[0],Vector3.Zero),(box,p)=>box.Expand(p));
        Aabb mergedExtent=Extent(mergedCorners),bakedExtent=Extent(bakedCorners);
        Check(merged.Length>0&&mergedCorners.Length==bakedCorners.Length&&mergedExtent.Position.DistanceTo(bakedExtent.Position)<.01f&&mergedExtent.End.DistanceTo(bakedExtent.End)<.01f,
            $"{baked.Length} static primitives bake into {merged.Length} merged meshes, keeping all {bakedCorners.Length/3} triangles where they stood");
        foreach(var model in g.World.Residents.Select(r=>r.Body))
        {
            var skeleton=ActorVisual.Descendants(model).OfType<Skeleton3D>().Single();
            bool tripo=Hoshikawa.World.TripoResidents.TryGetValue(model.Name,out var own),chiyo=model.Name=="Chiyo",koma=model.Name=="KOMA-7",mixamo=tripo&&!chiyo;
            string[] bones=mixamo?new[]{"Hips","Spine2","Head","LeftHand","RightHand","LeftFoot","RightFoot"}:chiyo?new[]{"Hip","Spine02","Head","L_Hand","R_Hand","L_Foot","R_Foot"}:new[]{"Chest","Neck","Jaw","LClavicle","RHand","LToe","RIndex3","LThumb3"};
            Check(skeleton.GetBoneCount()==(mixamo?65:chiyo?41:58)&&bones.All(b=>skeleton.FindBone(b)>=0),
                "character rig imported: "+model.Name);
            if(tripo)
            {
                Check(model.GetChild<Node3D>(0).SceneFilePath==$"res://assets/models/{own.Asset}.glb",model.Name+" uses its own Tripo model");
                var visual=model.GetChildren().OfType<ActorVisual>().Single();
                Check(visual.HasClip("idle")&&visual.Animator!.IsPlaying()&&visual.Animator.GetAnimation("idle").LoopMode==Animation.LoopModeEnum.Linear,model.Name+" idle plays and loops");
                int joint=skeleton.FindBone(koma?"Head":chiyo?"Spine02":"Spine1"),foot=skeleton.FindBone(mixamo?"LeftFoot":"L_Foot");
                visual.Play("idle",1,true);visual.Animator!.Advance(0);
                var pose=skeleton.GetBonePoseRotation(joint);var planted=skeleton.GetBoneGlobalPose(foot).Origin;
                visual.Animator.Advance(1);
                Check(pose.AngleTo(skeleton.GetBonePoseRotation(joint))>.005f,model.Name+" idle animates the rig");
                Check(planted.DistanceTo(skeleton.GetBoneGlobalPose(foot).Origin)<.001f,model.Name+" idle keeps its foot planted");
                var meshes=ActorVisual.Descendants(model).OfType<MeshInstance3D>().ToArray();
                Check(meshes.Length==1&&meshes[0].MaterialOverride==null&&meshes[0].Mesh.SurfaceGetMaterial(0) is BaseMaterial3D {AlbedoTexture:not null,NormalEnabled:true},model.Name+" retains its textured material");
                Check(Math.Abs(model.Position.Y-Hoshikawa.World.Height(model.Position.X,model.Position.Z))<.001f,model.Name+" is placed on village terrain");
                var collider=ActorVisual.Descendants(model).OfType<CollisionShape3D>().Single();
                Check(collider.Shape is CapsuleShape3D capsule&&Math.Abs(capsule.Height-own.Height)<.001f&&Math.Abs(collider.Position.Y-own.Height/2)<.001f,model.Name+" collision fits its height");
                double length=visual.Animator.GetAnimation("idle").Length;
                Check(Math.Abs(length-(koma?6:4))<.01,$"{model.Name} has a {(koma?"six":"four")}-second idle");
                int hip=skeleton.FindBone(chiyo?"Hip":"Hips"),right=skeleton.FindBone(chiyo?"R_Foot":"RightFoot");
                var fixedHip=skeleton.GetBoneGlobalPose(hip);var fixedFoot=skeleton.GetBoneGlobalPose(right);
                float ankle=(skeleton.GlobalTransform*fixedFoot.Origin).Y-model.Position.Y;
                Check(ankle>0&&ankle<.2f,$"{model.Name} stands on the ground (ankle {ankle:F3} m above it)");
                foreach(double f in new[]{0,.25,.5,.75,1})
                {
                    visual.Animator.Seek(f*length,true);
                    Check(skeleton.GetBoneGlobalPose(hip).IsEqualApprox(fixedHip)&&skeleton.GetBoneGlobalPose(right).IsEqualApprox(fixedFoot)&&skeleton.GetBoneGlobalPose(foot).Origin.DistanceTo(planted)<.001f,$"{model.Name} hips and feet stay fixed: {f*length:0.##}");
                }
                visual.Play("idle",1,true);
            }
        }
        Check(g.World.GetNode<Node3D>("Weathered boulder").Position.IsEqualApprox(new Vector3(32.285194f,2.944041f,-41.920254f)),"countryside scatter is unchanged");
        Check(!g.World.GetChildren().OfType<MeshInstance3D>().Any(n=>n.Mesh is SphereMesh&&n.Position.Z is >-9 and <0&&Math.Abs(n.Position.X-Hoshikawa.World.BrookX(n.Position.Z))<2.3f+n.Scale.X),"no countryside boulder sits in the brook mouth");
        foreach(var species in new[]{"keyaki","sugi","sakura","bamboo","shrub"})
        {
            var tree=g.World.GetChildren().OfType<Node3D>().FirstOrDefault(n=>n.Name.ToString().StartsWith(species));
            Check(tree!=null,species+" tree is placed");
            var meshes=ActorVisual.Descendants(tree!).OfType<MeshInstance3D>().ToArray();
            Check(meshes.Length>0&&meshes.All(m=>Enumerable.Range(0,m.Mesh.GetSurfaceCount()).All(i=>m.GetSurfaceOverrideMaterial(i)!=null)),species+" tree model loads with game materials");
            Check(Math.Abs(tree!.Position.Y-Hoshikawa.World.Height(tree.Position.X,tree.Position.Z))<.01f,species+" tree is grounded");
        }
        var playerRig=ActorVisual.Descendants(g.Player.Model).OfType<Skeleton3D>().Single();
        Check(playerRig.GetBoneCount()==65&&new[]{"Hips","Spine2","Head","RightHand","LeftFoot"}.All(b=>playerRig.FindBone(b)>=0),
            "Tripo player rig imported with 65 bones");
        foreach(var clip in new[]{"idle","walk","run","jump","dodge","attack1","attack2","attack3","guard","skill","stagger","death"})
            Check(g.Player.Visual.HasClip(clip),"Blender animation imported: "+clip);
        var animator=g.Player.Visual.Animator!;
        foreach(string clip in new[]{"idle","walk","run","jump"})
        {
            g.Player.Visual.Play(clip,1,true);animator.Advance(0);
            var pose=Enumerable.Range(0,playerRig.GetBoneCount()).Select(playerRig.GetBonePoseRotation).ToArray();
            animator.Advance(animator.GetAnimation(clip).Length*.3);
            Check(Enumerable.Range(0,pose.Length).Any(i=>pose[i].AngleTo(playerRig.GetBonePoseRotation(i))>.01f),
                "Tripo clip deforms the skeleton: "+clip);
            int hip=playerRig.FindBone("Hips");
            for(int i=0;i<=4;i++)
            {
                animator.Seek(animator.GetAnimation(clip).Length*i/4,true);
                var origin=playerRig.GetBoneGlobalPose(hip).Origin;
                Check(new Vector2(origin.X,origin.Z).Length()<.01f,"Tripo clip stays over collision body: "+clip+" sample "+i);
            }
        }
        g.Player.Visual.Play("idle",1,true);
        await Frames(30);Check(g.Player.IsOnFloor(),"player starts on physical terrain");
        foreach(var enemy in g.Enemies)enemy.SetPhysicsProcess(false);
        var before=g.Player.GlobalPosition;await Press("forward",60);
        float walk=before.DistanceTo(g.Player.GlobalPosition);Check(walk>1.5f&&walk<2.5f,"camera-relative walking");
        before=g.Player.GlobalPosition;Hold("run");await Press("forward",60);Release("run");
        Check(before.DistanceTo(g.Player.GlobalPosition)>walk*1.5f,"running is faster than walking");
        Check(Math.Abs(animator.GetAnimation("jump").Length-13/24.0)<.01,"jump contains one Tripo hop, not the repeated source sequence");
        foreach(string scenario in new[]{"standing","running","long fall"})
        {
            g.Player.Respawn();await Frames(12);
            float ground=g.Player.Position.Y,maxRise=0,lo=float.MaxValue,hi=float.MinValue;
            bool airborne=false,landed=false;double previousTime=0;
            if(scenario=="long fall")g.Player.Position+=Vector3.Up*8;
            else
            {
                if(scenario=="running"){Hold("run");Hold("right");}
                Hold("jump");
            }
            for(int i=0;i<90;i++)
            {
                await Frames(1);if(i==1)Release("jump");
                maxRise=Math.Max(maxRise,g.Player.Position.Y-ground);
                if(!g.Player.IsOnFloor())
                {
                    airborne=true;
                    double time=animator.CurrentAnimationPosition;
                    if(animator.CurrentAnimation!="jump"||time>9/24.0+.002||time+.002<previousTime)
                        throw new Exception($"Jump restarts or lands while airborne: {scenario}, time={time}, previous={previousTime}");
                    previousTime=time;
                    float hip=(playerRig.GlobalTransform*playerRig.GetBoneGlobalPose(playerRig.FindBone("Hips")).Origin-g.Player.GlobalPosition).Y;
                    lo=Math.Min(lo,hip);hi=Math.Max(hi,hip);
                }
                else if(airborne)
                {
                    Check(animator.CurrentAnimation=="jump"&&animator.CurrentAnimationPosition>=10/24.0-.002,
                        "landing animation starts on terrain contact: "+scenario);
                    landed=true;break;
                }
            }
            Release("right");Release("run");Release("jump");
            Check(airborne&&landed&&maxRise>.35f,"jump/fall leaves ground and lands: "+scenario);
            Check(hi-lo<.18f,"jump animation adds no repeated vertical hops: "+scenario);
            if(scenario=="standing")
            {
                await Press("jump",2);
                Check(!g.Player.IsOnFloor()&&Math.Abs(animator.GetPlayingSpeed())<.001,
                    "jump during landing recovery returns to physics-driven pose playback");
                await Frames(50);
                Check(g.Player.IsOnFloor(),"consecutive jump lands normally");
            }
            await Frames(15);
            Check(animator.CurrentAnimation=="idle","landing recovers to locomotion: "+scenario);
        }
        before=g.Player.GlobalPosition;await Press("dodge",4);Check(g.Player.Invulnerable>0,"dodge has invulnerability window");
        await Frames(35);Check(g.Player.GlobalPosition.DistanceTo(before)>2,"dodge moves the character");
        // Traverse the entire footpath, including the bridge, using the normal movement input.
        g.Player.Respawn();await Frames(10);Hold("run");await Press("forward",395);Release("run");
        Check(g.Player.GlobalPosition.Z<-16&&g.Player.IsOnFloor(),$"village to countryside crossing is walkable {g.Player.GlobalPosition}, floor={g.Player.IsOnFloor()}");
        async Task WalkTo(Vector2 target,bool run=true)
        {
            if(run)Hold("run");Hold("forward");
            try
            {
                for(int frame=0;frame<360;frame++)
                {
                    var p=g.Player.GlobalPosition;var delta=target-new Vector2(p.X,p.Z);
                    if(delta.Length()<.55f)return;
                    g.CameraYaw=Mathf.Atan2(-delta.X,-delta.Y);await Frames(1);
                    if(p.Z<-103&&p.Z>-119)CheckBridgeFloor(p.Y);
                }
                throw new Exception($"Country path blocked before {target}; player={g.Player.GlobalPosition}");
            }
            finally {Release("forward");Release("run");}
        }
        void CheckBridgeFloor(float y){if(y<3.8f)throw new Exception("Player fell through the Cedar Bridge");}
        Place(g.Player,new(0,Hoshikawa.World.Height(0,-68)+.1f,-68));await Frames(10);
        foreach(float z in new[]{-76f,-84f,-92f,-100f,-106f,-113f,-121f,-128f,-135f,-143f,-154f})
            await WalkTo(new(Hoshikawa.World.RoadX(z),z));
        await Frames(5);
        Check(g.Region=="Komorebi"&&g.Player.IsOnFloor(),"walk from relay through the viaduct and over Cedar Bridge into Komorebi");
        foreach(var (name,road,approach) in new[]{("Nao",new Vector2(0,-138),new Vector2(-6,-138)),
            ("Emi",new Vector2(0,-146),new Vector2(4.5f,-145)),("Yui",new Vector2(0,-155),new Vector2(-8.5f,-155.3f)),
            ("Toma",new Vector2(0,-154),new Vector2(16.2f,-154.5f))})
        {
            await WalkTo(road);await WalkTo(approach);await Frames(5);
            if(name=="Emi"){g.Player.Health=20;g.Player.Remedies=0;}
            g.Interact();Check(g.Mode==GameMode.Dialogue&&g.Speaker==name,"walk from village street to service: "+name);
            if(name=="Emi")Check(g.Player.Health==100&&g.Player.Remedies==3,"Komorebi medic restores health and remedies");
            g.Mode=GameMode.Playing;await WalkTo(road);
        }
        await WalkTo(new(22,-154));await WalkTo(new(24,-151));await WalkTo(new(28,-151));await WalkTo(new(28,-145));
        Check(g.Player.IsOnFloor(),"orchard entrance and aisle are walkable through the fence opening");
        await WalkTo(new(28,-151));await WalkTo(new(24,-151));await WalkTo(new(22,-154));await WalkTo(new(0,-154));
        await WalkTo(new(0,-153));await WalkTo(new(-12,-151.5f));await WalkTo(new(-33,-151.5f));await WalkTo(new(-36,-156));
        Check(g.Player.IsOnFloor(),"garden entrance, cross path and western bed are reachable");
        await WalkTo(new(-33,-151.5f));await WalkTo(new(-12,-151.5f));await WalkTo(new(0,-153));await WalkTo(new(0,-154));
        foreach(var (x,z) in new[]{(22f,-148f),(-24f,-146f)})
        {
            float h=Hoshikawa.World.Height(x,z)+1;
            Check(g.GetWorld3D().DirectSpaceState.IntersectRay(PhysicsRayQueryParameters3D.Create(new(x-1,h,z),new(x+1,h,z),1)).Count>0,
                "closed field fence has collision: "+x);
        }
        // Compare the rendered soil to the actual terrain collider, including its slopes.
        var terrain=g.World.GetNode<StaticBody3D>("Terrain collision");terrain.CollisionLayer=128;await Frames(1);
        try
        {
            float largestGap=0;
            foreach(var bed in g.World.GetChildren().OfType<MeshInstance3D>().Where(n=>n.Name.ToString().StartsWith("Komorebi garden bed")))
                foreach(var v in bed.Mesh.GetFaces())
                {
                    var p=bed.GlobalTransform*v;
                    using var query=PhysicsRayQueryParameters3D.Create(p+Vector3.Up*2,p-Vector3.Up*2,128);
                    using var hit=g.GetWorld3D().DirectSpaceState.IntersectRay(query);
                    if(hit.Count==0)throw new Exception("Missing terrain below garden soil");
                    float gap=p.Y-hit["position"].AsVector3().Y;
                    if(gap<0)throw new Exception("Garden soil buried below terrain");
                    largestGap=Math.Max(largestGap,gap);
                }
            Check(largestGap>0&&largestGap<.08f,$"garden soil follows terrain collider; maximum gap {largestGap:0.000} m");
        }
        finally {terrain.CollisionLayer=1;}
        await Frames(5);
        await WalkTo(new(0,-135));
        await WalkTo(new(Hoshikawa.World.RoadX(-126),-126));await WalkTo(new(25,-126));await WalkTo(new(29,-126),false);await Frames(5);
        Check(g.Player.IsOnFloor()&&Math.Abs(g.Player.Position.Y-5.23f)<.08f,"normal walking reaches the overlook deck without jumping");
        await WalkTo(new(25,-126),false);
        await WalkTo(new(Hoshikawa.World.RoadX(-126),-126));
        foreach(float z in new[]{-121f,-113f,-106f,-100f,-92f})await WalkTo(new(Hoshikawa.World.RoadX(z),z));
        await WalkTo(new(-12,-87));await WalkTo(new(-12,-90.6f),false);await Frames(5);
        Check(g.Player.IsOnFloor()&&Math.Abs(g.Player.Position.Y-5.23f)<.08f,"normal walking reaches the shelter floor without jumping");
        await WalkTo(new(-12,-87),false);
        await WalkTo(new(Hoshikawa.World.RoadX(-86),-86));await WalkTo(new(0,-68));
        Check(g.Player.IsOnFloor()&&g.Region=="Old Relay Road","return route reconnects to the original relay mission");
        var boundary=PhysicsRayQueryParameters3D.Create(new(0,8,-190),new(0,8,-202),1);
        Check(g.GetWorld3D().DirectSpaceState.IntersectRay(boundary).Count>0,"expanded northern map edge remains physically closed");
        g.CameraYaw=0;
        g.Player.Respawn();await Frames(5);
        await WalkTo(new(0,15));await WalkTo(new(23,15));await WalkTo(new(34,15));await WalkTo(new(34,20));
        Check(g.Player.IsOnFloor(),"Hoshikawa rice fields remain reachable through their fence opening");
        Place(g.Player,new(Hoshikawa.World.BrookX(15),1.2f,15));await Frames(40);
        Check(g.Player.IsOnFloor()&&g.Player.GlobalPosition.Y>-.1f,"brook footbridge carries the player");
        Place(g.Player,new(Hoshikawa.World.BrookX(33),Hoshikawa.World.Height(Hoshikawa.World.BrookX(33),33)+.1f,33));await Frames(10);
        await WalkTo(new(15.5f,33));Check(g.Player.IsOnFloor()&&g.Player.GlobalPosition.Y>-.2f,"player can climb out of the brook");
        g.Player.Respawn();await Frames(5);
        foreach(float z in new[]{40f,50f})await WalkTo(new(Hoshikawa.World.RoadX(z),z));
        await WalkTo(new(-1,60));
        Check(g.Player.IsOnFloor()&&Math.Abs(g.Player.GlobalPosition.Y-Hoshikawa.World.ShrineLevel)<.3f,"shrine steps reach the plateau");
        g.Player.Respawn();await Frames(5);await WalkTo(new(-8,54));await WalkTo(new(-26,54));await WalkTo(new(-26,30));
        Check(g.Player.IsOnFloor()&&g.Player.GlobalPosition.Y>Hoshikawa.World.Height(-17,30)+.6f,$"west terrace is reachable from its north ramp {g.Player.GlobalPosition}");
        var space=g.GetWorld3D().DirectSpaceState;
        foreach(var (from,to) in new[]{(new Vector2(0,31),new Vector2(0,15)),(new Vector2(0,15),new Vector2(17,15)),(new Vector2(22,15),new Vector2(34,15)),(new Vector2(34,15),new Vector2(34,20))})
        {
            var side=new Vector2(-(to-from).Normalized().Y,(to-from).Normalized().X)*.4f;
            var blocked=Enumerable.Range(0,21).Select(i=>from.Lerp(to,i/20f)).Where(p=>{float y=Hoshikawa.World.Height(p.X,p.Y)+1;
                return space.IntersectRay(PhysicsRayQueryParameters3D.Create(new(p.X-side.X,y,p.Y-side.Y),new(p.X+side.X,y,p.Y+side.Y),1)).Count>0;}).ToArray();
            Check(blocked.Length==0,$"village corridor {from}->{to} is clear {string.Join(" ",blocked)}");
        }
        var villageTrees=g.World.GetChildren().OfType<Node3D>().Where(n=>n.Position.Z>0&&n.Position.Z<76&&Math.Abs(n.Position.X)<40&&n.GetChildren().OfType<StaticBody3D>().Any()
            &&"keyaki sugi sakura bamboo shrub".Split(' ').Any(s=>n.Name.ToString().Contains(s))).ToArray();
        Check(villageTrees.Length>=30,$"Hoshikawa is planted with {villageTrees.Length} trees and shrubs");
        foreach(var tree in villageTrees)
        {
            Check(Math.Abs(tree.Position.Y-Hoshikawa.World.Height(tree.Position.X,tree.Position.Z))<.01f,"village tree grounded: "+tree.Name);
            float bottom=ActorVisual.Descendants(tree).OfType<MeshInstance3D>().Min(m=>(m.GlobalTransform*m.GetAabb()).Position.Y);
            float r=((CylinderShape3D)tree.GetChildren().OfType<StaticBody3D>().Single().GetChild<CollisionShape3D>(0).Shape).Radius*tree.Scale.X;
            Check(Enumerable.Range(0,8).All(i=>Hoshikawa.World.Height(tree.Position.X+r*Mathf.Cos(i*Mathf.Tau/8),tree.Position.Z+r*Mathf.Sin(i*Mathf.Tau/8))>=bottom),"village tree base meets sloped ground: "+tree.Name);
        }
        float railTilt=0;var rails=g.World.GetChildren().OfType<MeshInstance3D>().Where(n=>n.Name.ToString().StartsWith("Fence rail")).ToArray();
        Check(rails.Length>40,$"field fences are railed ({rails.Length} rails)");
        foreach(var rail in rails)
        {
            var ends=new[]{-1f,1f}.Select(s=>rail.GlobalTransform*new Vector3(0,0,s*((BoxMesh)rail.Mesh).Size.Z/2)).Select(e=>e.Y-Hoshikawa.World.Height(e.X,e.Z)).ToArray();
            railTilt=Math.Max(railTilt,Math.Abs(ends[0]-ends[1]));
        }
        Check(railTilt<.08f,$"fence rails follow the ground (worst end-to-end height difference {railTilt:F2} m)");
        Check(g.World.GetNode<MeshInstance3D>("Village grass").Mesh.GetFaces().All(v=>!Hoshikawa.World.OnFootpath(new(v.X,v.Z),0)),"village grass keeps off the footpaths");
        // Scene 1 plays as a visual novel driven by the real key path: E first finishes the typing line, then moves on.
        async Task Tap(Key key)
        {
            Input.ParseInputEvent(new InputEventKey {Keycode=key,PhysicalKeycode=key,Pressed=true});await Frames(1);
            Input.ParseInputEvent(new InputEventKey {Keycode=key,PhysicalKeycode=key,Pressed=false});await Frames(1);
        }
        g.Begin();
        Check(g.Mode==GameMode.Dialogue&&g.Speaker=="Zero"&&g.Script.Count==Prologue.Intro.Length,"the journey opens on Zero's voice over the link");
        Check(g.Hud.Portrait("Mio")!=null&&g.World.Residents.All(r=>g.Hud.Portrait(r.Name)!=null)&&g.Hud.Portrait("Zero")==null,"Mio and every resident have a dialogue portrait; unseen Zero has none");
        var voiced=Prologue.SceneNames.SelectMany(s=>s.Key.Select((b,i)=>(b.Speaker,Path:$"res://assets/audio/voice/ja/{b.Speaker}/{s.Value}.{i}.mp3"))).Where(b=>b.Speaker is "Mio" or "Zero").ToArray();
        Check(voiced.Length>0&&voiced.All(b=>ResourceLoader.Load(b.Path) is AudioStreamMP3 mp3&&mp3.GetLength()>.5),$"each of Mio's and Zero's {voiced.Length} prologue lines has its Japanese voice at its beat");
        await Frames(10);Check(g.Reveal>0&&g.Reveal<g.Dialogue.Length,"dialogue text types out");
        await Tap(Key.E);Check(g.Beat==0&&g.Reveal==g.Dialogue.Length,"the first press completes the typing line instead of skipping it");
        Check(g.Voice.Playing&&g.Voice.Stream?.ResourcePath=="res://assets/audio/voice/ja/Zero/Intro.0.mp3","Zero wakes Mio in Japanese over the link");
        await Tap(Key.E);Check(g.Beat==1&&g.Voice.Playing&&g.Voice.Stream?.ResourcePath=="res://assets/audio/voice/ja/Mio/Intro.1.mp3","Mio's reply cuts Zero's voice off and plays her own");
        await Tap(Key.E);Check(g.Reveal==g.Dialogue.Length&&g.Voice.Playing,"completing the typing line lets her voice play on");
        int presses=3;while(g.Mode==GameMode.Dialogue&&presses<100){await Tap(Key.E);presses++;}
        Check(g.Mode==GameMode.Playing&&presses==2*Prologue.Intro.Length,$"the opening scene plays every line, two presses each ({presses})");
        g.CameraYaw=0;g.Player.Respawn();await Frames(5);
        foreach(var resident in g.World.Residents)
        {
            // Settle on the talk spot under physics first, so a spot inside a prop or wall fails here instead of passing by teleport.
            var talk=resident.Body.GlobalPosition+Vector3.Back*2;talk.Y=Hoshikawa.World.Height(talk.X,talk.Z)+.1f;
            Place(g.Player,talk);await Frames(10);
            Check(g.Player.IsOnFloor()&&new Vector2(g.Player.GlobalPosition.X-talk.X,g.Player.GlobalPosition.Z-talk.Z).Length()<.2f,"talk spot is standable: "+resident.Name);
            g.Interact();
            Check(g.Mode==GameMode.Dialogue&&g.Speaker==resident.Name,"dialogue: "+resident.Name);
            if(resident.Name=="Ren")Check(g.Voice.Stream==null,"Ren, who has no voice files, stays silent");
            if(resident.Name=="Chiyo")Check(g.Script.Any(b=>b.Speaker=="Mio")&&g.Script.Any(b=>b.Text.Contains("Shio")),"Chiyo recognises Shio's terminal at the first meeting");
            g.Mode=GameMode.Playing;
        }
        Check(g.QuestStage==1,"Ren starts the relay mission");
        Place(g.Player,g.World.Residents.Single(r=>r.Name=="Chiyo").Body.GlobalPosition+Vector3.Back*2);await Frames(5);g.Interact();
        Check(g.Speaker=="Chiyo"&&g.Script.Count==1,"a first-meeting scene plays once, then the resident's usual line");g.EndDialogue();
        var opponent=g.Enemies[0];
        Vector3 spot=new(0,Hoshikawa.World.Height(0,-29)+.1f,-29);
        Place(g.Player,spot);opponent.GlobalPosition=spot+Vector3.Forward*2;g.Player.Model.Rotation=new(0,Mathf.Pi,0);await Frames(12);
        Hold("guard");await Frames(2);
        float hp=g.Player.Health;string response=g.Player.ReceiveHit(opponent,false);
        Check(response=="deflected"&&g.Player.Health==hp&&opponent.Posture==48,"timed deflect damages enemy posture without health loss");
        if(DisplayServer.GetName()!="headless")g.SaveScreenshot("res://artifacts/combat-deflect.png");
        await Frames(20);while(g.Player.DeflectWindow>0)await Frames(1);response=g.Player.ReceiveHit(opponent,false);
        Check(response=="guarded"&&g.Player.Posture>0,$"late guard builds player posture: response={response}, guard={g.Player.Guarding}, window={g.Player.DeflectWindow}, posture={g.Player.Posture}");
        response=g.Player.ReceiveHit(opponent,true);
        Check(response=="hit"&&g.Player.Health<hp,"perilous strike bypasses guard");Release("guard");
        await Frames(65);g.Player.Dodge(Vector3.Right);hp=g.Player.Health;
        Check(g.Player.ReceiveHit(opponent,true)=="evaded"&&g.Player.Health==hp,"dodge evades perilous damage");
        await Frames(50);Place(g.Player,spot);opponent.GlobalPosition=spot+Vector3.Forward*2;g.Player.Model.Rotation=new(0,Mathf.Pi,0);
        float enemyHp=opponent.Health;await Press("attack",2);await Frames(40);
        Check(opponent.Health<enemyHp,"attack resolves one physical-range hit");
        float afterHit=opponent.Health;await Frames(30);Check(opponent.Health==afterHit,"one swing cannot hit again after its active frame");
        opponent.Reset();opponent.GlobalPosition=spot+Vector3.Forward*4.2f;
        Check(g.Player.Skill(),"skill is available initially");Check(!g.Player.Skill(),"skill cannot be recast during cooldown");
        await Frames(65);Check(opponent.Health<100&&opponent.Posture>0,"skill hits at extended range");
        opponent.TakeHit(0,100);Check(opponent.Broken,"full posture opens execution window");
        opponent.GlobalPosition=spot+Vector3.Forward*2;g.Player.Attack();Check(opponent.Dead,"attack executes a broken robot");
        g.Player.Health=35;int herbs=g.Player.Remedies;g.Player.Heal();
        Check(g.Player.Health==35,"healing is blocked during an attack");await Frames(60);g.Player.Heal();
        Check(g.Player.Health==80&&g.Player.Remedies==herbs-1,"remedy restores health and consumes one charge");
        // Patrol and aggro also execute their own real physics loop.
        var live=g.Enemies[1];live.Reset();live.SetPhysicsProcess(true);
        Place(g.Player,new(0,0,30));before=live.GlobalPosition;await Frames(60);
        Check(live.GlobalPosition.DistanceTo(before)>.4f&&live.State=="PATROL","robot follows its patrol route");
        Place(g.Player,live.GlobalPosition+Vector3.Back*6);await Frames(30);Check(live.State=="PURSUE","robot acquires a nearby countryside player");
        g.Player.Health=100;g.Player.Invulnerable=0;g.Player.Stun=0;
        Place(g.Player,live.GlobalPosition+Vector3.Back*2);await Frames(110);
        Check(g.Player.Health<100,"enemy windup resolves a real damaging strike");
        g.SetPaused(true);before=live.GlobalPosition;await Frames(30);Check(live.GlobalPosition==before,"pause freezes patrol simulation");g.SetPaused(false);
        foreach(var e in g.Enemies){e.SetPhysicsProcess(false);e.Execute();}
        Check(g.QuestStage==2,"all patrols disabled advances mission");
        Place(g.Player,g.World.RelayPosition+Vector3.Back*2);g.Interact();
        Check(g.QuestStage==3&&g.Speaker=="Zero","relay activation contacts remote Zero");g.Mode=GameMode.Playing;
        Place(g.Player,g.World.Residents[0].Body.GlobalPosition+Vector3.Back*2);await Frames(60);g.Interact();
        Check(g.QuestStage==4,"returning to Ren completes the prologue");
        int mother=g.Script.FindIndex(b=>b.Speaker=="Mio"&&b.Text.Contains("mother"));
        Check(g.Script[^1].Speaker==""&&g.Script[g.NightFrom].Text.StartsWith("Night")&&mother>g.NightFrom&&g.Script[mother+1].Speaker=="Zero","the prologue closes on the lantern night, where Zero changes the subject from Mio's mother");
        for(int i=0;i<40&&g.Speaker!="Mio";i++)await Tap(Key.E);
        Check(g.Voice.Playing&&g.Voice.Stream?.ResourcePath=="res://assets/audio/voice/ja/Mio/Homecoming.6.mp3","the homecoming voices Mio under its own scene key");
        await Tap(Key.Escape);Check(g.Mode==GameMode.Playing&&!g.Voice.Playing,"escape skips the rest of a scene and silences its voice");
        if(DisplayServer.GetName()!="headless"){g.SaveScreenshot("res://artifacts/mission-complete.png");await Frames(5);}
        g.Restart();Check(g.QuestStage==0&&g.Enemies.All(e=>!e.Dead)&&g.Player.Health==100,"restart restores a fresh playable mission");
        Check(g.Mode==GameMode.Dialogue&&g.Script.SequenceEqual(Prologue.Intro),"restarting the prologue replays its opening scene");g.EndDialogue();
        GD.Print("Integration check completed: movement, terrain, dialogue, combat, patrols, pause, quest and restart.");
    }
}
