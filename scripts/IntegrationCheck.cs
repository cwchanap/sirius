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
        var regions=g.World.Buildings.GroupBy(b=>Hoshikawa.World.RegionAt(b.Position)).ToDictionary(r=>r.Key,r=>r.Count());
        Check(regions.Count==3&&regions["Hoshikawa"]==6&&regions["Old Relay Road"]==1&&regions["Komorebi"]==5,
            "six Hoshikawa buildings, the old teahouse on the relay road and five Komorebi buildings");
        var houses=Hoshikawa.World.Hamlet.Concat(Hoshikawa.World.KomorebiHouses).Append(Hoshikawa.World.OldTeahouse).ToArray();
        foreach(var (name,at,size,yaw,_) in houses)
        {
            var b=g.World.Buildings.Single(n=>n.Name==name);
            Check(new[]{-1f,1f}.SelectMany(sx=>new[]{-1f,1f}.Select(sz=>b.ToGlobal(new Vector3(sx*size.X/2,0,sz*size.Z/2))))
                .All(c=>Math.Abs(Hoshikawa.World.Height(c.X,c.Z)-b.GlobalPosition.Y)<.05f),name+" sits on a level pad");
        }
        foreach(var (name,_,_,_,style) in houses)
        {
            var b=g.World.Buildings.Single(n=>n.Name==name);
            var mesh=ActorVisual.Descendants(b).OfType<MeshInstance3D>().Single(m=>m.Mesh is ArrayMesh);var box=b.GlobalTransform.AffineInverse()*mesh.GlobalTransform*mesh.GetAabb();int tris=mesh.Mesh.GetFaces().Length/3;
            var foot=mesh.Mesh.GetFaces().Select(v=>mesh.GlobalTransform*v).Where(v=>v.Y<b.GlobalPosition.Y+.1f).ToArray();
            Check(tris<20000&&mesh.MaterialOverride is BaseMaterial3D{AlbedoTexture:not null,NormalEnabled:true,AlbedoColor.R:>1}&&Math.Abs(box.Position.Y)<.05f&&box.Size.Y>6
                &&foot.Length>0&&foot.All(v=>Math.Abs(Hoshikawa.World.Height(v.X,v.Z)-b.GlobalPosition.Y)<.05f),$"{name} is Tripo's textured {style}, brightened, and its footprint stands on its level pad, {tris} triangles");
        }
        foreach(var (name,_,_,_,_) in houses)
        {
            // Casting up from the sign's top edge, the first roof face met must be an underside (normal down), not the slab's top.
            // No roof overhead fails too: these signs hang from the eaves, and a corner poked right through the roof also meets nothing.
            var b=g.World.Buildings.Single(n=>n.Name==name);var sign=b.GetNode<MeshInstance3D>("Hanging shop sign");var size=((BoxMesh)sign.Mesh).Size;
            var faces=ActorVisual.Descendants(b).OfType<MeshInstance3D>().Where(m=>m.Mesh is ArrayMesh).SelectMany(m=>m.Mesh.GetFaces().Select(v=>m.GlobalTransform*v)).ToArray();
            Check(new[]{-1f,0,1f}.All(s=>{var p=sign.GlobalTransform*new Vector3(s*size.X/2,size.Y/2,0);float best=float.MaxValue;bool under=false;
                for(int i=0;i<faces.Length;i+=3)if(Geometry3D.RayIntersectsTriangle(p,Vector3.Up,faces[i],faces[i+1],faces[i+2]) is {VariantType:Variant.Type.Vector3} hit&&hit.AsVector3().Y-p.Y<best)
                {best=hit.AsVector3().Y-p.Y;under=(faces[i+2]-faces[i]).Cross(faces[i+1]-faces[i]).Y<0;}
                return under;}),name+" sign hangs under its eaves, clear of the roof");
        }
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
        float MeshHeight(float x,float z)=>Hoshikawa.World.MeshHeight(x,z);
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
        // Every water sheet runs the depth-aware water shader, the brook's ripples drifting down to its mouth.
        var sheets=new[]{"Hoshikawa river","Hoshikawa brook","Cedar River"}.Select(n=>g.World.GetNode<MeshInstance3D>(n).MaterialOverride as ShaderMaterial).ToArray();
        Check(sheets.All(m=>m?.Shader.ResourcePath=="res://assets/shaders/water.gdshader")&&sheets[1]!.GetShaderParameter("flow").AsVector2().Y<0
            &&sheets[0]!.GetShaderParameter("flow").AsVector2().X<0&&sheets[2]!.GetShaderParameter("flow").AsVector2().X<0,
            "river, brook and Cedar River share the flowing water shader; the brook drifts toward its mouth, both rivers west toward the sea");
        // Beyond the play area the sea lies still at the river's level, both rivers fall into the valley from gorges in the eastern range,
        // and the Cedar River leaves it through the western canyon to the sea.
        var sea=g.World.GetNode<MeshInstance3D>("Sea");var seaFaces=sea.Mesh.GetFaces();
        var falls=g.World.GetChildren().OfType<MeshInstance3D>().Where(n=>n.Name.ToString().EndsWith(" falls")).ToArray();
        var reaches=g.World.GetChildren().OfType<MeshInstance3D>().Where(n=>Hoshikawa.World.Rivers.Any(r=>n.Name.ToString().StartsWith(r.Name+" "))&&!falls.Contains(n)).ToArray();
        Check(seaFaces.Length>0&&seaFaces.All(v=>v.Y==Hoshikawa.World.WaterLevel&&(!Hoshikawa.World.InPlay(v.X,v.Z)||Math.Abs(v.X)==Hoshikawa.World.HalfWidth||v.Z==Hoshikawa.World.SouthEdge))
            &&((ShaderMaterial)sea.MaterialOverride).GetShaderParameter("flow").AsVector2()==Vector2.Zero
            &&falls.Length==4&&falls.All(f=>((ShaderMaterial)f.MaterialOverride).Shader.ResourcePath=="res://assets/shaders/waterfall.gdshader")
            &&Hoshikawa.World.Rivers.All(r=>reaches.Any(n=>n.Name.ToString().StartsWith(r.Name+" "))),
            $"the still sea lies beyond the shore at the river's level; {falls.Length} falls and {reaches.Length} gorge reaches carry the rivers in and out of the valley");
        // Height() levels each paddy under its own still water; the aze never dip more than their 2 cm under the terrain mesh, and crest well over the water.
        var aze=g.World.GetNode<MeshInstance3D>("Aze levees").Mesh.GetFaces();
        for(int i=0;i<Hoshikawa.World.Paddies.Length;i++)
        {
            var p=Hoshikawa.World.Paddies[i];var water=g.World.GetNode<MeshInstance3D>($"Rice paddy water {i}");float floor=Hoshikawa.World.Height(p.GetCenter().X,p.GetCenter().Y);
            bool level=Enumerable.Range(0,49).All(k=>Math.Abs(Hoshikawa.World.Height(p.Position.X+.3f+(p.Size.X-.6f)*(k%7)/6,p.Position.Y+.3f+(p.Size.Y-.6f)*(k/7)/6)-floor)<.001f);
            float lowestCrest=new[]{new Vector2(p.Position.X-.4f,p.GetCenter().Y),new(p.End.X+.4f,p.GetCenter().Y),new(p.GetCenter().X,p.Position.Y-.4f),new(p.GetCenter().X,p.End.Y+.4f)}
                .Min(c=>aze.Where(v=>new Vector2(v.X,v.Z).DistanceTo(c)<.2f).Max(v=>v.Y))-water.Position.Y;
            Check(level&&water.Position.Y-floor is >.05f and <.12f&&((ShaderMaterial)water.MaterialOverride).Shader.ResourcePath.EndsWith("paddy.gdshader")&&lowestCrest>.15f,
                $"rice paddy {i} floor is level under still water, its aze cresting {lowestCrest:F2} m or more above it");
        }
        Check(aze.All(v=>v.Y>=MeshHeight(v.X,v.Z)-.021f),"the aze stand on the terrain mesh, sinking at most 2 cm where they end");
        using(var crest=PhysicsRayQueryParameters3D.Create(new(31,3,25),new(31,-3,25),1))Check(g.GetWorld3D().DirectSpaceState.IntersectRay(crest) is {Count:>0} hit&&hit["collider"].As<Node>().Name=="Aze collision","the middle aze carries a walker");
        // The water wheel is Tripo's, turning with the brook: its rim dips under the water but clears the bed, crossed legs on the ground cradle its axle.
        var wheel=g.World.GetNode<Node3D>("Water wheel");var wheelMesh=ActorVisual.Descendants(wheel).OfType<MeshInstance3D>().Single();int wheelTris=wheelMesh.Mesh.GetFaces().Length/3;
        var axle=Hoshikawa.World.WheelAxle;var lowest=wheel.ToLocal(axle-new Vector3(0,Hoshikawa.World.WheelRadius,0));await Frames(10);var moved=wheel.ToGlobal(lowest);
        Check(moved.Z<axle.Z-.01f&&Math.Abs(moved.X-axle.X)<.01f&&Math.Abs(moved.DistanceTo(axle)-Hoshikawa.World.WheelRadius)<.01f,$"the water wheel turns on its axle, its bottom moving downstream with the brook ({axle.Z-moved.Z:F3} m in 10 frames)");
        var wheelFaces=wheelMesh.Mesh.GetFaces().Select(v=>wheelMesh.GlobalTransform*v).ToArray();float rimLow=wheelFaces.Min(v=>v.Y);
        Check(wheelTris<10000&&wheelMesh.MaterialOverride is BaseMaterial3D{AlbedoTexture:not null,NormalEnabled:true,AlbedoColor.R:>1}&&rimLow<Hoshikawa.World.WaterLevel-.2f&&wheelFaces.All(v=>v.Y>MeshHeight(v.X,v.Z)+.05f),
            $"the water wheel is Tripo's textured model, brightened, its rim {Hoshikawa.World.WaterLevel-rimLow:F2} m under the water and clear of the bed, {wheelTris} triangles");
        var legs=g.World.GetChildren().OfType<MeshInstance3D>().Where(n=>n.Name.ToString().StartsWith("Water wheel leg")||n.Name.ToString().StartsWith("Water wheel trough post")).ToArray();
        Check(legs.Length==6&&legs.All(l=>{var foot=l.GlobalTransform*new Vector3(0,0,-1);return foot.Y-Hoshikawa.World.Height(foot.X,foot.Z) is >-.15f and <0;}),"the water wheel's four legs and two trough posts stand on the brook bed and its bank");
        using(var block=PhysicsRayQueryParameters3D.Create(axle-new Vector3(0,0,3),axle,1))Check(g.GetWorld3D().DirectSpaceState.IntersectRay(block) is {Count:>0} hit&&hit["collider"].As<Node>().Name=="Water wheel collision"
            &&Math.Abs(((Vector3)hit["position"]).Z-(axle.Z-Hoshikawa.World.WheelRadius))<.05f,"the water wheel is solid to its rim");
        var nearWheel=g.World.GetChildren().OfType<MeshInstance3D>().Where(n=>n.Name.ToString().Contains("River pebbles")||n.Name.ToString().Contains("Bank stone")).Select(n=>n.GlobalPosition)
            .Concat(ActorVisual.Descendants(g.World.GetNode("River reeds")).OfType<MeshInstance3D>().SelectMany(m=>m.Mesh.GetFaces().Select(v=>m.GlobalTransform*v)))
            .Where(v=>Math.Abs(v.X-axle.X)<.8f&&Math.Abs(v.Z-axle.Z)<1.4f).ToArray();
        Check(nearWheel.Length==0,$"reeds, pebbles and bank stones keep clear of the water wheel ({nearWheel.Length} inside it)");
        // The scarecrow stands in a paddy, the rice left unplanted round its pole.
        var rice=new[]{"Rice","Rice, paler hills"}.SelectMany(n=>ActorVisual.Descendants(g.World.GetNode(n)).OfType<MeshInstance3D>()).SelectMany(m=>m.Mesh.GetFaces().Select(v=>m.GlobalTransform*v)).ToArray();
        Check(Hoshikawa.World.Paddies.Any(p=>p.HasPoint(Hoshikawa.World.Scarecrow))&&rice.Length>0&&rice.All(v=>new Vector2(v.X,v.Z).DistanceTo(Hoshikawa.World.Scarecrow)>.35f),"the scarecrow stands in a paddy, the rice cleared round its pole");
        // Both bridges are plank decks on log stringers over stone piers standing in their river beds; under the stone bridge the river runs open.
        foreach(var (name,planks,deck) in new[]{("Stone Bridge",21,.82f),("Cedar Bridge",33,4.1f)})
        {
            var parts=g.World.GetNode(name).GetChildren().OfType<MeshInstance3D>().ToArray();
            var piers=parts.Where(n=>n.Name.ToString().Contains("Stone bridge pier")).Select(n=>(n.GlobalPosition,((BoxMesh)n.Mesh).Size)).ToArray();
            Check(parts.Count(n=>n.Name.ToString().Contains("Bridge plank"))==planks&&parts.Count(n=>n.Name.ToString().Contains("Bridge stringer"))==3&&piers.Length>=4
                &&piers.All(p=>p.GlobalPosition.Y-p.Size.Y/2<Hoshikawa.World.Height(p.GlobalPosition.X,p.GlobalPosition.Z)-.3f&&p.GlobalPosition.Y+p.Size.Y/2<deck-.5f),
                $"{name} is {planks} planks on stringers over {piers.Length} stone piers footed in the ground");
        }
        using(var deck=PhysicsRayQueryParameters3D.Create(new(0,3,-8),new(0,-3,-8),1))Check(g.GetWorld3D().DirectSpaceState.IntersectRay(deck) is {Count:>0} hit&&Math.Abs(hit["position"].AsVector3().Y-.82f)<.01f
            &&Hoshikawa.World.Height(0,-8)<Hoshikawa.World.WaterLevel-1,"the stone bridge's deck carries walkers at .82 m over open river");
        // The Cedar River's banks carry reeds, pebbles and half-sunk rocks where it holds water, clear of the Cedar Bridge and its embankments;
        // the stone bridge keeps its span clear too.
        var cedarReeds=ActorVisual.Descendants(g.World.GetNode("Cedar River reeds")).OfType<MeshInstance3D>().SelectMany(m=>m.Mesh.GetFaces().Select(v=>m.GlobalTransform*v)).ToArray();
        var stones=g.World.GetChildren().OfType<MeshInstance3D>().Where(n=>n.Name.ToString().Contains("River pebbles")||n.Name.ToString().Contains("Half-sunk rock")).Select(n=>n.GlobalPosition).ToArray();
        const float cedarZ=Hoshikawa.World.CedarZ;
        bool Cedar(Vector3 v)=>Math.Abs(v.Z-cedarZ)<6;bool River(Vector3 v)=>Math.Abs(v.Z+8)<6;float bridgeX=Hoshikawa.World.RoadX(cedarZ);
        Check(cedarReeds.Length>0&&cedarReeds.All(v=>Cedar(v)&&Math.Abs(v.X-bridgeX)>5.5f&&Hoshikawa.World.Height(v.X,cedarZ)<1)&&stones.Count(Cedar)>10&&stones.Count(River)>10
            &&stones.All(v=>!(Cedar(v)&&Math.Abs(v.X-bridgeX)<5.5f)&&!(River(v)&&Math.Abs(v.X)<5.5f)),
            $"reeds ({cedarReeds.Length/9} blades) and {stones.Count(Cedar)} stones line the Cedar River, {stones.Count(River)} stones the river, clear of both bridges");
        // The moored boat is Tripo's, floating in the river clear of the bridge and its bed, bobbing on the swell and tied by its bow to a post on the bank.
        var boat=g.World.GetNode<Node3D>("Moored boat");var boatMesh=ActorVisual.Descendants(boat).OfType<MeshInstance3D>().Single();int boatTris=boatMesh.Mesh.GetFaces().Length/3;
        var hullFaces=boatMesh.Mesh.GetFaces().Select(v=>boatMesh.GlobalTransform*v).ToArray();float keel=hullFaces.Min(v=>v.Y),gunwale=hullFaces.Max(v=>v.Y),hullBed=hullFaces.Min(v=>v.Y-MeshHeight(v.X,v.Z));
        Check(boatTris<10000&&boatMesh.MaterialOverride is BaseMaterial3D{AlbedoTexture:not null,NormalEnabled:true,AlbedoColor.R:>1}&&keel<Hoshikawa.World.WaterLevel-.1f&&gunwale>Hoshikawa.World.WaterLevel+.4f&&hullBed>.05f
            &&hullFaces.All(v=>v.Z is >-12 and <-4&&Math.Abs(v.X)>5.5f),$"the boat is Tripo's textured model, brightened, its keel {Hoshikawa.World.WaterLevel-keel:F2} m under the river and {hullBed:F2} m over its bed, clear of the bridge, {boatTris} triangles");
        var rest=boat.Position;await Frames(20);
        Check(boat.Position!=rest&&Math.Abs(boat.Position.Y-Hoshikawa.World.BoatRest.Y)<=.026f&&new Vector2(boat.Position.X-rest.X,boat.Position.Z-rest.Z).Length()<.001f,"the boat bobs in place on the swell");
        using(var hull=PhysicsRayQueryParameters3D.Create(new(Hoshikawa.World.Mooring.X,3,Hoshikawa.World.Mooring.Y),new(Hoshikawa.World.Mooring.X,-3,Hoshikawa.World.Mooring.Y),1))
            Check(g.GetWorld3D().DirectSpaceState.IntersectRay(hull) is {Count:>0} hit&&hit["collider"].As<Node>().Name=="Boat collision","the boat is solid");
        var mooring=g.World.GetNode<Node3D>("Mooring post").GlobalPosition;var rope=g.World.GetChildren().OfType<MeshInstance3D>().Where(n=>n.Name.ToString().StartsWith("Mooring rope")).ToArray();
        var ropeEnds=rope.SelectMany(r=>new[]{r.GlobalTransform*new Vector3(0,0,-1),r.GlobalTransform*new Vector3(0,0,1)}).ToArray();
        Check(Math.Abs(mooring.Y-Hoshikawa.World.Height(mooring.X,mooring.Z))<.001f&&rope.Length==2&&ropeEnds.Any(e=>e.DistanceTo(mooring+Vector3.Up*.7f)<.05f)&&ropeEnds.Any(e=>hullFaces.Min(v=>v.DistanceTo(e))<.25f),
            "the mooring post stands on the ground at the water's edge and its rope reaches the boat's bow");
        // The washing steps are Tripo's, their walls buried in the bank and their slab just over the river.
        var steps=g.World.GetNode<Node3D>("Washing steps");var stepsMesh=ActorVisual.Descendants(steps).OfType<MeshInstance3D>().Single();int stepsTris=stepsMesh.Mesh.GetFaces().Length/3;
        var base_=stepsMesh.Mesh.GetFaces().Select(v=>stepsMesh.GlobalTransform*v).Where(v=>v.Y<steps.Position.Y+.02f).ToArray();
        Check(stepsTris<10000&&stepsMesh.MaterialOverride is BaseMaterial3D{AlbedoTexture:not null,NormalEnabled:true,AlbedoColor.R:>1}&&base_.Length>0
            &&base_.All(v=>v.Y<Math.Max(MeshHeight(v.X,v.Z),Hoshikawa.World.WaterLevel)),$"the washing steps are Tripo's textured model, brightened, their base under the bank or the river, {stepsTris} triangles");
        float step=0;Vector2 stepAt=default;
        for(float x=-20;x<20;x+=.05f)for(float z=-8.9f;z<50;z+=.05f)
        {
            if(z<-3&&Math.Abs(x)<4.5f)continue;// the stone bridge's approaches drop into the river under its abutments
            float h=Hoshikawa.World.Height(x,z),d=Math.Max(Math.Abs(Hoshikawa.World.Height(x+.05f,z)-h),Math.Abs(Hoshikawa.World.Height(x,z+.05f)-h));
            if(d>step){step=d;stepAt=new(x,z);}
        }
        Check(step<.1f,$"hamlet terrain is continuous (largest 5 cm step {step:F3} at {stepAt})");
        Check(g.World.Residents.Count==10&&g.World.Residents.Select(r=>r.Name).Distinct().Count()==10,"ten named residents across two villages");
        Check(g.Patrols.Count()==3&&g.Enemies.Count(e=>e.Scout)==4&&g.Enemies.All(e=>Hoshikawa.World.RegionAt(e.Home)=="Old Relay Road"),"three patrol robots and four scouts on the relay road");
        var merged=g.World.GetChildren().OfType<MeshInstance3D>().Where(n=>n.Name.ToString().StartsWith("Merged statics")).ToArray();
        var baked=g.World.FindChildren("*","MeshInstance3D",true,false).Cast<MeshInstance3D>().Where(n=>!n.Visible&&(n.Mesh is PrimitiveMesh||n.Mesh.ResourceName.StartsWith("prop "))).ToArray();
        Vector3[] Corners(IEnumerable<MeshInstance3D> meshes)=>meshes.SelectMany(n=>n.Mesh.GetFaces().Select(v=>n.GlobalTransform*v)).ToArray();
        var (mergedCorners,bakedCorners)=(Corners(merged),Corners(baked));
        Aabb Extent(Vector3[] points)=>points.Aggregate(new Aabb(points[0],Vector3.Zero),(box,p)=>box.Expand(p));
        Aabb mergedExtent=Extent(mergedCorners),bakedExtent=Extent(bakedCorners);
        Check(merged.Length>0&&mergedCorners.Length==bakedCorners.Length&&mergedExtent.Position.DistanceTo(bakedExtent.Position)<.01f&&mergedExtent.End.DistanceTo(bakedExtent.End)<.01f,
            $"{baked.Length} static primitives bake into {merged.Length} merged meshes, keeping all {bakedCorners.Length/3} triangles where they stood");
        var props=baked.Where(n=>n.Mesh.ResourceName.StartsWith("prop ")).GroupBy(n=>n.Mesh.ResourceName.ToString()[5..]).ToDictionary(x=>x.Key,x=>x.Count());
        Check("lamp bench barrel fence_post fence_panel pebbles river_rock sluice trough hasa field_post field_rail plank cabbage daikon".Split(' ').All(props.ContainsKey),
            "lamps, benches, barrels, the bamboo and field fences, brook stones, paddy sluice, wheel trough and rack, bridge planks and the Komorebi crops are baked Blender props: "+string.Join(", ",props.Select(x=>$"{x.Key} {x.Value}")));
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
                visual.Animator!.Seek(0,true);
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
        var firstBoulder=g.World.GetNode<Node3D>("Weathered boulder").Position;
        Check(firstBoulder.IsEqualApprox(new Vector3(-54.729736f,1.7227528f,35.275383f)),$"countryside scatter is unchanged, its first boulder at {firstBoulder.X:R}, {firstBoulder.Y:R}, {firstBoulder.Z:R}");
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
        Check(g.Music.Current=="valley"&&g.Music.Players.All(p=>p.Value.Playing==(p.Key=="valley")),"the valley's music plays in Hoshikawa, and no other track");
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
        async Task<bool> Head(Vector2 target,bool run=true,int frames=360)
        {
            if(run)Hold("run");Hold("forward");
            try
            {
                for(int frame=0;frame<frames;frame++)
                {
                    var p=g.Player.GlobalPosition;var delta=target-new Vector2(p.X,p.Z);
                    if(delta.Length()<.55f)return true;
                    g.CameraYaw=Mathf.Atan2(-delta.X,-delta.Y);await Frames(1);
                    if(Math.Abs(p.Z-cedarZ)<8)CheckBridgeFloor(p.Y);
                }
                return false;
            }
            finally {Release("forward");Release("run");}
        }
        async Task WalkTo(Vector2 target,bool run=true){if(!await Head(target,run))throw new Exception($"Country path blocked before {target}; player={g.Player.GlobalPosition}");}
        // Down a trail's middle, a waypoint every 10 m or less.
        async Task Follow(string trail,bool reverse=false)
        {
            var points=Hoshikawa.World.Trails.Single(t=>t.Name==trail).Points.Select(p=>new Vector2(p.X,p.Z)).ToArray();if(reverse)Array.Reverse(points);
            for(int i=1;i<points.Length;i++){int n=(int)Math.Ceiling(points[i-1].DistanceTo(points[i])/10);for(int k=1;k<=n;k++)await WalkTo(points[i-1].Lerp(points[i],(float)k/n));}
        }
        void CheckBridgeFloor(float y){if(y<3.8f)throw new Exception("Player fell through the Cedar Bridge");}
        // The relay road: up the switchback from the junction to the teahouse terrace, along the high road and down into the relay plaza
        // under the viaduct, then on over Cedar Pass and the Cedar Bridge into Komorebi.
        Place(g.Player,new(0,Hoshikawa.World.Height(0,-16)+.1f,-16));await Frames(10);
        await Follow("Switchback");
        Check(g.Player.IsOnFloor()&&Math.Abs(g.Player.GlobalPosition.Y-7.5f)<.3f,$"the switchback climbs from the junction to the teahouse terrace {g.Player.GlobalPosition}");
        await Follow("High road");await Follow("Relay plaza");
        Check(g.Player.IsOnFloor()&&g.Region=="Old Relay Road"&&Math.Abs(g.Player.GlobalPosition.Y-3)<.3f,$"the high road comes down into the relay plaza under the viaduct {g.Player.GlobalPosition}");
        Check(g.Music.Current=="relay"&&g.Music.Players["relay"].Playing&&Math.Abs(g.Music.Players["relay"].VolumeDb-Music.Tracks["relay"].Db)<.01f&&g.Music.Players["valley"].StreamPaused,
            "the relay road has its own music, crossfaded in while the valley's waits paused");
        foreach(float z in new[]{-180f,-186f,-192f,-200f,-206f,-213f,-221f,-228f,-235f,-243f,-254f})
            await WalkTo(new(Hoshikawa.World.RoadX(z),z));
        await Frames(5);
        Check(g.Region=="Komorebi"&&g.Player.IsOnFloor(),"walk from the relay under the viaduct and over Cedar Bridge into Komorebi");
        Check(g.Music.Current=="valley"&&g.Music.Players["valley"].Playing&&g.Music.Players["relay"].StreamPaused,"past the relay road the valley's music comes back");
        foreach(var (name,road,approach) in new[]{("Nao",new Vector2(0,-238),new Vector2(-3.8f,-238)),
            ("Emi",new Vector2(0,-246),new Vector2(4.5f,-245)),("Yui",new Vector2(0,-260.2f),new Vector2(-4,-260.2f)),
            ("Toma",new Vector2(0,-254),new Vector2(16.2f,-254.5f))})
        {
            await WalkTo(road);await WalkTo(approach);await Frames(5);
            if(name=="Emi"){g.Player.Health=20;while(g.Player.Inventory.Take("remedy")){}g.Player.SkillCharges=0;}
            g.Interact();Check(g.Mode==GameMode.Dialogue&&g.Speaker==name,"walk from village street to service: "+name);
            if(name=="Emi")Check(g.Player.Health==100&&g.Player.Inventory.Count("remedy")==g.Player.Inventory.Capacity("remedy")&&g.Player.SkillCharges==Player.MaxSkillCharges,"Komorebi medic restores health, remedies and Resonance Cut's charges");
            g.Mode=GameMode.Playing;await WalkTo(road);
        }
        await WalkTo(new(22,-254));await WalkTo(new(24,-251));await WalkTo(new(28,-251));await WalkTo(new(28,-245));
        Check(g.Player.IsOnFloor(),"orchard entrance and aisle are walkable through the fence opening");
        await WalkTo(new(28,-251));await WalkTo(new(24,-251));await WalkTo(new(22,-254));await WalkTo(new(0,-254));
        await WalkTo(new(0,-253));await WalkTo(new(-12,-251.5f));await WalkTo(new(-33,-251.5f));await WalkTo(new(-36,-256));
        Check(g.Player.IsOnFloor(),"garden entrance, cross path and western bed are reachable");
        await WalkTo(new(-33,-251.5f));await WalkTo(new(-12,-251.5f));await WalkTo(new(0,-253));await WalkTo(new(0,-254));
        foreach(var (x,z) in new[]{(22f,-248f),(-24f,-246f)})
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
        await WalkTo(new(0,-235));
        await WalkTo(new(Hoshikawa.World.RoadX(-226),-226));await WalkTo(new(25,-226));await WalkTo(new(29,-226),false);await Frames(5);
        Check(g.Player.IsOnFloor()&&Math.Abs(g.Player.Position.Y-5.23f)<.08f,"normal walking reaches the overlook deck without jumping");
        await WalkTo(new(25,-226),false);
        await WalkTo(new(Hoshikawa.World.RoadX(-226),-226));
        foreach(float z in new[]{-221f,-213f,-206f,-200f,-192f})await WalkTo(new(Hoshikawa.World.RoadX(z),z));
        await WalkTo(new(-12,-187));await WalkTo(new(-12,-190.6f),false);await Frames(5);
        Check(g.Player.IsOnFloor()&&Math.Abs(g.Player.Position.Y-5.23f)<.08f,"normal walking reaches the shelter floor without jumping");
        await WalkTo(new(-12,-187),false);
        await WalkTo(new(Hoshikawa.World.RoadX(-180),-180));await WalkTo(new(1.5f,-170));
        Check(g.Player.IsOnFloor()&&g.Region=="Old Relay Road","return route reconnects to the relay plaza");
        // East from the plaza runs the relay road extension's way in, toward the station, closed in the prologue at its barrier.
        await WalkTo(new(12,-160.5f));await WalkTo(new(30,-161));
        Check(!await Head(new(56,-160),true,240)&&g.Player.IsOnFloor()&&g.Player.GlobalPosition.X is >36 and <41,$"the road east from the plaza toward the station stops at its barrier {g.Player.GlobalPosition}");
        // The teahouse ladder is the relay road's shortcut: drawn up, it leaves the terrace out of reach from the junction under its cliff;
        // lowered from the terrace, it is walked both ways and stays down when Mio recovers at the village.
        var ladder=g.World.Shortcuts.Single();bool Turned(float angle)=>ladder.Part.Quaternion.AngleTo(new Quaternion(Vector3.Right,angle))<.01f;var lever=new Vector2(ladder.From.X,ladder.From.Z);var top=new Vector2(ladder.Part.Position.X,ladder.Part.Position.Z-2);var below=top+new Vector2(0,12);
        Place(g.Player,new(below.X,Hoshikawa.World.Height(below.X,below.Y)+.1f,below.Y));await Frames(10);
        Check(!await Head(top,true,240)&&g.Player.GlobalPosition.Y<4&&g.Interaction().Icon!="ladder",
            $"the drawn-up ladder leaves the teahouse terrace out of reach from the junction, and cannot be lowered from below {g.Player.GlobalPosition}");
        Place(g.Player,new(lever.X,Hoshikawa.World.Height(lever.X,lever.Y)+.1f,lever.Y));await Frames(10);
        Check(g.Interaction().Icon=="ladder","the ladder is lowered from the terrace");
        g.Interact();await Frames(130);await WalkTo(top,false);
        Check(g.Opened.Contains(ladder.Name)&&Turned(ladder.Open)&&g.Interaction().Icon!="ladder","lowering the ladder opens the shortcut, once");
        await WalkTo(below,false);
        Check(g.Player.IsOnFloor()&&g.Player.GlobalPosition.Y<2.5f,$"the lowered ladder walks down to the junction {g.Player.GlobalPosition}");
        await WalkTo(top);
        Check(g.Player.IsOnFloor()&&Math.Abs(g.Player.GlobalPosition.Y-7.5f)<.3f,$"and back up to the teahouse terrace {g.Player.GlobalPosition}");
        // The quarry's remedy pouch lets Mio carry one more remedy for the rest of the prologue.
        var bag=g.Player.Inventory;
        Check(bag.Held.Select(i=>i.Id).SequenceEqual(new[]{"remedy","terminal"})&&bag.Count("remedy")==3&&bag.Count("terminal")==1,"Mio sets out with three remedies and the wrist terminal");
        var pouch=g.World.Pickups.Single(p=>p.Item=="pouch").Node;var crate=pouch.GlobalPosition+Vector3.Back*1.3f;
        Place(g.Player,new(crate.X,Hoshikawa.World.Height(crate.X,crate.Z)+.1f,crate.Z));await Frames(10);
        Check(g.Interaction().Icon=="remedy","the remedy pouch waits on its crate at the quarry's end");
        g.Interact();
        Check(!pouch.Visible&&bag.Count("pouch")==1&&bag.Capacity("remedy")==4&&bag.Count("remedy")==4&&g.Interaction().Icon!="remedy"&&g.ToastIcon=="remedy",
            "taking the remedy pouch adds it to Mio's key items and raises the remedies she carries to four");
        // Two resonance cells lie on the relay road, each on standable ground and taken once.
        var cells=g.World.Pickups.Where(p=>p.Item=="cell").Select(p=>p.Node).ToArray();
        foreach(var found in cells)
        {
            var by=found.GlobalPosition+Vector3.Back*1.3f;by.Y=Hoshikawa.World.Height(by.X,by.Z)+.1f;Place(g.Player,by);await Frames(10);
            Check(g.Player.IsOnFloor()&&new Vector2(g.Player.GlobalPosition.X-by.X,g.Player.GlobalPosition.Z-by.Z).Length()<.2f&&g.Interaction().Icon=="resonance",$"a resonance cell lies on standable ground at {found.GlobalPosition}");
            int had=bag.Count("cell");g.Interact();
            Check(!found.Visible&&bag.Count("cell")==had+1&&g.Interaction().Icon!="resonance",$"taking it puts a resonance cell in Mio's supplies ({had+1})");
        }
        g.Player.SkillCharges=Player.MaxSkillCharges;Check(cells.Length==2&&!g.Player.Use("cell")&&bag.Count("cell")==2,"a resonance cell is not spent while Resonance Cut's charges are full");
        g.Player.SkillCharges=1;Check(g.Player.Use("cell")&&g.Player.SkillCharges==2&&bag.Count("cell")==1,"a resonance cell restores one charge of Resonance Cut");
        g.Retry();await Frames(5);
        Check(g.Opened.Contains(ladder.Name)&&Turned(ladder.Open)&&!pouch.Visible&&bag.Count("remedy")==4&&bag.Count("cell")==1&&g.Player.SkillCharges==Player.MaxSkillCharges,
            "the ladder stays down and the pouch taken when Mio recovers at the village, which refills remedies but not cells");
        // The map's edges are its terrain now: a cliff behind Komorebi stops a walker at chest height, and the one wall on land's side is off the beach, here in the cove between two capes.
        float chest=Hoshikawa.World.Height(0,-297)+1.2f;
        using(var cliff=PhysicsRayQueryParameters3D.Create(new(0,chest,-297),new(0,chest,-315),1))
            Check(g.GetWorld3D().DirectSpaceState.IntersectRay(cliff) is {Count:>0} hit&&hit["collider"].As<Node>().Name=="Terrain collision"
                &&!g.World.GetChildren().Any(n=>n.Name.ToString().StartsWith("Valley boundary")),"the mountain wall behind Komorebi closes the northern edge, no boundary box in the open");
        chest=Hoshikawa.World.Height(-64,42)+1.2f;
        using(var surf=PhysicsRayQueryParameters3D.Create(new(-64,chest,42),new(-100,chest,42),1))
            Check(g.GetWorld3D().DirectSpaceState.IntersectRay(surf) is {Count:>0} hit&&hit["collider"].As<Node>().Name=="Invisible wall"
                &&hit["position"].AsVector3() is var at&&Hoshikawa.World.Height(at.X,at.Z)<Hoshikawa.World.WaterLevel-.4f,"the sea wall stands off the west shore, half a metre deep");
        g.CameraYaw=0;
        g.Player.Respawn();await Frames(5);
        await WalkTo(new(0,15));await WalkTo(new(23,15));await WalkTo(new(34,15));await WalkTo(new(34,20));
        Check(g.Player.IsOnFloor(),"Hoshikawa rice fields remain reachable through their fence opening");
        Place(g.Player,new(Hoshikawa.World.BrookX(15),1.2f,15));await Frames(40);
        Check(g.Player.IsOnFloor()&&g.Player.GlobalPosition.Y>-.1f,"brook footbridge carries the player");
        Place(g.Player,new(Hoshikawa.World.BrookX(33),Hoshikawa.World.Height(Hoshikawa.World.BrookX(33),33)+.1f,33));await Frames(10);
        await WalkTo(new(15.5f,33));Check(g.Player.IsOnFloor()&&g.Player.GlobalPosition.Y>-.2f,"player can climb out of the brook");
        var kawabata=Hoshikawa.World.Kawabata;Place(g.Player,new(kawabata.X,Hoshikawa.World.Height(kawabata.X,kawabata.Y+1.9f)+.1f,kawabata.Y+1.9f));await Frames(10);
        await WalkTo(new(kawabata.X,kawabata.Y-1.2f),false);await Frames(10);float slab=g.Player.GlobalPosition.Y;
        await WalkTo(new(kawabata.X,kawabata.Y+1.9f),false);
        Check(Math.Abs(slab-(Hoshikawa.World.WaterLevel+.08f))<.06f&&g.Player.IsOnFloor()&&g.Player.GlobalPosition.Y>.45f,$"the washing steps walk down to their slab at {slab:F2} m and back up the bank");
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
        foreach(var (name,_,size,_,style) in houses)
        {
            // Up from beside the sign, the first thing met is the model's camera-only shell at the eave; along the ground, its front wall.
            var b=g.World.Buildings.Single(n=>n.Name==name);var (_,walls,sign)=Hoshikawa.World.TripoHouses[style];var under=b.ToGlobal(new(sign.X,1.2f,walls.End.Y+.4f));
            Godot.Collections.Dictionary Up(uint mask)=>space.IntersectRay(PhysicsRayQueryParameters3D.Create(under,under+Vector3.Up*8,mask));
            var eave=Up(Hoshikawa.World.CameraLayer);float eaveY=eave.Count>0?b.ToLocal((Vector3)eave["position"]).Y:0;
            Check(eave.Count>0&&Up(1).Count==0&&eaveY>size.Y-.3f,$"{name} eaves stop the camera's spring arm {eaveY:F2} m up but not the player or robots");
            var front=space.IntersectRay(PhysicsRayQueryParameters3D.Create(b.ToGlobal(new(walls.Position.X+1,1,walls.End.Y+2.5f)),b.ToGlobal(new(walls.Position.X+1,1,0)),1));
            Check(front.Count>0&&b.IsAncestorOf((Node)front["collider"])&&Math.Abs(b.ToLocal((Vector3)front["position"]).Z-walls.End.Y)<.1f,name+" walls stop the player at the Tripo model's front wall");
        }
        var pots=g.World.GetChildren().OfType<MeshInstance3D>().Where(n=>n.Name.ToString().StartsWith("Plant pot")).ToArray();
        Check(pots.Length==4,"hall and clinic have their potted plants");
        foreach(var pot in pots)
        {
            var bottom=pot.GlobalPosition-new Vector3(0,.16f,0);var hit=space.IntersectRay(PhysicsRayQueryParameters3D.Create(bottom+Vector3.Up*.3f,bottom-Vector3.Up*.3f,1));
            Check(hit.Count>0&&Math.Abs(((Vector3)hit["position"]).Y-bottom.Y)<.03f,"plant pot stands on the deck or ground under it: "+pot.Name);
        }
        foreach(var wellName in new[]{"Village well","Komorebi well"})
        {
            var well=g.World.GetNode<Node3D>(wellName);var wellMesh=ActorVisual.Descendants(well).OfType<MeshInstance3D>().Single();var wellBox=wellMesh.GlobalTransform*wellMesh.GetAabb();
            Check(wellMesh.Mesh.GetFaces().Length/3<10000&&wellMesh.MaterialOverride is BaseMaterial3D{AlbedoTexture:not null,NormalEnabled:true,AlbedoColor.R:>1}&&Math.Abs(wellBox.Position.Y-Hoshikawa.World.Height(well.Position.X,well.Position.Z))<.05f&&wellBox.Size.Y>2.6f,
                $"the {wellName} is Tripo's textured model, brightened, grounded, {wellMesh.Mesh.GetFaces().Length/3} triangles");
            var post=space.IntersectRay(PhysicsRayQueryParameters3D.Create(well.GlobalPosition+new Vector3(2,1.5f,0),well.GlobalPosition+new Vector3(0,1.5f,0),1));
            var wellRoof=space.IntersectRay(PhysicsRayQueryParameters3D.Create(well.GlobalPosition+Vector3.Up*6,well.GlobalPosition+Vector3.Up*1.2f,Hoshikawa.World.CameraLayer));
            Check(post.Count>0&&Math.Abs(((Vector3)post["position"]).X-well.GlobalPosition.X-.9f)<.05f&&wellRoof.Count>0,$"the {wellName}'s posts block the player and its roof stops the camera");
        }
        foreach(var (model,x,z,height) in new[]{("lantern",-2.6f,57.5f,1.75f),("lantern",.6f,57.5f,1.75f),("lantern",-6.5f,17.5f,1.75f),("lantern",-6.5f,29f,1.75f),("cart",12.9f,1.6f,1f),("notice",6.4f,17.2f,2.3f),("scarecrow",33.6f,25.8f,1.7f),
            ("lantern",-3.9f,-270.5f,1.75f),("lantern",3.9f,-270.5f,1.75f),("cart",19f,-261f,1f),("notice",5.6f,-251.6f,2.3f),("scarecrow",-29f,-257f,1.7f)})
        {
            var prop=g.World.GetChildren().OfType<Node3D>().Single(n=>Math.Abs(n.Position.X-x)<.01f&&Math.Abs(n.Position.Z-z)<.01f&&n.GetChildren().OfType<StaticBody3D>().Any());
            var mesh=ActorVisual.Descendants(prop).OfType<MeshInstance3D>().Single();var box=mesh.GlobalTransform*mesh.GetAabb();int tris=mesh.Mesh.GetFaces().Length/3;
            var hit=space.IntersectRay(PhysicsRayQueryParameters3D.Create(prop.ToGlobal(new(0,.6f,3)),prop.ToGlobal(new(0,.6f,0)),1));
            Check(tris<10000&&mesh.MaterialOverride is BaseMaterial3D{AlbedoTexture:not null,NormalEnabled:true,AlbedoColor.R:>1}&&Math.Abs(box.Position.Y-Hoshikawa.World.Height(x,z))<.05f&&Math.Abs(box.Size.Y-height)<.1f
                &&hit.Count>0&&prop.IsAncestorOf((Node)hit["collider"]),$"the {model} at ({x}, {z}) is Tripo's textured model, brightened, grounded and solid, {tris} triangles");
        }
        var villageTrees=g.World.GetChildren().OfType<Node3D>().Where(n=>n.Position.Z>0&&n.Position.Z<76&&Math.Abs(n.Position.X)<40&&n.GetChildren().OfType<StaticBody3D>().Any()
            &&"keyaki sugi sakura bamboo shrub".Split(' ').Any(s=>n.Name.ToString().Contains(s))).ToArray();
        Check(villageTrees.Length>=30,$"Hoshikawa is planted with {villageTrees.Length} trees and shrubs");
        var komorebiTrees=g.World.GetChildren().OfType<Node3D>().Where(n=>n.Position.Z<-232&&Math.Abs(n.Position.X)<40&&n.GetChildren().OfType<StaticBody3D>().Any()
            &&"keyaki shrub kaki".Split(' ').Any(s=>n.Name.ToString().StartsWith(s))).ToArray();
        var kaki=komorebiTrees.Where(n=>n.Name.ToString().StartsWith("kaki")).ToArray();
        Check(kaki.Length==9&&komorebiTrees.Count(n=>n.Name.ToString().StartsWith("shrub"))==9,$"Komorebi's orchard has nine persimmon trees and its courtyards nine shrubs, of {komorebiTrees.Length} trees");
        // Every persimmon hangs in a tree's crown: inside its bounds and over a metre up.
        var persimmons=g.World.GetChildren().OfType<MeshInstance3D>().Where(n=>n.Name.ToString().StartsWith("Orchard persimmon")).ToArray();
        var crowns=kaki.Select(t=>(Tree:t,Box:ActorVisual.Descendants(t).OfType<MeshInstance3D>().Select(m=>m.GlobalTransform*m.GetAabb()).Aggregate((a,b)=>a.Merge(b)))).ToArray();
        var hung=persimmons.Select(p=>crowns.Where(c=>c.Box.HasPoint(p.Position))
            .Select(c=>p.Position.Y-c.Tree.Position.Y).DefaultIfEmpty(-1).Max()).ToArray();
        Check(persimmons.Length==9*14&&hung.All(h=>h>1),$"{persimmons.Length} persimmons hang in the orchard's crowns, the lowest {hung.Min():F2} m up");
        foreach(var tree in villageTrees.Concat(komorebiTrees))
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
            var ends=new[]{rail.GetAabb().Position.Z,rail.GetAabb().End.Z}.Select(z=>rail.GlobalTransform*new Vector3(0,0,z)).Select(e=>e.Y-Hoshikawa.World.Height(e.X,e.Z)).ToArray();
            railTilt=Math.Max(railTilt,Math.Abs(ends[0]-ends[1]));
        }
        Check(railTilt<.08f,$"fence rails follow the ground (worst end-to-end height difference {railTilt:F2} m)");
        var lawn=ActorVisual.Descendants(g.World.GetNode("Village grass")).OfType<MeshInstance3D>().SelectMany(m=>m.Mesh.GetFaces().Select(v=>m.GlobalTransform*v)).ToArray();
        Check(lawn.Length>0&&lawn.All(v=>!Hoshikawa.World.OnFootpath(new(v.X,v.Z),0)),"village grass keeps off the footpaths");
        var reeds=ActorVisual.Descendants(g.World.GetNode("River reeds")).OfType<MeshInstance3D>().SelectMany(m=>m.Mesh.GetFaces().Select(v=>m.GlobalTransform*v)).ToArray();
        Check(reeds.Length>0&&reeds.All(v=>!Hoshikawa.World.OnFootpath(new(v.X,v.Z),0)&&!Hoshikawa.World.Landing(new(v.X,v.Z),0)&&!(Math.Abs(v.X)<3.6f&&v.Z is >-13.5f and <-2.5f)&&!(Math.Abs(v.Z-15)<1.2f&&v.X is >17 and <21.6f)),
            $"reeds line the brook and river, clear of the footpaths, the landings, the stone bridge and the footbridge ({reeds.Length/9} blades)");
        // Scene 1 plays as a visual novel driven by the real key path: E first finishes the typing line, then moves on.
        async Task Tap(Key key)
        {
            Input.ParseInputEvent(new InputEventKey {Keycode=key,PhysicalKeycode=key,Pressed=true});await Frames(1);
            Input.ParseInputEvent(new InputEventKey {Keycode=key,PhysicalKeycode=key,Pressed=false});await Frames(1);
        }
        g.Begin();
        Check(g.Mode==GameMode.Dialogue&&g.Speaker=="Zero"&&g.Script.Count==Prologue.Intro.Length,"the journey opens on Zero's voice over the link");
        Check(g.Hud.Portrait("Mio")!=null&&g.World.Residents.All(r=>g.Hud.Portrait(r.Name)!=null)&&g.Hud.Portrait("Zero")==null,"Mio and every resident have a dialogue portrait; unseen Zero has none");
        Check(Hud.Icons.All(n=>Hud.Icon(n) is {} t&&t.GetWidth()==256&&t.GetHeight()==256)&&ResourceLoader.Exists("res://assets/art/ui/logo.png"),$"all {Hud.Icons.Length} HUD icons and the title logo load, the icons 256 px square");
        Check(g.World.Residents.All(r=>r.Body.GetChildren().OfType<Label3D>().Single().VisibilityRangeEnd is >0 and <20),"residents' names float over them only near the camera");
        var voiced=Prologue.SceneNames.Where(s=>!Prologue.Unvoiced.Contains(s.Value)).SelectMany(s=>s.Key.Select((b,i)=>(b.Speaker,Path:$"res://assets/audio/voice/ja/{b.Speaker}/{s.Value}.{i}.mp3"))).Where(b=>b.Speaker is "Mio" or "Zero").ToArray();
        Check(voiced.Length>0&&voiced.All(b=>ResourceLoader.Load(b.Path) is AudioStreamMP3 mp3&&mp3.GetLength()>.5),$"each of Mio's and Zero's {voiced.Length} prologue lines has its Japanese voice at its beat");
        Check(g.Sounds.Count==18&&g.Sounds.Values.All(s=>s!=null&&s.GetLength()>.1)&&new[]{"attack1","attack2","attack3","skill","hurt"}.All(b=>g.Sounds.ContainsKey("mio_"+b)),"every combat sound and each of Mio's five combat shouts loads");
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
        // Mio faces north (-Z) and the robot faces her (+Z) unless a check turns it round.
        Place(g.Player,spot);opponent.GlobalPosition=spot+Vector3.Forward*2;opponent.Model.Rotation=Vector3.Zero;g.Player.Model.Rotation=new(0,Mathf.Pi,0);g.Player.Health=100;await Frames(12);
        Hold("guard");await Frames(2);
        float hp=g.Player.Health;string response=g.Player.ReceiveHit(opponent,false);
        Check(response=="deflected"&&g.Player.Health==hp&&opponent.Posture==48,"timed deflect damages enemy posture without health loss");
        if(DisplayServer.GetName()!="headless")g.SaveScreenshot("res://artifacts/combat-deflect.png");
        await Frames(20);while(g.Player.DeflectWindow>0)await Frames(1);response=g.Player.ReceiveHit(opponent,false);
        Check(response=="guarded"&&g.Player.Posture>0,$"late guard builds player posture: response={response}, guard={g.Player.Guarding}, window={g.Player.DeflectWindow}, posture={g.Player.Posture}");
        response=g.Player.ReceiveHit(opponent,true);
        Check(response=="hit"&&g.Player.Health==hp-Enemy.PerilousDamage,$"perilous strike bypasses guard and takes {Enemy.PerilousDamage} vitality");Release("guard");
        await Frames(65);g.Player.Dodge(Vector3.Right);hp=g.Player.Health;
        Check(g.Player.ReceiveHit(opponent,true)=="evaded"&&g.Player.Health==hp,"dodge evades perilous damage");
        await Frames(50);Place(g.Player,spot);opponent.GlobalPosition=spot+Vector3.Forward*2;g.Player.Model.Rotation=new(0,Mathf.Pi,0);
        bool Fx(string name)=>g.GetChildren().Any(n=>n.Name.ToString().Contains(name));
        float enemyHp=opponent.Health;await Press("attack",2);for(int i=0;i<30&&opponent.Health==enemyHp;i++)await Frames(1);
        Check(Engine.TimeScale<1&&Fx("Sparks")&&Fx("Cut streak"),"a landed swing holds the valley in a brief hit-stop, throws sparks and cuts a streak across the plating");await Frames(40);
        Check(enemyHp-opponent.Health==Enemy.SwordDamage&&!opponent.Dead&&Engine.TimeScale==1,"from the front a swing only scratches a robot's plating");
        float afterHit=opponent.Health;await Frames(30);Check(opponent.Health==afterHit,"one swing cannot hit again after its active frame");
        // With no robot near, a second press during the first swing waits for its link, well before its end, and each swing lunges.
        opponent.GlobalPosition=spot+Vector3.Forward*14;Place(g.Player,spot);await Frames(30);var lungeFrom=g.Player.GlobalPosition;var trail=g.Player.GetNode<SwordTrail>("Sword trail");
        await Press("attack");await Frames(4);bool slashing=trail.Emitting&&(trail.GlobalTransform*trail.Mesh.GetAabb()).Grow(.05f).HasPoint(trail.Blade().Tip);await Press("attack");string first=g.Player.Visual.Animator!.CurrentAnimation.ToString();await Frames((int)(Player.Swings[0].Link*60)-8);
        Check(first.Contains("attack1")&&g.Player.Visual.Animator.CurrentAnimation.ToString().Contains("attack2"),$"a queued attack chains at the first swing's link ({first} -> {g.Player.Visual.Animator.CurrentAnimation})");
        float blade=trail.Blade().Base.DistanceTo(trail.Blade().Tip);
        Check(slashing&&blade>.25f&&blade<.6f,$"the sword trail's ribbon follows the blade while it swings ({blade:F2} m)");
        await Frames(40);var stepped=g.Player.GlobalPosition-lungeFrom;
        Check(stepped.Z<-.7f&&Math.Abs(stepped.X)<.1f,$"the two swings lunge Mio forward ({-stepped.Z:F2} m)");
        opponent.Reset();opponent.GlobalPosition=spot+Vector3.Forward*4.2f;opponent.Model.Rotation=Vector3.Zero;Place(g.Player,spot);await Frames(30);
        Check(g.Player.SkillCharges==Player.MaxSkillCharges&&g.Player.Skill()&&g.Player.SkillCharges==Player.MaxSkillCharges-1,"Resonance Cut spends one of its charges");
        Check(!g.Player.Skill()&&g.Player.SkillCharges==Player.MaxSkillCharges-1,"Resonance Cut cannot be recast while it is cast");
        // The slam's hit-stop holds Mio about seven frames longer than the cast itself.
        int cast=(int)(Player.SkillLength*60)+15;await Frames((int)(Player.SkillSlam*60)+4);
        Check(opponent.Stunned&&opponent.Health==opponent.MaxHealth&&opponent.Posture==0&&Fx("Resonance wave")&&Fx("Resonance pillar"),"Resonance Cut's slam sends out its wave and stuns a robot at extended range, doing it no harm");
        Check(opponent.GetNode<CpuParticles3D>("Stun crackle").Emitting,"jade sparks crackle over a stunned robot");
        await Frames(cast-(int)(Player.SkillSlam*60)-4);
        opponent.GlobalPosition=spot+Vector3.Forward*2;Check(!g.Player.CanShutDown(opponent),"a robot facing Mio cannot be shut down, even stunned");
        opponent.Model.Rotation=new(0,Mathf.Pi,0);Check(g.Player.CanShutDown(opponent),"a robot in reach with its back to Mio can be shut down");
        g.Player.Attack();Check(opponent.Dead,"an attack from behind shuts a robot down outright");
        opponent.Reset();opponent.GlobalPosition=spot+Vector3.Forward*2;opponent.Model.Rotation=Vector3.Zero;
        opponent.TakeHit(0,100);Check(opponent.Broken,"full posture opens execution window");
        g.Player.Attack();Check(opponent.Dead,"attack executes a broken robot");opponent.Reset();
        g.Player.Health=35;int herbs=bag.Count("remedy");
        Check(!g.Player.Use("remedy")&&g.Player.Health==35&&bag.Count("remedy")==herbs,"healing is blocked during an attack, and keeps the remedy");await Frames(70);
        Check(g.Player.Use("remedy")&&g.Player.Health==80&&bag.Count("remedy")==herbs-1,"remedy restores health and consumes one charge");
        // The items screen opens with I over play, or from the pause menu, uses the item shown with E or its Use button and closes back
        // to where it opened, play frozen under it.
        Button Focused()=>(Button)g.Hud.GetViewport().GuiGetFocusOwner();
        Button Labelled(string text)=>g.Hud.FindChildren("*","Button",true,false).OfType<Button>().Single(b=>b.Text==text);
        int charges=g.Player.SkillCharges;g.Player.Health=30;await Tap(Key.I);herbs=bag.Count("remedy");
        Check(g.Mode==GameMode.Inventory&&g.Hud.Selected=="remedy"&&Focused().Text==""&&g.Hud.FindChildren("*","Button",true,false).Count(b=>((Button)b).Text=="")==bag.Held.Count(),
            "I opens the items screen with a slot for each item Mio holds, the first one focused and shown");
        var still=g.Player.GlobalPosition;Hold("forward");await Frames(10);Release("forward");
        Check(g.Player.GlobalPosition==still,"play stays frozen under the items screen");
        await Tap(Key.E);Check(g.Mode==GameMode.Inventory&&g.Player.Health==75&&bag.Count("remedy")==herbs-1,"E on the items screen uses the remedy it shows");
        await Tap(Key.Right);Check(g.Hud.Selected=="cell"&&!Labelled("Use").Disabled,"the arrow keys move the selection to the next slot, a resonance cell");
        await Tap(Key.Down);Check(Items.Get(g.Hud.Selected).Kind==ItemKind.Key&&!Labelled("Use").Visible,"and down to the key items, which have no Use button");
        await Tap(Key.I);Check(g.Mode==GameMode.Playing,"I closes the items screen back to play");
        await Tap(Key.Escape);Labelled("Items").EmitSignal(BaseButton.SignalName.Pressed);await Frames(2);
        Check(g.Mode==GameMode.Inventory,"the pause menu's Items button opens the items screen");
        g.Player.SkillCharges=2;await Tap(Key.Right);Labelled("Use").EmitSignal(BaseButton.SignalName.Pressed);await Frames(2);
        Check(g.Player.SkillCharges==3&&bag.Count("cell")==0&&g.Hud.Selected=="cell"&&Labelled("Use").Disabled,"its Use button spends the last resonance cell, which keeps its slot, now empty");
        await Tap(Key.Escape);Check(g.Mode==GameMode.Paused,"escape closes it back to the pause menu it came from");
        Labelled("Items").EmitSignal(BaseButton.SignalName.Pressed);await Frames(2);Labelled("Back").EmitSignal(BaseButton.SignalName.Pressed);await Frames(2);
        Check(g.Mode==GameMode.Paused,"so does its Back button");g.SetPaused(false);g.Player.SkillCharges=charges;
        for(int charge=1;charge<Player.MaxSkillCharges;charge++){await Frames(cast);Check(g.Player.Skill(),$"Resonance Cut casts again on charge {charge+1}");}
        await Frames(cast);Check(g.Player.SkillCharges==0&&!g.Player.Skill(),$"with its {Player.MaxSkillCharges} charges spent Resonance Cut waits for a rest");
        var scout=g.Enemies.First(e=>e.Scout);g.Player.Health=100;g.Player.Invulnerable=0;
        Check(scout.Health==scout.MaxHealth&&scout.MaxHealth<opponent.MaxHealth&&scout.Model.Scale.X<opponent.Model.Scale.X&&g.Player.ReceiveHit(scout,true)=="hit"&&g.Player.Health==100-scout.Strike(true)&&scout.Strike(true)<opponent.Strike(true),
            $"scouts are smaller, weaker and strike lighter than the patrols ({100-g.Player.Health:F1} damage)");
        // Patrol and aggro also execute their own real physics loop.
        var live=g.Enemies[0];live.Reset();live.SetPhysicsProcess(true);
        Place(g.Player,new(0,0,30));before=live.GlobalPosition;await Frames(60);
        Check(live.GlobalPosition.DistanceTo(before)>.4f&&live.State=="PATROL","robot follows its patrol route");
        Place(g.Player,live.GlobalPosition+Vector3.Back*6);await Frames(30);Check(live.State=="PURSUE","robot acquires a nearby countryside player");
        Check(g.Music.Current=="battle","a robot after Mio brings in the battle music");
        g.Player.Health=100;g.Player.Invulnerable=0;g.Player.Stun=0;
        Place(g.Player,live.GlobalPosition+Vector3.Back*2);await Frames(110);
        Check(g.Player.Health==100-Enemy.StrikeDamage,$"enemy windup resolves a real strike that takes {Enemy.StrikeDamage} vitality");
        g.SetPaused(true);before=live.GlobalPosition;await Frames(30);Check(live.GlobalPosition==before,"pause freezes patrol simulation");g.SetPaused(false);
        live.Stun(.25f);await Frames(5);Check(live.State=="STUNNED"&&live.Badge=="stunned","a stun holds a robot, its stunned icon over its head");await Frames(20);Check(!live.Stunned&&live.State!="STUNNED","the stun wears off by itself");
        Place(g.Player,new(0,0,30));before=live.GlobalPosition;var facing=live.Forward;await Frames(10);
        Check(live.State=="RECOVER"&&live.GlobalPosition.DistanceTo(before)<.02f&&live.Forward.Dot(facing)>.999f,"a recovering robot stays put and keeps its facing even once Mio is out of its sight");
        // Stunned, it neither turns nor strikes while Mio walks round behind it; then her real attack input shuts it down.
        live.Stun(Enemy.ResonanceStun);var back=live.Forward;var behind=live.GlobalPosition-back*2;behind.Y=Hoshikawa.World.Height(behind.X,behind.Z)+.1f;Place(g.Player,behind);g.Player.Model.Rotation=new(0,Mathf.Atan2(back.X,back.Z),0);await Frames(30);
        Check(live.State=="STUNNED"&&live.Forward.Dot(back)>.999f&&g.Player.CanShutDown(live)&&live.Badge=="shutdown","a stunned robot keeps its back turned while Mio gets behind it, its icon now the shutdown one");
        await Press("attack");Check(live.Dead&&live.Badge=="","behind a robot, Mio's attack button shuts it down and clears its icon");
        Check(g.Music.Current=="battle","the battle music outlasts the fight a moment");await Frames((int)(Music.Calm*60)+10);
        Check(g.Music.Current=="relay","then the relay road's music returns");
        foreach(var e in g.Enemies)e.SetPhysicsProcess(false);foreach(var e in g.Patrols)e.Execute();
        Check(g.QuestStage==2&&g.Enemies.Where(e=>e.Scout).All(e=>!e.Dead),"all three patrols disabled advances the mission, the scouts left standing");
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
        // The ladder's body takes its new turn on the next physics step.
        await Frames(2);
        Check(g.Opened.Count==0&&Turned(ladder.Closed)&&g.World.Pickups.All(p=>p.Node.Visible)&&bag.Held.Select(i=>i.Id).SequenceEqual(new[]{"remedy","terminal"})&&bag.Capacity("remedy")==3&&bag.Count("remedy")==3,
            "restart draws the ladder up again, puts the pouch and cells back and Mio's items back to what she set out with");
        Place(g.Player,new(lever.X,Hoshikawa.World.Height(lever.X,lever.Y)+.1f,lever.Y));await Frames(10);g.Interact();await Frames(20);
        g.Restart();g.EndDialogue();await Frames(100);
        Check(g.Opened.Count==0&&Turned(ladder.Closed),"a restart while the ladder is still swinging down leaves it drawn up");
        // The whole flow above also runs under --lang=ja and --lang=zh_TW. Here each language is also checked on its own: glyphs, fixed-width boxes,
        // line breaking, and the real language buttons.
        string startLanguage=Localization.Current;var sans=ThemeDB.FallbackFont;
        string Read(string path){using var file=Godot.FileAccess.Open(path,Godot.FileAccess.ModeFlags.Read);return file.GetAsText();}
        g.Mode=GameMode.Title;g.Hud.RefreshMenu();await Frames(3);
        foreach(var (code,name) in Localization.Languages.Skip(1))
        {
            Localization.Set(code);g.Hud.ApplyLanguage();await Frames(2);
            var ui=Localization.ParseStrings(Read($"res://assets/dialogue/ui.{code}.json"));var lines=Localization.ParseLines(Read($"res://assets/dialogue/{code}.json"),code);
            var serif=Localization.SerifFallbacks()[0];
            var glyphs=ui.Values.Concat(lines.Values).SelectMany(t=>t).Where(c=>c>' ').Distinct().ToArray();var lacking=glyphs.Where(c=>!sans.HasChar(c)||!serif.HasChar(c)).ToArray();
            Check(lacking.Length==0,$"{name}: the bundled sans and serif faces draw all {glyphs.Length} characters of its text {new string(lacking)}");
            var nameFace=Localization.NameFont(code)!;var lackingName=name.Where(c=>!nameFace.HasChar(c)).ToArray();
            Check(lackingName.Length==0,$"{name}: the face its language button is drawn in has every character of the button's own name {new string(lackingName)}");
            float Width(string english,int size,Font? face=null)=>(face??sans).GetStringSize(Localization.T(english),fontSize:size).X;
            var tight=new (string Text,int Size,float Room)[]{("BEGIN JOURNEY     →",17,288),("Return to Hoshikawa",17,288),("Resume journey",17,278),("Recover at the village",17,278),("Restart prologue",17,278),
                ("Quit game",17,278),("Quit",17,78),("WASD  Move      SHIFT  Run      SPACE  Jump",13,334),("LMB / J  Attack     RMB / K  Guard & deflect",13,334),
                ("CTRL / ALT  Dodge     TAB  Lock     Q  Skill",13,334),("E  Interact    H  Remedy    I  Items    ESC  Pause",12,334),
                ("Items",17,278),("Use",17,138),("Back",17,138),("Supplies",13,378),("Key items",13,378),("The train with no passengers",19,360),
                ("Speak to Ren at the workshop",17,340),("Clear the old relay road",17,340),("Restart the manual relay",17,340),("Return to Ren in Hoshikawa",17,340),("Evacuation route secured",17,340)};
            var overflow=tight.Where(t=>Width(t.Text,t.Size)>t.Room).Select(t=>t.Text).ToArray();
            Check(overflow.Length==0,$"{name}: every menu, control and panel string fits its fixed box {string.Join(" | ",overflow)}");
            var items=Items.All.Where(i=>Width(i.Name,26,serif)>236||g.Hud.Wrap(Localization.T(i.Text),358,15).Count>5).Select(i=>i.Name).ToArray();
            Check(items.Length==0,$"{name}: every item's name fits beside its icon and its text in five lines {string.Join(" | ",items)}");
            var wide=ui.Keys.Where(k=>Width(k,14)>540).ToArray();Check(wide.Length==0,$"{name}: no string is wider than the toast panel {string.Join(" | ",wide)}");
            int Wrapped(string text,out bool badStart)
            {
                var paragraph=new TextParagraph {Width=1030};paragraph.AddString(text,sans,20,code);badStart=false;
                for(int i=1;i<paragraph.GetLineCount();i++)badStart|="、。，．）」』】！？…ー,.!?)".Contains(text[paragraph.GetLineRange(i).X]);
                return paragraph.GetLineCount();
            }
            var spill=lines.Where(l=>Wrapped(l.Value,out _)>4).Select(l=>l.Key).ToArray();Check(spill.Length==0,$"{name}: every dialogue line fits the text box in four lines or fewer {string.Join(" ",spill)}");
            var kinsoku=lines.Where(l=>{Wrapped(l.Value,out var bad);return bad;}).Select(l=>l.Key).ToArray();Check(kinsoku.Length==0,$"{name}: no line wraps onto a closing mark {string.Join(" ",kinsoku)}");
            var labels=ActorVisual.Descendants(g).OfType<Label3D>().Select(l=>l.Text).Where(t=>t.Any(char.IsAsciiLetter)).Distinct().ToArray();
            var untranslated=labels.Where(t=>!Localization.IsTranslated(t,code)).ToArray();
            Check(labels.Length>=25&&untranslated.Length==0,$"{name}: all {labels.Length} signs and name labels in the valley have a translation {string.Join(" | ",untranslated)}");
            Check(g.Hud.FindChildren("*","Button",true,false).OfType<Button>().Where(b=>Localization.Languages.All(l=>l.Name!=b.Text)).All(b=>b.Atr(b.Text)==Localization.T(b.Text)),$"{name}: the menu's buttons translate themselves");
        }
        foreach(var (code,name) in Localization.Languages)
        {
            g.Mode=GameMode.Title;g.Hud.RefreshMenu();await Frames(2);
            g.Hud.FindChildren("*","Button",true,false).OfType<Button>().Single(b=>b.Text==name).EmitSignal(BaseButton.SignalName.Pressed);await Frames(2);
            Check(Localization.Current==code&&TranslationServer.GetLocale()==code,$"the title screen's {name} button switches the game to it");
            foreach(var (mode,action) in new[]{(GameMode.Title,"BEGIN JOURNEY     →"),(GameMode.Paused,"Resume journey")})
            {g.Mode=mode;g.Hud.RefreshMenu();await Frames(2);Check((g.Hud.GetViewport().GuiGetFocusOwner() as Button)?.Text==action,$"{name}: on the {mode} screen the main button, not a language button, holds focus, so Enter does what it says");}
            foreach(var mode in new[]{GameMode.Title,GameMode.Paused,GameMode.Defeated,GameMode.Inventory,GameMode.Playing})
            {int drawn=g.Hud.Redraws;g.Mode=mode;g.Hud.RefreshMenu();await Frames(3);Check(g.Hud.Redraws>drawn,$"{name}: the HUD draws its {mode} screen");}
        }
        g.SetLanguage(startLanguage);g.Mode=GameMode.Playing;g.Hud.RefreshMenu();
        Check(Localization.Missing.Count==0,"every string the HUD drew had a translation: "+string.Join(" | ",Localization.Missing));
        GD.Print("Integration check completed: movement, terrain, dialogue, combat, patrols, pause, quest, restart and language.");
    }
}
