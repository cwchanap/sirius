"""Fit the existing deformable surfaces to Mio's authored turnaround landmarks.

Run inside Blender through refine_humans.py. Reference planes live in a separate
scene and are never selected for the runtime GLB export.
"""
import bpy, json, math
from pathlib import Path
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[1]
LANDMARKS=json.loads((ROOT/'assets/art/mio-turnaround-landmarks.json').read_text())
BODY=LANDMARKS['body']
BODY_SCALE=(LANDMARKS['runtime_height_m']-.006)/(BODY['ground_y']-BODY['crown_y'])
WAIST_Z=.006+(BODY['ground_y']-BODY['waist_y'])*BODY_SCALE
SHOULDER_Z=.006+(BODY['ground_y']-BODY['shoulder_y'])*BODY_SCALE-.030

def lerp(z,rows):
    if z<=rows[0][0]:return rows[0][1]
    for (a,x),(b,y) in zip(rows,rows[1:]):
        if z<=b:return x+(y-x)*(z-a)/(b-a)
    return rows[-1][1]

def smooth(t):
    t=max(0,min(1,t));return t*t*(3-2*t)

def sheet_position(p,arm_override=None):
    x,y,z=p
    # Crown and floor stay fixed; the waist and shoulder line follow the sheet.
    zz=lerp(z,[(0,0),(.006,.006),(.585,.565),(1.012,1.04),
        (1.112,WAIST_Z),(1.345,1.375),(1.413,SHOULDER_Z),(1.50,1.518),
        (1.548,1.560),(1.650,1.657),(1.782,1.782),(1.85,1.85)])
    sx=lerp(z,[(0,.94),(.60,.94),(1.01,.90),(1.18,.76),
        (1.35,.90),(1.435,.86),(1.51,1),(1.85,1)])
    inside=lerp(z,[(0,.23),(1.05,.23),(1.30,.17),(1.44,.115),(1.53,.09)])
    arm=smooth((abs(x)-inside)/.07)*(1-smooth((z-1.47)/.07))
    if arm_override is not None:arm=arm_override
    zz-=arm*.080*smooth((1.32-z)/.30)
    center=lerp(z,[(0,abs(x)),(.99,abs(x)),(1.06,.338),(1.18,.289),(1.30,.230),(1.42,.169),(1.53,.06)])
    upper=smooth((z-1.05)/.24)
    ax=center*(1-.20*upper)+(abs(x)-center)*(1-.38*upper)
    xx=x*sx*(1-arm)+math.copysign(ax,x)*arm
    # Broaden the shoulder girdle without widening the neck, waist or hands.
    xx*=1+.12*math.exp(-((z-1.405)/.105)**2)*smooth((abs(x)-.045)/.075)
    # Head-sheet eye spacing is 76 mm; apply the same fit to eyes, hair and bones.
    xx*=1-.12*smooth((z-1.515)/.090)
    yy=y*(1-.30*arm*upper)
    if y>0:yy*=1-.14*smooth((z-1.52)/.10)
    return Vector((xx,yy,zz))

def fit_to_sheet():
    bag_pivot=Vector((.238,-.007,.956))
    bag_offset=sheet_position(bag_pivot,arm_override=0)-bag_pivot
    for o in bpy.context.scene.objects:
        if o.type!='MESH':continue
        inv=o.matrix_world.inverted()
        is_bag=o.name.startswith(('Illustrated utility satchel','Satchel'))
        is_strap=o.name.startswith(('Flat leather shoulder strap','Shoulder strap seam'))
        for v in o.data.vertices:
            p=o.matrix_world@v.co
            fitted=p+bag_offset if is_bag else sheet_position(p)
            if is_strap:fitted=fitted.lerp(p+bag_offset,smooth((1.12-p.z)/.071))
            v.co=inv@fitted
        o.data.update()
    bpy.ops.object.select_all(action='DESELECT');RIG.select_set(True)
    bpy.context.view_layer.objects.active=RIG;bpy.ops.object.mode_set(mode='EDIT')
    for b in RIG.data.edit_bones:
        b.head=sheet_position(b.head);b.tail=sheet_position(b.tail)
    bpy.ops.object.mode_set(mode='OBJECT')
    # The reference fit must move bones and meshes through the same mapping.
    assert abs(RIG.data.bones['Head'].head_local.z-sheet_position(Vector((0,.020,1.561))).z)<1e-5
    assert sheet_position(Vector((.14,0,1.18))).x<.125
    assert sheet_position(Vector((0,0,1.112))).z>1.15
    spacing=abs(RIG.data.bones['LEye'].head_local.x-RIG.data.bones['REye'].head_local.x)
    assert .075<spacing<.080, ('Head-sheet eye spacing',spacing)

def reference_scene():
    name='MioTurnaroundGuides'
    if name in bpy.data.scenes:
        old=bpy.data.scenes[name]
        for o in list(old.objects):
            if len(o.users_scene)==1:bpy.data.objects.remove(o,do_unlink=True)
        bpy.data.scenes.remove(old)
    scene=bpy.data.scenes.new(name)
    source=bpy.data.scenes['HumanV2_mio']
    model=bpy.data.collections.new('Current model');scene.collection.children.link(model)
    for o in source.objects:model.objects.link(o)
    scale=1.799/906
    specs=[('Body front','mio-turnaround-body-v1.png',(1536,1024),(48,20,414,944),233,937,scale,'front',.006),
        ('Body side','mio-turnaround-body-v1.png',(1536,1024),(421,20,687,944),540,937,scale,'side',.006),
        ('Body back','mio-turnaround-body-v1.png',(1536,1024),(695,20,1055,944),875,937,scale,'back',.006),
        ('Head front','mio-turnaround-head-v1.png',(2048,683),(28,25,476,430),249,273,.00073,'front',1.657),
        ('Head profile','mio-turnaround-head-v1.png',(2048,683),(517,44,1012,440),700,273,.00073,'side',1.657)]
    for label,file,size,box,cx,cy,s,view,z0 in specs:
        collection=bpy.data.collections.new(label+' reference');scene.collection.children.link(collection)
        if label!='Body front':collection.hide_viewport=True
        x0,y0,x1,y1=box;points=[]
        for x,y in [(x0,y1),(x1,y1),(x1,y0),(x0,y0)]:
            u=(x-cx)*s;z=z0+(cy-y)*s
            points.append((u,.33,z) if view=='front' else (-.50,u,z) if view=='side' else (-u,-.33,z))
        data=bpy.data.meshes.new(label);data.from_pydata(points,[],[(0,1,2,3)]);data.update()
        o=bpy.data.objects.new(label,data);collection.objects.link(o)
        uv=data.uv_layers.new(name='UVMap')
        coords=[(x0/size[0],1-y1/size[1]),(x1/size[0],1-y1/size[1]),(x1/size[0],1-y0/size[1]),(x0/size[0],1-y0/size[1])]
        for i,p in enumerate(coords):uv.data[i].uv=p
        material=bpy.data.materials.new(label+' illustration');material.use_nodes=True
        nodes=material.node_tree.nodes;nodes.clear()
        tex=nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(ROOT/'assets/art'/file),check_existing=True)
        emission=nodes.new('ShaderNodeEmission');out=nodes.new('ShaderNodeOutputMaterial')
        material.node_tree.links.new(tex.outputs['Color'],emission.inputs[0]);material.node_tree.links.new(emission.outputs[0],out.inputs[0])
        data.materials.append(material);o.hide_render=True
        o['source_box_pixels']=list(box);o['meters_per_display_pixel']=s
    scene['landmarks']=str(ROOT/'assets/art/mio-turnaround-landmarks.json')
    return scene
