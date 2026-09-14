"""Ray-check the eye layers immediately outside both eyelid silhouettes."""
import bpy,bmesh,math,json
from mathutils import Vector
scene=bpy.data.scenes['HumanV2_mio']
skin=next(o for o in scene.objects if o.name.startswith('Mio painted skin'))
eyes=[o for o in scene.objects if o.name.startswith(('Mio warm sclera','Mio illustrated amber eyes'))]
bm=bmesh.new();bm.from_mesh(skin.data)
loops={side:[(skin.matrix_world@e.verts[0].co,skin.matrix_world@e.verts[1].co) for e in bm.edges
    if e.is_boundary and all(side*v.co.x>0 and v.co.y<-.03 and 1.63<v.co.z<1.69 for v in e.verts)] for side in [-1,1]}
bm.free();samples=0;escapes=[]
for angle in [-45,0,45]:
 a=math.radians(angle);view=Vector((math.sin(a),-math.cos(a),0));right=Vector((math.cos(a),math.sin(a),0))
 for side,edges in loops.items():
  assert len(edges)>40
  center=sum((a+b for a,b in edges),Vector())/(2*len(edges))
  for a,b in edges:
   mid=(a+b)/2;radial=mid-center
   tangent=b-a;normal=right*tangent.z-Vector((0,0,tangent.dot(right)))
   if normal.dot(radial)<0:normal=-normal
   normal.normalize()
   for offset in [.00015,.00035,.0007]:
    origin=mid+normal*offset+view*.3
    distances={}
    for obj in [skin]+eyes:
     inv=obj.matrix_world.inverted();hit,p,_,_=obj.ray_cast(inv@origin,inv.to_3x3()@-view)
     if hit:distances[obj.name]=(obj.matrix_world@p-origin).length
    depth=distances.get(skin.name,1)
    for eye in eyes:
     if distances.get(eye.name,1)<depth-.00005:escapes.append((angle,side,offset,eye.name,tuple(mid)))
    samples+=1
RESULT={'samples':samples,'escapes':len(escapes),'examples':escapes[:8]}
assert not escapes, RESULT
print('MIO_EYE_RIM_OK:',json.dumps(RESULT))
