# Hoshikawa Organic Hamlet Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Rebuild Hoshikawa as an organic Japanese hamlet — reshaped terrain, six distinct building archetypes, a shrine, a brook, rich dressing — and replace the sphere-blob trees game-wide with Blender-authored species.

**Architecture:** Terrain stays a pure function (`World.Height`) extended with hamlet terms driven by one shared layout table. Buildings are procedural C# (`Architecture.cs`, partial `World`) using a roof height-field generator. Trees are geometry-only GLBs built by a deterministic background-Blender script; C# assigns shared materials (bark textures + `foliage.gdshader`). Textures come from Codex gpt-image.

**Tech Stack:** Godot 4.6.2 Mono / C# .NET 8, Blender 5.2 (`/Applications/Blender.app/Contents/MacOS/Blender --background --factory-startup --python`), `codex exec` image generation, PIL for chroma-keying/splitting.

**Spec:** `docs/superpowers/specs/2026-09-26-hoshikawa-hamlet-design.md`

## Global Constraints

- `World.Height(x,z)` is the only terrain source; never hardcode a Y.
- `RoadX()` and `terrain.gdshader` are not changed.
- Exactly six `Buildings` with `Position.Z > -78`, five with `Position.Z < -136`.
- The global `rng` (seed 2189) must see the same draw sequence: `Village()` draws only from `villageRng` (seed 3301); `Tree()` draws exactly three values from the generator it is passed.
- Clear corridors: (0,31)→(0,15)→(23,15)→(34,15)→(34,20); each resident position + (0,0,2).
- Komorebi's `House()` calls and orchard look unchanged.
- Code style: dense C#, file-scoped `namespace Hoshikawa;`, no blank lines inside methods, rare constraint-only comments.
- Commits: only when the user has approved committing (user rule overrides per-task commits); otherwise leave changes staged per task.

## Review Focus

1. A player who walks off the route into the brook channel must be able to climb out (bank ≤ 40°, `FloorMaxAngle` is 50°) — test in Task 2.
2. Trees/props must not float or sink on the new slopes (terrace ramp, brook banks, mound) — grounding test in Task 4/5.
3. The west terrace wall must not trap the player: both terrace ends are walkable ramps — test in Task 2.
4. Frame time with ~300 alpha-cut GLB trees under Metal — record FPS in the graphical run (Task 6) and add `VisibilityRangeEnd` if below 60.
5. Rebalancing the world must not move existing countryside content (rng stream) — pinned boulder position test in Task 4.

---

### Task 1: Hamlet layout table and terrain

**Files:**
- Modify: `scripts/World.cs` (constants near `RoadX`, `Height()`, `Village()` house/resident calls)
- Test: `scripts/IntegrationCheck.cs` (after the building-count check, line ~27)

**Interfaces:**
- Produces: `World.Hamlet` : `(string Name,Vector2 At,Vector3 Size,float Yaw,string Style)[]`; `World.BrookX(float z)`; `World.ShrineCenter` (Vector2 (-1,63)), `World.ShrineLevel` (2.4f); `World.BrookWater` (-0.40f).

- [ ] **Step 1: Add the pad test** (fails: buildings still at old coordinates)

```csharp
foreach(var (name,at,size,yaw,_) in Hoshikawa.World.Hamlet)
{
    var b=g.World.Buildings.Single(n=>n.Name==name);
    Check(new[]{-1f,1f}.SelectMany(sx=>new[]{-1f,1f}.Select(sz=>b.ToGlobal(new Vector3(sx*size.X/2,0,sz*size.Z/2))))
        .All(c=>Math.Abs(Hoshikawa.World.Height(c.X,c.Z)-b.GlobalPosition.Y)<.05f),name+" sits on a level pad");
}
Check(Hoshikawa.World.Height(Hoshikawa.World.BrookX(30),30)<-.8f,"brook channel is carved");
Check(Math.Abs(Hoshikawa.World.Height(-1,63)-Hoshikawa.World.ShrineLevel)<.05f,"shrine plateau is level");
```

- [ ] **Step 2: Build + headless self-test, expect `SELF_TEST_FAIL` on "Kisaragi Workshop sits on a level pad"** (Hamlet undefined → compile error first; that is the red state).

- [ ] **Step 3: Implement.** Layout table and helpers:

```csharp
public static readonly (string Name,Vector2 At,Vector3 Size,float Yaw,string Style)[] Hamlet={
    ("Kisaragi Workshop",new(-11,9),new(8,4,7),75,"workshop"),("Seed & Grain",new(12.5f,6),new(6,4.6f,5.5f),-95,"kura"),
    ("Village Assembly",new(-12.5f,21),new(9,5,7),82,"hall"),("The Copper Kettle",new(12,24),new(8,4.5f,7),-80,"teahouse"),
    ("Hoshikawa Clinic",new(-11.5f,37),new(7,3.5f,6),98,"clinic"),("Mori Farmhouse",new(11,41.5f),new(8.5f,3.4f,6.5f),-70,"farmhouse")};
public static readonly Vector2 ShrineCenter=new(-1,63);
public const float ShrineLevel=2.4f,BrookWater=-.4f;
public static float BrookX(float z)=>19.3f+.6f*Mathf.Sin(z*.16f);
```

`Height()` ends with `if(z>-2)y=HamletGround(x,z,y,true);` where `HamletGround(x,z,y,pads)` applies, in order:
swells `.35f*Mathf.Sin(x*.21f+.6f)*Mathf.Sin(z*.17f)` × `SmoothStep(3.5,6,|x-RoadX(z)|)` × `SmoothStep(-2,4,z)` × `(1-SmoothStep(15,18,x))`;
terrace `+1.3f*SmoothStep(-19.2,-20.8,x)*SmoothStep(2,6,z)*(1-SmoothStep(46,52,z))`;
square `Lerp(y,0,1-SmoothStep(7,10,|(x,z)-(0,21)|))`;
pads (only when `pads`): for each Hamlet entry, local = rotate(p-At, -Yaw), `edge=max(|lx|-(w/2+1.6),|lz|-(d/2+1.6))`, `level=HamletGround(At.X,At.Y,BaseHeight(At),false)`, `y=Lerp(y,level,1-SmoothStep(0,2.2,edge))`;
brook `y=Math.Min(y,Lerp(y,-.95f,(1-SmoothStep(.6,1.8,|x-BrookX(z)|))*(1-SmoothStep(54,60,z))))`;
mound `y=Lerp(y,ShrineLevel,1-SmoothStep(4.5,11,|(x,z)-ShrineCenter|))`.
Split the current body of `Height()` into `BaseHeight(x,z)` so pads can sample it without recursion.
In `Village()`, place the six houses from `Hamlet` (temporarily via the legacy `House(name,pos,size,wall,workshop,yaw)`), and move residents to the spec positions with `Resident(...,face:)` where `face` is a yaw toward the road.

- [ ] **Step 4: Build, `--import`, headless self-test → `SELF_TEST_PASS`.** All existing route and dialogue checks must still pass.
- [ ] **Step 5: Capture `--overview` and `--portrait` into `artifacts/hamlet/task1/`, inspect.**

### Task 2: Brook, footbridge, terrace wall and shrine site

**Files:**
- Create: `scripts/Hamlet.cs` (partial `World`: `Brook()`, `Terrace()`, `Shrine()`, `StoneLantern(Node3D parent,Vector3 p,float scale=1)`, `Torii(Vector3 p,float yaw)`)
- Modify: `scripts/World.cs` (`Village()` calls them; footpath at z=15 split to `(0,15)-(17.2,15)` and `(21.4,15)-(34,15)`)
- Modify: `scripts/Hud.cs` minimap draws the brook polyline from `BrookX`
- Test: `scripts/IntegrationCheck.cs` (next to the rice-field route check, line ~227)

**Interfaces:**
- Consumes: `BrookX`, `ShrineCenter`, `ShrineLevel`, `BrookWater` from Task 1.
- Produces: `StoneLantern(...)` used by Task 5; roof mesh helper is *not* needed here (shrine uses Task 3 kit later; Task 2 builds it with legacy boxes and Task 3 upgrades it).

- [ ] **Step 1: Add tests**

```csharp
Place(g.Player,new(Hoshikawa.World.BrookX(15),1.2f,15));await Frames(40);
Check(g.Player.IsOnFloor()&&g.Player.GlobalPosition.Y>-.1f,"brook footbridge carries the player");
Place(g.Player,new(Hoshikawa.World.BrookX(33),Hoshikawa.World.Height(Hoshikawa.World.BrookX(33),33)+.1f,33));await Frames(10);
await WalkTo(new(15.5f,33));Check(g.Player.IsOnFloor()&&g.Player.GlobalPosition.Y>-.2f,"player can climb out of the brook");
g.Player.Respawn();await Frames(5);
foreach(float z in new[]{40f,50f})await WalkTo(new(Hoshikawa.World.RoadX(z),z));
await WalkTo(new(-1,60));
Check(g.Player.IsOnFloor()&&Math.Abs(g.Player.GlobalPosition.Y-Hoshikawa.World.ShrineLevel)<.3f,"shrine steps reach the plateau");
g.Player.Respawn();await Frames(5);await WalkTo(new(-8,54));await WalkTo(new(-26,54));await WalkTo(new(-26,30));
Check(g.Player.IsOnFloor()&&g.Player.GlobalPosition.Y>1,"west terrace is reachable from its south ramp");
```

- [ ] **Step 2: Run, expect FAIL at "brook footbridge carries the player"** (no deck: player falls to the bed).
- [ ] **Step 3: Implement.**
  - Water: strip mesh along `BrookX(z)` z∈[-6,56] step 1, half-width 1.15, constant `y=BrookWater`, `water.gdshader`; spring stones (flattened boulders) at z≈56.
  - Footbridge: 9 planks across x `BrookX(15)±2.1`, each plank top = `Lerp(Height(xA,15),Height(xB,15),t)+.28f*Mathf.Sin(Mathf.Pi*t)`, 0.12 thick, 1.6 deep, `solid:true`; two low rails (non-solid visual, solid rails at ±0.8 z would narrow the path — keep them solid, 1.6 m clear).
  - Terrace wall: segments every 2 m along x=-19.95 for z∈[3,51], box from `Height(-19.2,z)-.3` to `Height(-20.9,z)+.25`, 0.55 thick, `stone`, solid; skip segments shorter than .35 m.
  - Shrine: torii at `(RoadX(52),52)` (two 0.28 posts h 4.2 solid, kasagi top beam 5.4 m with upturned ends, nuki beam, vermilion `Mat(new("b54a32"))`); 12 visual step slabs z 53→58.5 (2.6 wide, top = `Height(x,z)+.08`); honden 3×2.6 on 0.6 m stone base at `ShrineCenter+(0,2.2)` facing −Z; offering box, bell rope, shimenawa with shide; two `StoneLantern` at `(-2.6,57.5)`/`(0.6,57.5)`.
  - `StoneLantern`: base cylinder, shaft cylinder, fire box (box + emissive `Mat(new("f5d9a0"),.6f)` inner), roof = `CylinderMesh{RadialSegments=4,TopRadius=.05f}` rotated 45°, finial sphere; collision cylinder r .3.
  - Minimap: `for z in [-6,56] step 2: DrawLine(Map(BrookX(z),z), Map(BrookX(z+2),z+2), river colour, 2)`.
- [ ] **Step 4: Headless self-test → PASS; capture brook + shrine views.**

### Task 3: Village material atlas and the Minka building kit

**Files:**
- Create: `assets/art/village-atlas.png` (2×2: thatch (0,0), dark cedar boards (1,0), earthen plaster (0,1), grey kawara tiles (1,1)); `scripts/Architecture.cs`
- Modify: `scripts/World.cs` (`Atlas()` gains a `string texture="res://assets/art/material-atlas.png"` parameter; `Village()` calls `Minka` instead of `House`); `ASSETS.md`
- Test: `scripts/IntegrationCheck.cs` (after the pad test)

**Interfaces:**
- Consumes: `Hamlet` table (Task 1).
- Produces: `Node3D Minka(string name,Vector2 at,Vector3 size,float yaw,string style)` (adds to `Buildings`); `MeshInstance3D Roof(Node3D parent,string name,float w,float d,float rise,float overhang,string kind,Material top,float thickness)` where `kind` ∈ `"gable"|"irimoya"|"thatch"`.

- [ ] **Step 1: Generate the atlas with Codex** (per `generating-images-with-cli`, Path B):

```bash
codex exec --skip-git-repo-check -s workspace-write -C /Users/chanwaichan/workspace/sirius - <<'PROMPT'
Use your built-in image-generation tool to create a genuine AI raster. Do NOT draw it with Python/PIL/SVG/code; if you have no image tool print NO IMAGE TOOL and stop.
A square 2x2 texture atlas, four equal quadrants, each a seamless tileable hand-painted stylized material for a warm painterly anime JRPG countryside (Ghibli-like brushwork, soft light, no text, no borders, no perspective, flat top-down orthographic swatches):
top-left: golden-grey weathered rice-straw thatch, layered bundles running vertically;
top-right: dark weathered Japanese cedar boards (yakisugi-inspired), vertical planks;
bottom-left: warm ochre earthen plaster wall with subtle straw flecks;
bottom-right: blue-grey Japanese kawara clay roof tiles, rows running horizontally.
Then copy the generated image to /Users/chanwaichan/workspace/sirius/assets/art/village-atlas.png and print the path.
PROMPT
sips -z 2048 2048 assets/art/village-atlas.png
```
Open and inspect it; record prompt/tool/date in `ASSETS.md`.

- [ ] **Step 2: Add tests**

```csharp
foreach(var (name,_,_,_,style) in Hoshikawa.World.Hamlet)
    Check(g.World.Buildings.Single(n=>n.Name==name).FindChild(style=="farmhouse"?"Thatch roof":style is "kura" or "workshop" or "clinic"?"Gable roof":"Irimoya roof",false,false)!=null,name+" uses its "+style+" archetype");
Check(g.World.Buildings.Where(b=>b.Position.Z<-136).All(b=>b.FindChild("Ceramic tile roof",false,false)!=null),"Komorebi keeps its buildings");
```

- [ ] **Step 3: Run, expect FAIL at "Kisaragi Workshop uses its workshop archetype".**
- [ ] **Step 4: Implement `Architecture.cs`.**
  - `Roof`: builds a SurfaceTool grid (0.5 m cells) over `[-hx,hx]×[-hz,hz]` (`hx=w/2+overhang`, `hz=d/2+overhang`), ridge along local X. Height profile `P(t)=rise*(1-t)-sag*4t(1-t)` with `t=1-a/hz` and `sag=.12*rise` (thatch: `sag=0`), `a=hz-|z|`, `b=hx-|x|`.
    - gable: `h=P(a)`; add vertical gable triangles at `x=±w/2` (wall material) with `CullMode.Disabled`.
    - irimoya: centre strip `|x|≤hx-b0` uses `h=P(a)`; end strips use `h=min(P(a),P(b))`; vertical tsuma triangles at `x=±(hx-b0)` fill from `min(P(a),P(b0))` to `P(a)`; `b0=.45*hz`.
    - thatch: irimoya with `rise=.95*hz`, `thickness=.45`, straight profile.
    - Corner lift `+.25*(1-SmoothStep(0,1.2,a))*Pow(|x|/hx,6)` (not for thatch).
    - Emit top (UV = (x,slope distance)*.35), underside offset by `thickness` with flipped winding, and a perimeter fascia strip; ridge cap box along the ridge.
  - `Minka`: stone base (solid), walls as solid box (collision) with visual overlay: posts at corners + every ≤2 m (`wood`), nuki beams at 0.9 m and h-0.3 m, plaster panels `Atlas(new(0,1),…, village-atlas)`, 0.9 m lower board wainscot `Atlas(new(1,0))`; front: sliding shoji door pair (paper `Mat(new("efe6cf"),.08f)` + lattice), two lattice windows; entry step; hanging name sign + `Label` (same pattern as `House`); rafter tails every 0.6 m under front/back eaves. Style extras:
    - workshop: gable roof `roof` (existing red tiles) + lean-to shed (3 m, posts, single-slope board roof) on local +X holding the worktable, salvaged cells, solar collector on the roof, metal stovepipe.
    - kura: plaster walls `Mat(new("ece6d6"))`, lower 1.4 m namako (dark tile panel + 45° white battens), heavy gable roof with grey kawara `Atlas(new(1,1),…,village-atlas)`, thick ridge, one heavy door with shutters, small high window.
    - hall: irimoya grey kawara, gable porch (kōhai) over the door on two posts, engawa-less, wide step.
    - teahouse: irimoya red tiles, indigo noren (3 hanging cloth panels), red bench + nodate umbrella (cone `CylinderMesh{TopRadius=.03}` + pole) at local (−1.6, 0, d/2+2.6), paper lantern by the door.
    - clinic: gable red tiles, engawa (0.45 m high board deck, 1.1 m deep, posts to the eave) along the front, medic banner (reuse Komorebi cross style), herb drying rack.
    - farmhouse: thatch roof `Atlas(new(0,0),…,village-atlas)`, earthen plaster walls, doma door (wide board door), smoke gable, engawa.
  - Shrine honden from Task 2 switches to `Roof(...,"gable",…)` with `Mat(new("5f7f6f"))` copper.
- [ ] **Step 5: Headless self-test → PASS; capture each building via `review_arrangement.gd`-style views; inspect and iterate proportions.**

### Task 4: Tree species (textures, Blender generator, foliage shader, `Tree()`)

**Files:**
- Create: `assets/art/foliage-atlas.png` (RGBA 2048², cells keyaki (0,0), sakura (1,0), sugi (0,1), bamboo (1,1)); `assets/art/bark-{keyaki,sugi,sakura,bamboo}.png`; `tools/build_props.py`; `assets/models/props/{keyaki_a,keyaki_b,keyaki_c,sugi_a,sugi_b,sugi_c,sakura_a,sakura_b,bamboo_a,shrub_a,shrub_b}.glb`; `assets/shaders/foliage.gdshader`
- Modify: `scripts/World.cs` (`Tree()`, legacy renamed `OrchardTree()` used by the Komorebi orchard only); `ASSETS.md`
- Test: `scripts/IntegrationCheck.cs` (after the rig checks)

**Interfaces:**
- Produces: `Node3D Tree(Vector3 p,float scale,string species="keyaki",RandomNumberGenerator? r=null)`; species ∈ `keyaki|sugi|sakura|bamboo|shrub`; node `Name` = species; trunk `StaticBody3D` collider radius/height from `TreeTrunks` table; `OrchardTree(Vector3 p,float scale)` (legacy).

- [ ] **Step 1: Pin the rng stream before touching anything.** Temporarily print `GetNode<Node3D>("Weathered boulder").Position` in a headless run, then add:

```csharp
Check(g.World.GetNode<Node3D>("Weathered boulder").Position.IsEqualApprox(new Vector3(/*printed x,y,z*/)),"countryside scatter is unchanged");
foreach(var species in new[]{"keyaki","sugi","sakura","bamboo","shrub"})
{
    var tree=g.World.GetChildren().OfType<Node3D>().First(n=>n.Name.ToString().StartsWith(species));
    var meshes=ActorVisual.Descendants(tree).OfType<MeshInstance3D>().ToArray();
    Check(meshes.Length>0&&meshes.All(m=>Enumerable.Range(0,m.Mesh.GetSurfaceCount()).All(i=>m.GetSurfaceOverrideMaterial(i)!=null)),species+" tree model loads with game materials");
    Check(Math.Abs(tree.Position.Y-Hoshikawa.World.Height(tree.Position.X,tree.Position.Z))<.01f,species+" tree is grounded");
}
```
(The pinned vector literal is the value printed in this step; it replaces the comment before the test is run.)

- [ ] **Step 2: Run, expect FAIL on "keyaki tree model loads".**
- [ ] **Step 3: Generate textures with Codex** (same invocation as Task 3, two prompts):
  - foliage atlas: "2x2 atlas of stylized hand-painted foliage cluster sprites for alpha-cut leaf cards, each quadrant one dense cluster filling ~85% of its cell on a perfectly flat pure #0000FF blue background, no shadows on the background: top-left fresh green zelkova leaves; top-right pale pink cherry blossoms with a few leaves; bottom-left dark blue-green Japanese cedar needle sprays; bottom-right slender bamboo leaves." → key with `python3 ~/.codex/skills/.system/imagegen/scripts/remove_chroma_key.py` (key #0000FF) → `foliage-atlas.png` RGBA; verify corners alpha 0.
  - bark atlas: "2x2 seamless tileable stylized painted bark swatches: grey mottled zelkova, red-brown fibrous cedar, dark banded cherry, green bamboo culm with nodes" → split with PIL into four 1024² files.
- [ ] **Step 4: Write `tools/build_props.py`** (runs with `--background --factory-startup`; `random.Random(seed)` per model for determinism):
  - Branch skeleton: recursive `branch(origin, direction, length, radius, depth)`; each branch is a polyline of 4–6 points with seeded jitter, skinned as a tapered 8-sided tube via bmesh with cylindrical UVs (u around, v = length/1.5); child branches spawn at 45–75 % of parent length.
  - Species parameters: keyaki (7 m, trunk splits at 2.2 m into 4–5 ascending limbs, vase/umbrella crown of 10–14 clumps r 1.1–1.6), sugi (11 m straight trunk, whorled short branches, 9–12 stacked clumps shrinking to a spire), sakura (5.5 m, trunk split at 1.4 m, wide horizontal limbs, 8–10 flattened clumps), bamboo (12–16 culms 5–8 m, r .04–.06, node rings, leaf cards at upper third), shrub (3–5 clumps within 1.2 m, 0.8–1.4 m tall, keyaki cell tinted).
  - Foliage clump: ~70 quads (0.7–1.1 m) scattered on a jittered sphere surface, random rotation, UVs into the species' atlas cell; custom normals = normalize(vertex − clump centre) blended 70 % with face normal; vertex colour R = sway (height fraction × distance from trunk), G = 0.75–1.0 ambient.
  - Materials: `bark_<species>` on tubes, `foliage` on cards (names only, no images).
  - Export each variant via `bpy.ops.export_scene.gltf(filepath=..., export_format='GLB', use_selection=True, export_vertex_color='ACTIVE', export_normals=True, export_materials='EXPORT', export_image_format='NONE')`; print triangle counts; keep each tree ≤ 6 k triangles.
  - Run: `/Applications/Blender.app/Contents/MacOS/Blender --background --factory-startup --python tools/build_props.py`.
- [ ] **Step 5: `foliage.gdshader`**

```glsl
shader_type spatial;
render_mode cull_disabled, diffuse_burley;
uniform sampler2D atlas : source_color, filter_linear_mipmap, repeat_disable;
instance uniform vec4 tint : source_color = vec4(1.0);
void vertex(){
    vec3 w=(MODEL_MATRIX*vec4(VERTEX,1.0)).xyz;
    VERTEX.xz+=COLOR.r*.12*vec2(sin(TIME*1.3+w.x*.35+w.z*.2),cos(TIME*1.1+w.z*.3));
}
void fragment(){
    vec4 t=texture(atlas,UV);
    ALBEDO=t.rgb*tint.rgb*COLOR.g; ALPHA=t.a; ALPHA_SCISSOR_THRESHOLD=.45;
    BACKLIGHT=t.rgb*.35; ROUGHNESS=.9;
}
```

- [ ] **Step 6: Rewrite `Tree()`**: cache `PackedScene`s; `float a=r.Randf(),b=r.Randf(),c=r.Randf()` (exactly three draws); variant = `(int)(a*variants)`; yaw = `c*Mathf.Tau`; tint = `new Color(.9f+b*.2f,.92f+b*.14f,.88f+b*.1f)`; assign per-surface overrides by material `ResourceName` (`foliage` → shared foliage `ShaderMaterial`, `bark_x` → `StandardMaterial3D{AlbedoTexture=bark-x.png}`), set `SetInstanceShaderParameter("tint",tint)` on foliage meshes; add trunk collider from `TreeTrunks`. Countryside: species `a<.7?"keyaki":"sugi"`; NorthernValley: `a<.75?"sugi":"keyaki"`; Komorebi courtyard: keyaki; orchard: `OrchardTree`.
- [ ] **Step 7: `--import`, headless self-test → PASS; capture `review_valley.gd` views and compare with `artifacts/countryside/`.**

### Task 5: Village dressing

**Files:**
- Modify: `scripts/Hamlet.cs` (`Dressing()`), `scripts/World.cs` (`Village()` lamps/grass/trees)
- Test: `scripts/IntegrationCheck.cs` (next to the rice-route check)

**Interfaces:**
- Consumes: `Tree(...)`, `StoneLantern(...)`, `Hamlet`, `BrookX`.

- [ ] **Step 1: Add corridor test**

```csharp
var space=g.GetWorld3D().DirectSpaceState;
foreach(var (a,b) in new[]{(new Vector2(0,31),new Vector2(0,15)),(new Vector2(0,15),new Vector2(17,15)),(new Vector2(22,15),new Vector2(34,15))})
    for(float t=0;t<=1;t+=.05f)
    {
        var p=a.Lerp(b,t);float y=Hoshikawa.World.Height(p.X,p.Y)+1;
        Check(space.IntersectRay(PhysicsRayQueryParameters3D.Create(new(p.X-.35f,y,p.Y),new(p.X+.35f,y,p.Y),1)).Count==0,$"village corridor clear at {p}");
    }
foreach(var tree in g.World.GetChildren().OfType<Node3D>().Where(n=>n.Position.Z>0&&n.Position.Z<76&&Math.Abs(n.Position.X)<24&&n.GetChildren().OfType<StaticBody3D>().Any()&&"keyaki sugi sakura bamboo shrub".Split(' ').Any(s=>n.Name.ToString().StartsWith(s))))
    Check(Math.Abs(tree.Position.Y-Hoshikawa.World.Height(tree.Position.X,tree.Position.Z))<.01f,"village tree grounded: "+tree.Name);
```

- [ ] **Step 2: Run → current state should PASS the corridor test (guard), then implement dressing and rerun.**
- [ ] **Step 3: Implement** (all positions through `Height()`, all draws from `villageRng`):
  - Square: well roof (4 posts + small gable `Roof`), notice board (2 posts, board, tiny roof, 5 paper notes), 2 benches, 2 `StoneLantern` at (-6.5,17.5)/(-6.5,29), sakura at (-7,27.5), planters.
  - Lamps: `Lamp()` at `RoadX(z)±3.9` for z ∈ {2,10,19,29,37,45}, alternating sides, skipping any within 1.5 m of a resident or the z∈[13.5,16.5] east corridor.
  - Trees: terrace keyaki ×4 (x∈[-30,-23]), bamboo ×3 (behind workshop, (-24,6..12)), sugi ×7 ring behind the shrine (r 8–11, z>63), sakura ×1 beside the honden, shrubs ×12 by house fronts (outside corridors/resident spots), keyaki ×2 east of the brook near the farmhouse.
  - Gardens: KOMA's plot x 10–15 z 10.3–12.8 (soil `GroundStrip`, tomato stakes + red spheres, cabbages); farmhouse plot.
  - Props: laundry pole with cloths by the clinic, firewood stack with board roof and straw rolls by the farmhouse, handcart by the kura, barrels/potted plants by doors, bamboo fences (posts + 3 horizontal culms) around gardens, stepping stones from road to each door (flat cylinders every 0.8 m).
  - Grass: 2500 blades in village lawns via `villageRng`, excluded within 3.8 m of `RoadX`, the square radius 8, pad rectangles (edge<0), brook |x−BrookX|<2.2, mound r<10.
- [ ] **Step 4: Headless self-test → PASS; capture and iterate composition.**

### Task 6: Review captures, graphical run, documentation

**Files:**
- Create: `tools/review_hamlet.gd` (views: square, workshop end, brook bridge, shrine approach, terrace, overview; writes `artifacts/hamlet/*.png`)
- Modify: `README.md`, `PLAN.md`, `VERIFICATION.md`, `ASSETS.md`, `CLAUDE.md`

- [ ] **Step 1: `review_hamlet.gd`** — copy the `review_valley.gd` structure with the six views above and `DirAccess.make_dir_recursive_absolute("res://artifacts/hamlet")`.
- [ ] **Step 2: Headless + graphical self-test** (`--fixed-fps 60`, drop `--headless` for graphical); both must print `SELF_TEST_PASS`; record check counts. During the graphical run log `Engine.GetFramesPerSecond()` once in the village with `--play` (no fixed fps) — if < 60, set `VisibilityRangeEnd=160` on tree meshes and rerun.
- [ ] **Step 3: Before/after composite** of `--overview` and `--portrait` vs `artifacts/village-before/`; inspect.
- [ ] **Step 4: Docs** — PLAN checklist section "Organic hamlet"; README per-file notes (`Architecture.cs`, `Hamlet.cs`, `build_props.py`, `foliage.gdshader`); VERIFICATION evidence + limits; ASSETS provenance for three generated textures; CLAUDE.md: `BrookX` is shared by `Height()`, the water strip, footbridge and minimap; new files; `build_props.py` command.
