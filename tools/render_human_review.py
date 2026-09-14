"""Render the actual Blender models in a temporary studio, without altering exports."""
import bpy, os, math
from mathutils import Vector

ROOT='/Users/chanwaichan/workspace/sirius'

def render(model='mio',pose=None,close=False,view=None,clay=False,hair=True,angle=None,elevation=0):
    source=bpy.data.scenes['HumanV2_'+model]
    studio=bpy.data.scenes.new('Temporary character review')
    bpy.context.window.scene=studio
    for o in source.objects:
        if not hair and o.type=='MESH' and any('hair' in m.name.lower() for m in o.data.materials):continue
        studio.collection.objects.link(o)
    rig=next(o for o in source.objects if o.type=='ARMATURE')
    rig.animation_data.action=None
    for b in rig.pose.bones:b.rotation_euler=(0,0,0);b.location=(0,0,0)
    bpy.context.view_layer.update()
    if pose:
        track=next(t for t in rig.animation_data.nla_tracks if t.name==pose)
        rig.animation_data.action=track.strips[0].action
        studio.frame_set(9 if pose!='guard' else 1)
    studio.render.engine='CYCLES';studio.cycles.samples=24;studio.cycles.use_denoising=True
    studio.render.resolution_x=1000;studio.render.resolution_y=1100;studio.render.resolution_percentage=100
    studio.world=bpy.data.worlds.new('Review world');studio.world.use_nodes=True
    studio.world.node_tree.nodes['Background'].inputs[0].default_value=(.24,.28,.34,1)
    studio.world.node_tree.nodes['Background'].inputs[1].default_value=.5
    studio.view_settings.view_transform='AgX'
    if clay:
        material=bpy.data.materials.new('Review clay');material.use_nodes=True
        bsdf=material.node_tree.nodes.get('Principled BSDF');bsdf.inputs['Base Color'].default_value=(.34,.39,.42,1);bsdf.inputs['Roughness'].default_value=.8
        studio.view_layers[0].material_override=material
    added=[]
    for loc,power,size in [((-3,-4,5),420,4),((3,-2,3),240,3),((1,3,4),500,3)]:
        data=bpy.data.lights.new('Studio softbox','AREA');data.energy=power;data.shape='DISK';data.size=size
        o=bpy.data.objects.new('Studio softbox',data);studio.collection.objects.link(o);o.location=loc
        o.rotation_euler=(Vector((0,0,1.0))-o.location).to_track_quat('-Z','Y').to_euler();added.append(o)
    bpy.ops.mesh.primitive_plane_add(size=200)
    floor=bpy.context.object;floor.location.z=-.007;added.append(floor)
    m=bpy.data.materials.new('Review matte backdrop');m.diffuse_color=(.07,.085,.10,1);floor.data.materials.append(m)
    data=bpy.data.cameras.new('Review camera');cam=bpy.data.objects.new('Review camera',data);studio.collection.objects.link(cam);added.append(cam)
    target=Vector((0,0,1.65 if close else .96))
    cam.location=(.35,-2.8,1.78) if close else (2.3,-6,2.8)
    cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();data.type='ORTHO';data.ortho_scale=.56 if close else 2.18
    if view=='front':
        target=Vector((0,0,.93));cam.location=(0,-6,.93);data.ortho_scale=2.08
    elif view=='side':
        target=Vector((0,0,.93));cam.location=(6,0,.93);data.ortho_scale=2.08
    elif view=='keyart':
        target=Vector((0,0,1.37));cam.location=(2.8,-4,1.7);data.ortho_scale=1.15
    elif view=='back':
        target=Vector((0,0,.96));cam.location=(-2,6,2.3);data.ortho_scale=2.18
    elif view=='profile':
        target=Vector((0,0,1.65));cam.location=(4,-.4,1.74);data.ortho_scale=.59
    elif view=='threequarter':
        target=Vector((0,0,.93));cam.location=(3.5,-6,.93);data.ortho_scale=2.08
    elif view=='back-ortho':
        target=Vector((0,0,.93));cam.location=(0,6,.93);data.ortho_scale=2.08
    elif view=='profile-ortho':
        target=Vector((0,0,1.675));cam.location=(6,0,1.675);data.ortho_scale=.48
    elif view=='face-front':
        target=Vector((0,0,1.675));cam.location=(0,-6,1.675);data.ortho_scale=.48
    elif view=='head-threequarter':
        target=Vector((0,0,1.675));cam.location=(3.5,-6,1.675);data.ortho_scale=.48
    elif view=='satchel':
        target=Vector((.12,0,1.03));cam.location=(1.7,-3,1.5);data.ortho_scale=.60
    elif view=='boots':
        target=Vector((0,0,.30));cam.location=(1,-3,1.0);data.ortho_scale=.83
    if angle is not None:
        target=Vector((0,0,1.67));a=math.radians(angle);e=math.radians(elevation)
        cam.location=target+Vector((math.sin(a)*math.cos(e),-math.cos(a)*math.cos(e),math.sin(e)))
        data.ortho_scale=.42
    cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler()
    studio.camera=cam
    suffix=view or ('face' if close else pose or 'full')
    if angle is not None:suffix=f'angle{angle}-elev{elevation}'
    if clay:suffix+='-clay'
    studio.render.filepath=ROOT+'/artifacts/'+model+'-'+suffix+'.png'
    bpy.ops.render.render(write_still=True)
    rig.animation_data.action=None
    for b in rig.pose.bones:b.rotation_euler=(0,0,0);b.location=(0,0,0)
    for o in added:bpy.data.objects.remove(o,do_unlink=True)
    bpy.context.window.scene=source;bpy.data.scenes.remove(studio)
    print(model,suffix,'rendered')

if __name__=='__main__':
    render(pose='idle');render(close=True);render(pose='guard');render(pose='run');render('villager',pose='idle')
