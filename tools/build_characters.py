"""Executed inside Blender through Blender MCP. Original meshes and keyed rigs."""
import bpy, math, os
from mathutils import Vector

OUT = '/Users/chanwaichan/workspace/sirius/assets/models'
os.makedirs(OUT, exist_ok=True)

def material(name, color, metal=0, emission=0):
    m=bpy.data.materials.new(name); m.diffuse_color=(*color,1); m.use_nodes=True
    p=m.node_tree.nodes.get('Principled BSDF'); p.inputs['Base Color'].default_value=(*color,1)
    p.inputs['Roughness'].default_value=.72; p.inputs['Metallic'].default_value=metal
    if emission:
        p.inputs['Emission Color'].default_value=(*color,1); p.inputs['Emission Strength'].default_value=emission
    return m

gold=material('Antique brass',(.58,.36,.12),.6); steel=material('Brushed blade',(.53,.65,.69),.7)
robotmat=material('Patrol porcelain armor',(.49,.56,.49),.55)
robotdark=material('Patrol black mechanics',(.045,.07,.078),.75)
danger=material('Patrol amber lens',(1,.24,.025),.2,3)

def setup(name):
    scene=bpy.data.scenes.new(name); bpy.context.window.scene=scene
    scene.render.fps=30
    return scene

def mesh_finish(obj,name,mat,bone):
    obj.name=name; obj.data.materials.append(mat)
    if bone:
        obj.parent=rig
        group=obj.vertex_groups.new(name=bone); group.add(list(range(len(obj.data.vertices))),1,'REPLACE')
        mod=obj.modifiers.new('Character rig','ARMATURE'); mod.object=rig
    for poly in obj.data.polygons: poly.use_smooth=True
    return obj

def ell(name,loc,scale,mat,bone=None):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=16,ring_count=10,location=loc)
    o=bpy.context.object; o.scale=scale; bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    return mesh_finish(o,name,mat,bone)

def box(name,loc,scale,mat,bone=None,bevel=.035):
    bpy.ops.mesh.primitive_cube_add(size=1,location=loc); o=bpy.context.object; o.scale=scale
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    if bevel:
        mod=o.modifiers.new('Soft crafted edges','BEVEL'); mod.width=bevel; mod.segments=2
        bpy.ops.object.modifier_apply(modifier=mod.name)
    return mesh_finish(o,name,mat,bone)

def taper(name,a,b,r1,r2,mat,bone=None,verts=12):
    d=Vector(b)-Vector(a)
    bpy.ops.mesh.primitive_cone_add(vertices=verts,radius1=r1,radius2=r2,depth=d.length,location=(Vector(a)+Vector(b))*.5)
    o=bpy.context.object; o.rotation_mode='QUATERNION'; o.rotation_quaternion=d.to_track_quat('Z','Y')
    bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
    return mesh_finish(o,name,mat,bone)

def rings(name,levels,mat,bone):
    verts=[]; faces=[]; n=16
    for z,rx,ry in levels:
        for i in range(n):
            t=2*math.pi*i/n; verts.append((math.cos(t)*rx,math.sin(t)*ry,z))
    for j in range(len(levels)-1):
        for i in range(n):
            a=j*n+i; b=j*n+(i+1)%n; faces.append((a,b,b+n,a+n))
    faces.append(tuple(range(n-1,-1,-1))); faces.append(tuple(range((len(levels)-1)*n,len(levels)*n)))
    me=bpy.data.meshes.new(name); me.from_pydata(verts,[],faces); me.update()
    o=bpy.data.objects.new(name,me); bpy.context.collection.objects.link(o)
    return mesh_finish(o,name,mat,bone)

def armature(name,robot=False):
    global rig
    bpy.ops.object.armature_add(); rig=bpy.context.object; rig.name=name+'Rig'
    bpy.ops.object.mode_set(mode='EDIT'); eb=rig.data.edit_bones; eb.remove(eb[0])
    spec=[('Root',(0,0,0),(0,0,.2),None),('Hips',(0,0,.88),(0,0,1.03),'Root'),
          ('Spine',(0,0,1.03),(0,0,1.43),'Hips'),('Head',(0,0,1.43),(0,0,1.83),'Spine')]
    for side,s in [('L',-1),('R',1)]:
        spec += [(side+'Thigh',(s*.115,0,.9),(s*.12,0,.51),'Hips'),
                 (side+'Shin',(s*.12,0,.51),(s*.12,0,.12),side+'Thigh'),
                 (side+'Arm',(s*.25,0,1.39),(s*.34,0,1.10),'Spine'),
                 (side+'Forearm',(s*.34,0,1.10),(s*.37,-.01,.84),side+'Arm')]
    for n,h,t,p in spec:
        b=eb.new(n); b.head=h; b.tail=t
        if p: b.parent=eb[p]
    bpy.ops.object.mode_set(mode='OBJECT')
    for b in rig.pose.bones: b.rotation_mode='XYZ'
    return rig

def robot():
    name='patrol'; scene=setup(name); armature(name,True)
    box('Armored pelvis',(0,0,.96),(.36,.29,.22),robotdark,'Hips')
    rings('Tapered torso armor',[(1.03,.19,.14),(1.31,.28,.16),(1.46,.25,.13)],robotmat,'Spine')
    box('Chest inset',(0,-.15,1.29),(.27,.035,.16),robotdark,'Spine',.02)
    ell('Reactor lens',(0,-.181,1.29),(.07,.019,.07),danger,'Spine')
    box('Helmet',(0,0,1.67),(.37,.30,.34),robotmat,'Head',.09)
    box('Face visor',(0,-.152,1.69),(.29,.035,.09),robotdark,'Head',.016)
    box('Optical scanner',(0,-.176,1.69),(.21,.012,.031),danger,'Head',.008)
    taper('Antenna',(.13,.03,1.81),(.13,.03,2.09),.012,.005,steel,'Head')
    ell('Antenna lamp',(.13,.03,2.09),(.021,.021,.021),danger,'Head')
    for side,s in [('L',-1),('R',1)]:
        ell('Shoulder bearing',(s*.26,0,1.4),(.11,.11,.11),robotdark,side+'Arm')
        box('Shoulder plate',(s*.3,0,1.41),(.19,.31,.17),robotmat,side+'Arm')
        taper('Upper servo',(s*.28,0,1.34),(s*.34,0,1.1),.065,.055,steel,side+'Arm')
        ell('Elbow pivot',(s*.34,0,1.1),(.075,.08,.075),robotdark,side+'Forearm')
        box('Forearm armor',(s*.37,0,.98),(.16,.21,.22),robotmat,side+'Forearm')
        ell('Metal hand',(s*.37,-.02,.83),(.07,.07,.09),robotdark,side+'Forearm')
        taper('Thigh piston',(s*.12,0,.89),(s*.12,0,.53),.08,.064,steel,side+'Thigh')
        box('Thigh plate',(s*.12,-.04,.75),(.16,.18,.27),robotmat,side+'Thigh')
        ell('Knee joint',(s*.12,0,.51),(.09,.09,.09),robotdark,side+'Shin')
        box('Greave',(s*.12,0,.32),(.18,.22,.29),robotmat,side+'Shin')
        box('Robot foot',(s*.12,-.055,.09),(.20,.34,.15),robotdark,side+'Shin')
        for z in [1.18,1.38]:
            ell('Chest bolt',(s*.18,-.146,z),(.019,.012,.019),gold,'Spine')
    taper('Shock baton',(.37,-.01,.91),(.37,-.01,.14),.041,.047,robotdark,'RForearm')
    for z in [.25,.35,.45]:
        box('Baton live coil',(.37,-.01,z),(.10,.08,.04),danger,'RForearm',.008)
    box('Back battery',(0,.19,1.24),(.27,.18,.31),robotdark,'Spine')
    animate(rig); export(name,scene)

def animate(rig):
    clips={'idle':60,'walk':30,'run':22,'attack1':18,'attack2':20,'attack3':25,'guard':40,'dodge':20,'jump':32,'skill':38,'stagger':22,'death':45}
    for name,end in clips.items():
        rig.animation_data_create(); action=bpy.data.actions.new(name); rig.animation_data.action=action
        for frame in sorted(set([1,end//4,end//2,3*end//4,end])):
            t=(frame-1)/(end-1); wave=math.sin(t*math.tau); peak=math.sin(t*math.pi)
            for b in rig.pose.bones:
                b.rotation_euler=(0,0,0); b.location=(0,0,0)
            p=rig.pose.bones
            p['RArm'].rotation_euler.x=-.15; p['RForearm'].rotation_euler.x=-.18
            if name=='idle':
                p['Spine'].rotation_euler.x=wave*.025; p['Head'].rotation_euler.z=wave*.035
            elif name in ('walk','run'):
                swing=.5 if name=='walk' else .82
                for side,s in [('L',1),('R',-1)]:
                    p[side+'Thigh'].rotation_euler.x=wave*s*swing
                    p[side+'Shin'].rotation_euler.x=max(0,-wave*s)*.9
                    p[side+'Arm'].rotation_euler.x=-wave*s*swing*.7
                p['Spine'].rotation_euler.x=.12 if name=='run' else .035
                p['Hips'].location.y=abs(wave)*.035
            elif name.startswith('attack') or name=='skill':
                strength=1 if name=='attack1' else -1 if name=='attack2' else .65
                p['RArm'].rotation_euler.x=-peak*2.05
                p['RArm'].rotation_euler.z=math.sin(t*math.pi*2)*1.2*strength
                p['RForearm'].rotation_euler.x=-peak*.6
                p['Spine'].rotation_euler.y=math.sin(t*math.pi*2)*.62*strength
                p['LArm'].rotation_euler.x=-peak*.9
                p['Hips'].location.z=-peak*.1
            elif name=='guard':
                p['RArm'].rotation_euler.x=-1.2; p['RArm'].rotation_euler.z=-.45
                p['RForearm'].rotation_euler.x=-.95; p['LArm'].rotation_euler.x=-1.2
                p['LForearm'].rotation_euler.x=-1.1
            elif name=='dodge':
                p['Root'].rotation_euler.x=-t*math.tau
                p['Hips'].location.y=-peak*.3
                for s in ['L','R']:
                    p[s+'Thigh'].rotation_euler.x=-.7*peak; p[s+'Shin'].rotation_euler.x=1.4*peak
            elif name=='jump':
                p['LThigh'].rotation_euler.x=-peak*.6; p['RThigh'].rotation_euler.x=peak*.4
                p['RShin'].rotation_euler.x=peak*.8; p['LArm'].rotation_euler.x=-peak*.7
            elif name=='stagger':
                p['Spine'].rotation_euler.x=-peak*.45; p['Head'].rotation_euler.x=-peak*.25
            elif name=='death':
                p['Root'].rotation_euler.x=-min(t*2,1)*math.pi*.48
                p['Root'].location.y=min(t*2,1)*.32
            for b in rig.pose.bones:
                b.keyframe_insert(data_path='rotation_euler',frame=frame)
                b.keyframe_insert(data_path='location',frame=frame)
        track=rig.animation_data.nla_tracks.new(); track.name=name
        track.strips.new(name,1,action); track.mute=True
    rig.animation_data.action=None
    for b in rig.pose.bones: b.rotation_euler=(0,0,0); b.location=(0,0,0)

def export(name,scene):
    bpy.ops.object.select_all(action='SELECT')
    bpy.context.view_layer.objects.active=rig
    bpy.ops.export_scene.gltf(filepath=OUT+'/'+name+'.glb',export_format='GLB',use_selection=True,use_active_scene=True,
        export_animations=True,export_animation_mode='NLA_TRACKS',export_apply=False)
    print(name, 'exported',len(scene.objects),'objects')

robot()
# Reuse the current human pipeline; full regeneration must not restore the old puppets.
import runpy
runpy.run_path(OUT+'/../../tools/refine_humans.py',run_name='__main__')
print('CHARACTER_ASSETS_COMPLETE')
