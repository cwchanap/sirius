"""Run in Blender after building Mio: only the hairline may remain open."""
import bpy, bmesh, json, math
from pathlib import Path

scene=bpy.data.scenes['HumanV2_mio']
hair=next(o for o in scene.objects if o.type=='MESH' and
          any('Mio painted hair' in m.name for m in o.data.materials))
bm=bmesh.new();bm.from_mesh(hair.data)
edges=[e for e in bm.edges if e.is_boundary]
remaining={v for e in edges for v in e.verts};loops=[]
while remaining:
    pending=[remaining.pop()];group=[]
    while pending:
        v=pending.pop();group.append(hair.matrix_world@v.co)
        neighbors={e.other_vert(v) for e in v.link_edges if e.is_boundary}&remaining
        remaining-=neighbors;pending.extend(neighbors)
    loops.append(dict(vertices=len(group),min=[min(p[i] for p in group) for i in range(3)],
                      max=[max(p[i] for p in group) for i in range(3)]))
assert len(loops)==1 and loops[0]['vertices']==64, loops
assert loops[0]['min'][2]<1.65 and loops[0]['max'][2]<1.75, 'Unexpected crown opening'
assert all(math.isfinite(x) for v in bm.verts for x in v.co)
assert all(p.use_smooth for p in hair.data.polygons)
assert not any(e.is_wire or len(e.link_faces)>2 for e in bm.edges)
RESULT=dict(boundary_loops=loops,vertices=len(bm.verts),polygons=len(bm.faces),
            smooth_polygons=sum(p.use_smooth for p in hair.data.polygons),crown_closed=True)
bm.free()
if globals().get('OUTPUT'):Path(OUTPUT).write_text(json.dumps(RESULT,indent=2)+'\n')
print('MIO_HAIR_OK:',RESULT)
