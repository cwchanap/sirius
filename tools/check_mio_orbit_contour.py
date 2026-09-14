"""Check eye-level bulges in the actual forehead-to-cheek silhouette in Blender."""
import bpy,json,math
from pathlib import Path
scene=bpy.data.scenes['HumanV2_mio']
objects=[o for o in scene.objects if o.type=='MESH' and o.name.startswith(('Mio painted skin','Mio warm sclera','Mio illustrated amber eyes'))]
edges=[]
for obj in objects:
 points=[obj.matrix_world@v.co for v in obj.data.vertices]
 edges.extend((points[e.vertices[0]],points[e.vertices[1]]) for e in obj.data.edges
  if min(points[e.vertices[0]].z,points[e.vertices[1]].z)<1.688 and max(points[e.vertices[0]].z,points[e.vertices[1]].z)>1.640)
sections=[]
for i in range(97):
 z=1.640+i*.0005
 cuts=[a.lerp(b,(z-a.z)/(b.z-a.z)) for a,b in edges if min(a.z,b.z)<=z<max(a.z,b.z)]
 assert cuts
 sections.append((z,cuts))
RESULT={}
for angle in [-70,-60,-45,45,60,70]:
 a=math.radians(angle);sign=-1 if angle>0 else 1
 rows=[(z,max(sign*(p.x*math.cos(a)+p.y*math.sin(a)) for p in points)) for z,points in sections]
 bulge=max(x-(rows[0][1]+(rows[-1][1]-rows[0][1])*i/(len(rows)-1)) for i,(_,x) in enumerate(rows))
 RESULT[str(angle)]={'outward_bulge_mm':bulge*1000,'outline':rows}
if globals().get('OUTPUT'):Path(OUTPUT).write_text(json.dumps(RESULT,indent=2)+'\n')
print('ORBIT_CONTOUR', {a:round(v['outward_bulge_mm'],3) for a,v in RESULT.items()})
if globals().get('ENFORCE',True):
 assert max(v['outward_bulge_mm'] for v in RESULT.values())<1.0, 'Eye-level face contour protrudes beyond forehead-to-cheek transition'
 print('MIO_ORBIT_CONTOUR_OK')
