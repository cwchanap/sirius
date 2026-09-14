"""Build deformable anime humans in Blender; run through Blender MCP.

Blender Z is up and -Y is forward. Runtime clip names and Godot scale stay stable.
The mechanical patrol is deliberately outside this human-only asset revision.
"""
import bpy, math, json
from mathutils import Vector

ROOT='/Users/chanwaichan/workspace/sirius'
OUT=ROOT+'/assets/models'
PREFIX='HumanV2_'
RIG=None

def smooth(t):
    t=max(0,min(1,t)); return t*t*(3-2*t)

def mat(name,color,rough=.65,metal=0,emission=0):
    m=bpy.data.materials.new(PREFIX+name);m.use_nodes=True;m.diffuse_color=(*color,1)
    p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(*color,1)
    p.inputs['Roughness'].default_value=rough;p.inputs['Metallic'].default_value=metal
    if emission:
        p.inputs['Emission Color'].default_value=(*color,1);p.inputs['Emission Strength'].default_value=emission
    return m

SKIN=mat('warm skin',(.72,.47,.34),.68)
LIP=mat('rose lips',(.40,.16,.14),.65)
EAR=mat('ear warmth',(.57,.28,.22))
INK=mat('ink trousers',(.022,.034,.055),.85)
TEAL=mat('petrol coat',(.025,.15,.17),.74)
LINEN=mat('ivory linen',(.72,.66,.51),.9)
RED=mat('vermilion woven scarf',(.48,.055,.035),.9)
LEATHER=mat('oxblood leather',(.105,.04,.025),.65)
SOLE=mat('boot rubber',(.018,.021,.026),.9)
GOLD=mat('antique brass',(.56,.32,.10),.35,.68)
HAIR=mat('midnight hair',(.012,.020,.034),.72)
HAIRLIGHT=mat('hair ribbon highlights',(.028,.044,.065),.7)
LASH=mat('eyelashes',(.009,.009,.014),.9)
WHITE=mat('warm eye white',(.9,.87,.79),.38)
STEEL=mat('blade steel',(.43,.55,.61),.25,.72)
SIGNAL=mat('jade terminal',(.02,.62,.43),.3,.15,.65)

def mesh(name,vertices,faces,material):
    data=bpy.data.meshes.new(name);data.from_pydata(vertices,[],faces);data.update()
    o=bpy.data.objects.new(name,data);bpy.context.collection.objects.link(o);data.materials.append(material)
    for p in data.polygons:p.use_smooth=True
    return o

def bind(o,weights):
    groups={}
    for v in o.data.vertices:
        w=weights(o.matrix_world@v.co) if callable(weights) else {weights:1}
        w={k:v for k,v in w.items() if v>.0001}; total=sum(w.values())
        assert total>0,(o.name,v.index)
        for name,value in w.items():
            if name not in groups:groups[name]=o.vertex_groups.new(name=name)
            groups[name].add([v.index],value/total,'REPLACE')
    o.parent=RIG
    mod=o.modifiers.new('Smooth anatomical skinning','ARMATURE');mod.object=RIG
    return o

def uvball(name,center,scale,material,weights=None):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=32,ring_count=20,location=center)
    o=bpy.context.object;o.name=name;o.scale=scale
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    o.data.materials.append(material)
    for p in o.data.polygons:p.use_smooth=True
    return bind(o,weights) if weights else o

def softbox(name,center,size,material,weights,bevel=.01):
    bpy.ops.mesh.primitive_cube_add(size=1,location=center);o=bpy.context.object;o.name=name;o.scale=size
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    mod=o.modifiers.new('Rounded edges','BEVEL');mod.width=bevel;mod.segments=3
    bpy.ops.object.modifier_apply(modifier=mod.name)
    o.data.materials.append(material)
    for p in o.data.polygons:p.use_smooth=True
    return bind(o,weights)

def tube(name,sections,material,n=24):
    """Closed loft: each section is center, two radii, with an anatomical axis."""
    vs=[];fs=[]
    for j,(center,rx,ry) in enumerate(sections):
        c=Vector(center)
        tangent=Vector(sections[min(j+1,len(sections)-1)][0])-Vector(sections[max(j-1,0)][0])
        tangent.normalize();side=Vector((1,0,0));side=(side-tangent*side.dot(tangent)).normalized()
        depth=tangent.cross(side).normalized()
        for i in range(n):
            a=i*math.tau/n;vs.append(tuple(c+side*math.cos(a)*rx+depth*math.sin(a)*ry))
    for j in range(len(sections)-1):
        for i in range(n):a=j*n+i;b=j*n+(i+1)%n;fs.append((a,b,b+n,a+n))
    fs.append(tuple(range(n-1,-1,-1)));fs.append(tuple(range((len(sections)-1)*n,len(sections)*n)))
    return mesh(name,vs,fs,material)

def union(name,objects,material,voxel=.008):
    bpy.ops.object.select_all(action='DESELECT')
    for o in objects:o.select_set(True)
    bpy.context.view_layer.objects.active=objects[0];bpy.ops.object.join();o=bpy.context.object;o.name=name
    mod=o.modifiers.new('Continuous garment topology','REMESH');mod.mode='VOXEL';mod.voxel_size=voxel;mod.use_smooth_shade=True
    bpy.ops.object.modifier_apply(modifier=mod.name)
    mod=o.modifiers.new('Tailored surface','SMOOTH');mod.factor=.65;mod.iterations=5;bpy.ops.object.modifier_apply(modifier=mod.name)
    mod=o.modifiers.new('Game mesh','DECIMATE');mod.ratio=.52;bpy.ops.object.modifier_apply(modifier=mod.name)
    o.data.materials.clear();o.data.materials.append(material)
    for p in o.data.polygons:p.use_smooth=True
    return o

def curve(name,points,radius,material,weights):
    c=bpy.data.curves.new(name,'CURVE');c.dimensions='3D';c.resolution_u=5;c.bevel_depth=radius;c.bevel_resolution=2
    s=c.splines.new('BEZIER');s.bezier_points.add(len(points)-1)
    for v,p in zip(s.bezier_points,points):v.co=p;v.handle_left_type='AUTO';v.handle_right_type='AUTO'
    o=bpy.data.objects.new(name,c);bpy.context.collection.objects.link(o);o.data.materials.append(material)
    bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o;bpy.ops.object.convert(target='MESH')
    return bind(bpy.context.object,weights)

def blend(a,b,t):return {a:1} if a==b else {a:1-smooth(t),b:smooth(t)}

def axial(z,points):
    if z<=points[0][0]:return {points[0][1]:1}
    for (a,ba),(b,bb) in zip(points,points[1:]):
        if z<=b:return blend(ba,bb,(z-a)/(b-a))
    return {points[-1][1]:1}

def upper_weights(v):
    body=axial(v.z,[(1.02,'Hips'),(1.19,'Spine'),(1.40,'Chest'),(1.56,'Neck')])
    amount=smooth((abs(v.x)-.115)/.105)
    amount*=max(smooth((abs(v.x)-.19)/.06),smooth((v.z-1.19)/.17))
    side='L' if v.x<0 else 'R'
    arm=axial(v.z,[(1.15,side+'Forearm'),(1.30,side+'Arm'),(1.49,side+'Clavicle')])
    return {**{k:x*(1-amount) for k,x in body.items()},**{k:x*amount for k,x in arm.items()}}

def leg_weights(v):
    side='L' if v.x<0 else 'R'
    return axial(v.z,[(.45,side+'Shin'),(.67,side+'Thigh'),(.92,side+'Thigh'),(1.07,'Hips')])

def foot_weights(v,side):
    if v.z>.20:return {side+'Shin':1}
    if v.z>.095:return blend(side+'Foot',side+'Shin',(v.z-.095)/.105)
    return blend(side+'Foot',side+'Toe',(-v.y-.07)/.09)

def segment_distance(point,a,b):
    d=b-a;t=max(0,min(1,(point-a).dot(d)/d.length_squared));return (point-(a+t*d)).length

def hand_weights(v,side):
    if v.z>.995:return blend(side+'Hand',side+'Forearm',(v.z-.995)/.05)
    if v.z>.975:return {side+'Hand':1}
    names=[side+'Hand']+[side+finger+str(i) for finger in ['Thumb','Index','Middle','Ring','Pinky'] for i in range(1,4)]
    distances=[]
    for name in names:
        b=RIG.data.bones[name];distances.append((segment_distance(v,b.head_local,b.tail_local),name))
    closest=sorted(distances)[:3];ws={name:1/(.005+d)**5 for d,name in closest};total=sum(ws.values())
    return {name:w/total for name,w in ws.items()}

def human_rig(name):
    global RIG
    bpy.ops.object.armature_add();RIG=bpy.context.object;RIG.name=name+'AnatomicalRig';RIG.show_in_front=True
    bpy.ops.object.mode_set(mode='EDIT');eb=RIG.data.edit_bones;eb.remove(eb[0])
    specs=[('Root',(0,0,0),(0,0,.2),None),('Hips',(0,0,.99),(0,0,1.10),'Root'),
        ('Spine',(0,0,1.10),(0,0,1.28),'Hips'),('Chest',(0,0,1.28),(0,0,1.48),'Spine'),
        ('Neck',(0,0,1.48),(0,0,1.61),'Chest'),('Head',(0,0,1.61),(0,0,1.84),'Neck'),
        ('Jaw',(0,-.025,1.66),(0,-.075,1.61),'Head'),
        ('HairBase',(0,.12,1.77),(0,.22,1.53),'Head'),('HairTip',(0,.22,1.53),(0,.21,1.32),'HairBase'),
        ('Scarf',(0,.07,1.49),(.08,.13,1.17),'Chest')]
    for side,s in [('L',-1),('R',1)]:
        specs.extend([(side+'Eye',(s*.052,-.10,1.714),(s*.052,-.15,1.714),'Head'),
            (side+'Clavicle',(s*.025,0,1.45),(s*.18,0,1.47),'Chest'),
            (side+'Arm',(s*.18,0,1.47),(s*.30,-.005,1.23),side+'Clavicle'),
            (side+'Forearm',(s*.30,-.005,1.23),(s*.342,-.018,1.015),side+'Arm'),
            (side+'Hand',(s*.342,-.018,1.015),(s*.355,-.025,.944),side+'Forearm'),
            (side+'Thigh',(s*.094,0,.99),(s*.104,-.018,.565),'Hips'),
            (side+'Shin',(s*.104,-.018,.565),(s*.104,.014,.14),side+'Thigh'),
            (side+'Foot',(s*.104,.014,.14),(s*.104,-.10,.062),side+'Shin'),
            (side+'Toe',(s*.104,-.10,.062),(s*.104,-.19,.059),side+'Foot')])
        for finger,offset,length in [('Index',-.025,.078),('Middle',-.008,.087),('Ring',.010,.079),('Pinky',.026,.060)]:
            start=Vector((s*(.355+offset),-.027,.948-abs(offset)*.18))
            for i in range(1,4):
                end=start+Vector((s*offset*.09,-.001,-length/3))
                specs.append((side+finger+str(i),tuple(start),tuple(end),side+'Hand' if i==1 else side+finger+str(i-1)));start=end
        start=Vector((s*.328,-.023,.981))
        for i,d in enumerate([(-s*.017,-.012,-.020),(-s*.005,-.011,-.019),(s*.002,-.004,-.016)],1):
            end=start+Vector(d);specs.append((side+'Thumb'+str(i),tuple(start),tuple(end),side+'Hand' if i==1 else side+'Thumb'+str(i-1)));start=end
    for name,a,b,parent in specs:
        bone=eb.new(name);bone.head=a;bone.tail=b
        if parent:bone.parent=eb[parent]
    bpy.ops.object.mode_set(mode='OBJECT')
    for bone in RIG.pose.bones:bone.rotation_mode='XYZ'

def garments(hero):
    material=TEAL if hero else mat('resident work jacket',(.22,.29,.18))
    torso=tube('Jacket torso',[( (0,0,z),x,y) for z,x,y in [(1.015,.17,.105),(1.07,.155,.093),(1.15,.122,.09),(1.23,.131,.095),(1.31,.147,.115),(1.38,.163,.112),(1.445,.174,.081),(1.48,.116,.068),(1.50,.064,.057)]],material,40)
    pieces=[torso]
    for side,s in [('L',-1),('R',1)]:
        sections=[((s*x,y,z),rx,ry) for x,y,z,rx,ry in [(.155,0,1.455,.008,.008),(.180,0,1.430,.053,.062),(.205,0,1.411,.066,.070),(.233,0,1.379,.063,.065),(.267,-.003,1.31,.056,.057),(.293,-.005,1.254,.050,.052),(.305,-.007,1.218,.049,.048),(.319,-.009,1.16,.047,.048),(.334,-.014,1.08,.037,.039),(.342,-.018,1.028,.031,.032)]]
        pieces.append(tube('Sleeve',sections,material))
    bind(union('Travel coat - continuous shoulders and elbows',pieces,material,.007),upper_weights)
    # Shirt follows the torso curvature, with a pointed placket and folded lapels.
    vs=[];fs=[]
    for z,width,depth,rx in [(1.22,.005,.098,.13),(1.28,.045,.110,.14),(1.34,.059,.115,.155),(1.39,.065,.112,.165),(1.44,.056,.084,.174),(1.47,.050,.074,.14),(1.492,.045,.062,.085)]:
        for x in [-width,0,width]:vs.append((x,-depth*math.sqrt(1-(x/rx)**2)-.007,z))
    for j in range(6):
        for i in range(2):a=j*3+i;fs.append((a,a+1,a+4,a+3))
    o=mesh('Ivory shirt and placket',vs,fs,LINEN)
    mod=o.modifiers.new('Shirt thickness','SOLIDIFY');mod.thickness=.004
    bpy.context.view_layer.objects.active=o;o.select_set(True);bpy.ops.object.modifier_apply(modifier=mod.name);bind(o,upper_weights)
    for s in [-1,1]:
        verts=[(s*.048,-.089,1.492),(s*.119,-.084,1.461),(s*.069,-.132,1.349),(s*.018,-.12,1.394)]
        o=mesh('Folded jacket lapel',verts,[(0,1,2,3)],material)
        mod=o.modifiers.new('Lapel lining','SOLIDIFY');mod.thickness=.005
        bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=mod.name);bind(o,upper_weights)
        curve('Gold lapel piping',[verts[0],verts[3],verts[2]],.0017,GOLD,upper_weights)
        curve('Tailored front seam',[(s*.07,-.128,1.34),(s*.082,-.09,1.18),(s*.098,-.102,1.035)],.0014,GOLD,upper_weights)
        curve('Jacket waist pocket',[(s*.078,-.102,1.135),(s*.123,-.079,1.153)],.003,LEATHER,upper_weights)
    for z in [1.32,1.275,1.23]:uvball('Shirt button',(0,-.12 if z>1.28 else -.105,z),(.006,.003,.006),GOLD,upper_weights)
    # The fitted jacket ends at the belt so the hip joint remains free in combat.
    pants=[uvball('Trouser pelvis',(0,0,.984),(.17,.105,.12),INK)]
    for side,s in [('L',-1),('R',1)]:
        sections=[((s*x,y,z),rx,ry) for x,y,z,rx,ry in [(.087,0,1.015,.081,.096),(.098,0,.95,.086,.086),(.105,.003,.875,.079,.083),(.105,.004,.77,.069,.072),(.104,-.004,.67,.058,.065),(.104,-.019,.565,.049,.053),(.104,-.01,.515,.050,.054),(.104,.004,.44,.057,.059),(.104,.013,.35,.051,.056),(.104,.015,.25,.035,.043),(.104,.014,.145,.028,.033)]]
        pants.append(tube('Trouser leg',sections,INK))
    bind(union('Trousers - blended hips and knees',pants,INK,.007),leg_weights)
    neck=tube('Neck and neckline',[((0,0,z),rx,ry) for z,rx,ry in [(1.465,.064,.058),(1.505,.044,.041),(1.55,.04,.039),(1.60,.047,.047),(1.625,.056,.057)]],SKIN,32)
    bind(neck,lambda v:axial(v.z,[(1.49,'Chest'),(1.55,'Neck'),(1.63,'Head')]))
    # Belt follows the waist, with eyelets, a real buckle, and a fitted satchel.
    belt=tube('Leather waist belt',[((0,0,1.02),.171,.109),((0,0,1.06),.171,.109)],LEATHER,48);bind(belt,'Hips')
    softbox('Rectangular belt buckle',(0,-.116,1.04),(.055,.018,.047),GOLD,'Hips',.006)
    softbox('Buckle inset',(0,-.128,1.04),(.034,.006,.027),LEATHER,'Hips',.003)
    for i in range(3):uvball('Belt eyelet',(.047+i*.019,-.109,1.04),(.003,.002,.004),GOLD,'Hips')
    softbox('Travel satchel',(-.208,.006,1.012),(.135,.115,.16),LEATHER,'Hips',.025)
    softbox('Satchel flap',(-.208,-.055,1.053),(.134,.018,.087),LEATHER,'Hips',.015)
    softbox('Satchel clasp',(-.208,-.069,1.025),(.024,.007,.027),GOLD,'Hips',.004)
    curve('Satchel stitched edge',[(-.268,-.067,1.08),(-.268,-.072,1.021),(-.208,-.074,1.01),(-.148,-.07,1.02)],.0014,LINEN,'Hips')
    if not hero:
        apron=mesh('Linen work apron',[(-.139,-.112,1.046),(.139,-.112,1.046),(.145,-.136,.785),(.071,-.151,.75),(-.071,-.151,.75),(-.145,-.136,.785)],[(0,1,2,3,4,5)],LINEN)
        mod=apron.modifiers.new('Apron thickness','SOLIDIFY');mod.thickness=.005
        bpy.context.view_layer.objects.active=apron;bpy.ops.object.modifier_apply(modifier=mod.name);bind(apron,'Hips')
        curve('Apron pocket seam',[(-.066,-.144,.95),(-.062,-.15,.866),(.062,-.15,.866),(.066,-.144,.95)],.0015,LEATHER,'Hips')

def boots_and_hands(hero,make_boots=True):
    for side,s in [('L',-1),('R',1)]:
        if make_boots:
            shoe=tube('Shaped boot toe', [((s*.104,y,z),rx,rz) for y,z,rx,rz in [(.073,.075,.04,.047),(.041,.092,.049,.064),(-.01,.085,.050,.057),(-.075,.063,.056,.04),(-.14,.057,.054,.034),(-.188,.055,.032,.028),(-.204,.055,.007,.014)]],LEATHER,24)
            shaft=tube('Fitted boot shaft',[((s*.104,y,z),rx,ry) for y,z,rx,ry in [(.01,.095,.044,.05),(.014,.15,.036,.039),(.016,.24,.039,.043),(.01,.34,.054,.057),(.003,.43,.058,.06)]],LEATHER,24)
            bind(union('Boot '+side,[shoe,shaft],LEATHER,.0045),lambda v,s=side:foot_weights(v,s))
            sole=tube('Layered sole '+side,[((s*.104,y,.027),rx,rz) for y,rx,rz in [(.069,.037,.019),(.035,.05,.019),(-.07,.058,.019),(-.16,.051,.018),(-.194,.027,.015),(-.205,.005,.008)]],SOLE,24)
            bind(sole,lambda v,s=side:foot_weights(v,s))
            for z in [.40,.29]:
                rim=tube('Boot strap '+side,[((s*.104,.01,z-.009),.059 if z>.35 else .046,.063 if z>.35 else .049),((s*.104,.01,z+.009),.059 if z>.35 else .046,.063 if z>.35 else .049)],LEATHER)
                bind(rim,lambda v,s=side:foot_weights(v,s))
                softbox('Boot brass buckle '+side,(s*.143,-.032,z),(.022,.014,.027),GOLD,side+'Shin',.003)
            for j in range(5):
                z=.18+j*.035
                curve('Boot lace '+side,[(s*.087,-.03,z),(s*.122,-.035,z+.02)],.0018,LINEN,side+'Shin')
        palm=tube('Palm',[((s*.338,-.017,1.054),.028,.026),((s*.343,-.02,1.03),.027,.024),((s*.349,-.021,1.017),.027,.021),((s*.352,-.024,.982),.037,.021),((s*.356,-.026,.951),.037,.019),((s*.356,-.026,.938),.031,.015)],SKIN)
        parts=[palm]
        for finger in ['Thumb','Index','Middle','Ring','Pinky']:
            sections=[]
            for i in range(1,4):
                b=RIG.data.bones[side+finger+str(i)]
                for t in [0,.4,.75]:
                    p=b.head_local.lerp(b.tail_local,t);r=(.0090 if finger=='Thumb' else .0075)*(1-(i-1+t)*.12)
                    sections.append((tuple(p),r,r*.88))
            end=RIG.data.bones[side+finger+'3'].tail_local;sections.extend([(tuple(end),.0048,.0043),(tuple(end+Vector((0,0,-.002))),.001,.001)])
            parts.append(tube('Finger '+side+finger,sections,SKIN,12))
        bind(union('Articulated hand '+side,parts,SKIN,.0028),lambda v,s=side:hand_weights(v,s))
        if make_boots:
            cuff=tube('Jacket cuff '+side,[((s*.341,-.018,1.033),.035,.035),((s*.345,-.021,1.006),.034,.034)],INK)
            bind(cuff,side+'Forearm')
            if hero:
                guard=tube('Fingerless glove '+side,[((s*.347,-.021,1.008),.031,.024),((s*.351,-.024,.985),.038,.024),((s*.355,-.026,.96),.037,.024)],LEATHER)
                bind(guard,side+'Hand')
                curve('Glove seam '+side,[(s*.35,.001,1.002),(s*.355,.001,.975),(s*.368,.002,.966)],.0013,GOLD,side+'Hand')

FACE_LEVELS=[(1.588,.015,.064,.051),(1.603,.047,.087,.064),(1.625,.075,.103,.080),(1.66,.103,.112,.098),(1.698,.119,.109,.109),(1.729,.120,.106,.112),(1.768,.114,.099,.108),(1.80,.096,.085,.095),(1.832,.063,.061,.068),(1.85,.016,.02,.023)]
def face_profile(z):
    for a,b in zip(FACE_LEVELS,FACE_LEVELS[1:]):
        if z<=b[0]:
            t=max(0,(z-a[0])/(b[0]-a[0]));return tuple(a[i]*(1-t)+b[i]*t for i in [1,2,3])
    return FACE_LEVELS[-1][1:]
def face_y(x,z):
    rx,front,back=face_profile(z);return -front*max(.01,1-(x/rx)**2)**.27

def face():
    vs=[];fs=[];rows=42;n=64
    for j in range(rows):
        z=1.588+(1.85-1.588)*j/(rows-1);rx,front,back=face_profile(z)
        for i in range(n):
            a=math.tau*i/n;x=math.sin(a)*rx;c=math.cos(a)
            y=-front*c**.54 if c>=0 else back*(-c)**.8
            vs.append((x,y,z))
    for j in range(rows-1):
        for i in range(n):a=j*n+i;b=j*n+(i+1)%n;fs.append((a,b,b+n,a+n))
    fs.extend([tuple(range(n-1,-1,-1)),tuple(range((rows-1)*n,rows*n))])
    bind(mesh('Sculpted anime face',vs,fs,SKIN),lambda v:blend('Head','Jaw',.45*smooth((1.655-v.z)/.05)*smooth((-v.y)/.05)))
    for side,s in [('L',-1),('R',1)]:
        uvball('Ear '+side,(s*.12,.012,1.686),(.021,.013,.034),SKIN,'Head')
        uvball('Ear concha '+side,(s*.129,.001,1.687),(.009,.005,.02),EAR,'Head')
        eye(side,s)
    nose=mesh('Nose bridge',[(0,-.110,1.705),(-.006,-.114,1.681),(-.008,-.116,1.661),(0,-.129,1.665),(.008,-.116,1.661),(.006,-.114,1.681),(0,-.115,1.657)],[(0,1,3),(0,3,5),(1,2,3),(5,3,4),(2,6,3),(3,6,4)],SKIN)
    bind(nose,'Head')
    curve('Upper lip',[(-.019,face_y(-.019,1.637)-.002,1.637),(0,face_y(0,1.635)-.003,1.635),(.019,face_y(.019,1.637)-.002,1.637)],.0015,LIP,'Head')
    curve('Lower lip light',[(-.012,face_y(-.012,1.632)-.002,1.632),(0,face_y(0,1.631)-.003,1.631),(.012,face_y(.012,1.632)-.002,1.632)],.0013,EAR,'Head')

def eye(side,s):
    cx=s*.053;cz=1.711;w=.037
    vs=[(cx,face_y(cx,cz)-.011,cz)];fs=[];n=48;nr=8
    for j in range(1,nr+1):
        r=j/nr
        for i in range(n):
            a=math.tau*i/n;dx=math.cos(a)*w*r;dz=math.sin(a)*(.018 if math.sin(a)>0 else .011)*r+s*dx*.11
            x=cx+dx;z=cz+dz;y=face_y(x,z)-.001-.01*(1-r*r);vs.append((x,y,z))
    for i in range(n):fs.append((0,1+i,1+(i+1)%n))
    for j in range(nr-1):
        for i in range(n):a=1+j*n+i;b=1+j*n+(i+1)%n;fs.append((a,a+n,b+n,b))
    bind(mesh('Almond sclera '+side,vs,fs,WHITE),'Head')
    # Circular painted iris, curved to sit on the sclera, with real UVs.
    cx-=s*.001;vs=[(cx,face_y(cx,cz)-.0125,cz)];uv=[(.5,.5)];fs=[]
    for j in range(1,7):
        r=j/6
        for i in range(n):
            a=math.tau*i/n;dx=math.cos(a)*.018*r;dz=math.sin(a)*.019*r
            ex=dx-s*.001;edge=math.sqrt(max(0,1-(ex/w)**2))
            dz=max(-.011*edge+s*ex*.11+.0003,min(.018*edge+s*ex*.11-.0003,dz))
            radius2=(ex/w)**2+((dz-s*ex*.11)/(.018 if dz-s*ex*.11>0 else .011))**2
            x=cx+dx;z=cz+dz;y=face_y(x,z)-.001-.01*(1-radius2)-.001
            vs.append((x,y,z));uv.append((.5+math.cos(a)*r*.45,.5+math.sin(a)*r*.45))
    for i in range(n):fs.append((0,1+i,1+(i+1)%n))
    for j in range(5):
        for i in range(n):a=1+j*n+i;b=1+j*n+(i+1)%n;fs.append((a,a+n,b+n,b))
    iris=mesh('Painted amber iris '+side,vs,fs,IRIS)
    layer=iris.data.uv_layers.new(name='Iris UV')
    for poly in iris.data.polygons:
        for loop in poly.loop_indices:layer.data[loop].uv=uv[iris.data.loops[loop].vertex_index]
    bind(iris,side+'Eye')
    for upper in [True,False]:
        points=[]
        for i in range(13):
            a=i*math.pi/12;dx=math.cos(a)*w;dz=math.sin(a)*(.018 if upper else -.011)+s*dx*.11
            x=s*.053+dx;z=cz+dz;points.append((x,face_y(x,z)-.003,z))
        curve(('Upper lash ' if upper else 'Lower eyelid ')+side,points,.0027 if upper else .0011,LASH if upper else LIP,'Head')
    curve('Winged outer lashes '+side,[(s*.087,face_y(s*.087,1.718)-.003,1.718),(s*.095,face_y(s*.095,1.720)-.003,1.720),(s*.10,face_y(s*.10,1.725)-.003,1.725)],.0018,LASH,'Head')
    curve('Expressive brow '+side,[(s*.018,face_y(s*.018,1.744)-.002,1.744),(s*.052,face_y(s*.052,1.748)-.002,1.748),(s*.087,face_y(s*.087,1.740)-.002,1.740)],.0026,HAIR,'Head')

def bezier(points,t):
    a,b,c,d=map(Vector,points);return a*(1-t)**3+b*3*(1-t)**2*t+c*3*(1-t)*t*t+d*t**3

def hair_lock(name,points,width,weights,highlight=True):
    sections=[]
    for j in range(20):
        t=j/19;c=bezier(points,t);r=width*(.03+1.4*math.sin(t*math.pi)**.55)*max(.015,(1-t)**.45)
        sections.append((tuple(c),r,r*.19))
    bind(tube(name,sections,HAIR,12),weights)
    if highlight:
        pts=[]
        for j in range(2,16,2):
            t=j/19;c=bezier(points,t);c.y-=width*.23*(1-t);pts.append(tuple(c))
        curve(name+' glint',pts,.0009,HAIRLIGHT,weights)

def hair(hero):
    vs=[];fs=[];n=64;rows=17
    for j in range(rows):
        for i in range(n):
            a=i*math.tau/n;front=(math.cos(a)+1)/2
            end=2.60-front**3*1.58;phi=.035+(end-.035)*j/(rows-1)
            vs.append((math.sin(a)*math.sin(phi)*.132,-math.cos(a)*math.sin(phi)*.118+.01,1.715+math.cos(phi)*.153))
    for j in range(rows-1):
        for i in range(n):a=j*n+i;b=j*n+(i+1)%n;fs.append((a,b,b+n,a+n))
    bind(mesh('Sculpted layered hair cap',vs,[tuple(reversed(f)) for f in fs],HAIR),'Head')
    for i in range(9):
        x=-.112+i*.027;endx=x+.020
        endz=1.737+(.014 if i%3 else -.008)
        if abs(endx)>.094:endz-=.058
        hair_lock('Swept fringe %02d'%i,[(x*.62-.018,-.024,1.867-abs(x)*.22),(x-.025,-.108,1.833),(endx,-.129,1.777),(endx,-.12,endz)],.023+(i%3)*.002,'Head')
    for s in [-1,1]:
        for i in range(3):
            hair_lock('Face framing lock',[(s*(.104+i*.009),-.006,1.819),(s*(.146+i*.008),-.05,1.744),(s*(.118+i*.013),-.044,1.627),(s*(.117+i*.014),-.027,1.535+i*.018)],.021-i*.003,'Head')
    if hero:
        for i in range(7):
            offset=(i-3)*.012
            hair_lock('Layered ponytail',[(offset,.117,1.769),(offset*1.6,.274,1.733),(offset*1.3,.28,1.432),(offset*1.3+.035,.21,1.34+abs(offset))],.028,
                lambda v:blend('HairTip','HairBase',(v.z-1.40)/.24))
        softbox('Hair tie',(0,.13,1.765),(.098,.04,.033),RED,'Head',.01)
        for s in [-1,1]:
            hair_lock('Loose nape strand',[(s*.085,.08,1.71),(s*.10,.10,1.62),(s*.07,.13,1.54),(s*.067,.10,1.50)],.015,'Head',False)
    else:
        for i in range(7):
            a=math.pi*.5+i*math.pi/6
            hair_lock('Soft bob ends',[(math.sin(a)*.11,-math.cos(a)*.1,1.79),(math.sin(a)*.137,-math.cos(a)*.12,1.69),(math.sin(a)*.132,-math.cos(a)*.12,1.61),(math.sin(a)*.11,-math.cos(a)*.105,1.58)],.023,'Head',False)
        # Curved woven brim; the head and ears remain visible underneath.
        hat=tube('Woven country hat',[((0,.012,1.824),.205,.174),((0,.012,1.839),.203,.173),((0,.012,1.85),.137,.116),((0,.012,1.904),.121,.10),((0,.012,1.928),.048,.044)],LINEN,48);bind(hat,'Head')
        band=tube('Hat ribbon',[((0,.012,1.857),.139,.118),((0,.012,1.875),.134,.113)],LEATHER,48);bind(band,'Head')

def scarf_and_weapon():
    collar=tube('Soft scarf collar',[((0,0,z),rx,ry) for z,rx,ry in [(1.482,.08,.072),(1.505,.071,.063),(1.526,.063,.058),(1.543,.058,.053)]],RED,40);bind(collar,'Neck')
    for j in range(3):
        curve('Woven scarf fold',[(-.052,-.04,1.493+j*.012),(0,-.072+j*.004,1.495+j*.012),(.052,-.04,1.5+j*.012)],.0015,LEATHER,'Neck')
    vs=[];fs=[]
    for j in range(15):
        t=j/14
        for i in range(7):
            u=i/6;vs.append((.055+(u-.5)*.082+t*.022,.072+t*.1+math.sin(t*7+u*2)*.006,1.49-t*.32+math.sin(u*math.pi)*.008))
    for j in range(14):
        for i in range(6):a=j*7+i;fs.append((a,a+1,a+8,a+7))
    o=mesh('Flowing scarf tail',vs,fs,RED);mod=o.modifiers.new('Woven thickness','SOLIDIFY');mod.thickness=.003
    bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=mod.name);bind(o,'Scarf')
    softbox('Offline wrist terminal',(-.337,-.05,1.075),(.067,.037,.068),GOLD,'LForearm',.012)
    softbox('Glass terminal screen',(-.337,-.072,1.078),(.049,.008,.043),SIGNAL,'LForearm',.007)
    grip=tube('Wrapped sword grip',[((.355,-.026,z),.017,.019) for z in [.966,.856]],LEATHER,16);bind(grip,'RHand')
    for j in range(6):
        curve('Sword handle binding',[(.34,-.04,.865+j*.016),(.366,-.044,.876+j*.016)],.0025,LINEN,'RHand')
    softbox('Curved crossguard',(.355,-.026,.841),(.154,.065,.023),GOLD,'RHand',.009)
    blade=mesh('Forged single edge sword',[(.329,-.029,.823),(.378,-.029,.823),(.382,-.029,.187),(.365,-.029,.12),(.342,-.029,.19),(.352,-.041,.81),(.361,-.041,.20)],[(0,1,5),(1,2,6,5),(2,3,6),(3,4,6),(4,0,5,6)],STEEL)
    mod=blade.modifiers.new('Steel thickness','SOLIDIFY');mod.thickness=.009
    bpy.context.view_layer.objects.active=blade;bpy.ops.object.modifier_apply(modifier=mod.name);bind(blade,'RHand')
    curve('Jade blade edge',[(.379,-.033,.817),(.383,-.033,.20),(.365,-.033,.12)],.0018,SIGNAL,'RHand')

CLIPS={'idle':90,'walk':32,'run':24,'attack1':18,'attack2':20,'attack3':25,'guard':60,'dodge':20,'jump':32,'skill':38,'stagger':22,'death':45}
def animate(hero):
    actions=[]
    for name,end in CLIPS.items():
        RIG.animation_data_create();action=bpy.data.actions.new(name);RIG.animation_data.action=action;actions.append(action)
        for frame in sorted(set([1,end]+list(range(3,end,3)))):
            t=(frame-1)/(end-1);wave=math.sin(t*math.tau);peak=math.sin(t*math.pi);p=RIG.pose.bones
            for bone in p:bone.rotation_euler=(0,0,0);bone.location=(0,0,0)
            # Relaxed clavicles, soft elbow bend and individually articulated fingers.
            for side,s in [('L',-1),('R',1)]:
                p[side+'Arm'].rotation_euler.z=s*.08
                p[side+'Forearm'].rotation_euler.x=-.10
                for finger in ['Index','Middle','Ring','Pinky']:
                    for j in range(1,4):p[side+finger+str(j)].rotation_euler.x=-(.85 if side=='R' and hero else .12+j*.05)
                for j in range(1,4):p[side+'Thumb'+str(j)].rotation_euler.x=-.25
            if name=='idle':
                p['Spine'].rotation_euler.x=wave*.008;p['Chest'].rotation_euler.x=wave*.012
                p['Head'].rotation_euler.y=wave*.02;p['Neck'].rotation_euler.z=wave*.014
                p['HairTip'].rotation_euler.x=wave*.025;p['Scarf'].rotation_euler.x=wave*.025
                p['Hips'].location.y=(1+wave)*.0015
            elif name in ('walk','run'):
                amp=.40 if name=='walk' else .70
                for side,s in [('L',1),('R',-1)]:
                    phase=wave*s;p[side+'Thigh'].rotation_euler.x=phase*amp
                    p[side+'Shin'].rotation_euler.x=max(0,-phase)*(.67 if name=='walk' else 1.05)
                    p[side+'Foot'].rotation_euler.x=-max(0,-phase)*.35+max(0,phase)*.12
                    p[side+'Toe'].rotation_euler.x=max(0,phase)*.12
                    p[side+'Arm'].rotation_euler.x=-phase*amp*.6
                    p[side+'Forearm'].rotation_euler.x=-.18-max(0,phase)*(.14 if name=='walk' else .55)
                    p[side+'Clavicle'].rotation_euler.y=phase*.025
                p['Hips'].location.y=abs(wave)*(.015 if name=='walk' else .025)
                p['Hips'].rotation_euler.y=wave*.04;p['Spine'].rotation_euler.y=-wave*.025
                p['Chest'].rotation_euler.x=.03 if name=='walk' else .10
                p['Head'].rotation_euler.x=-.025 if name=='walk' else -.06
                p['HairBase'].rotation_euler.x=-.05+wave*.08;p['HairTip'].rotation_euler.z=wave*.06
                p['Scarf'].rotation_euler.x=-.12+wave*.06
            elif name.startswith('attack') or name=='skill':
                twist=1 if name=='attack1' else -1 if name=='attack2' else .45
                sweep=math.sin(t*math.tau-.4)*peak
                p['Hips'].rotation_euler.y=sweep*.18*twist;p['Spine'].rotation_euler.y=sweep*.23*twist
                p['Chest'].rotation_euler.y=sweep*.30*twist
                p['RClavicle'].rotation_euler.y=-peak*.15
                p['RArm'].rotation_euler.x=-peak*1.6;p['RArm'].rotation_euler.z=sweep*.85*twist
                p['RForearm'].rotation_euler.x=-peak*.65;p['RHand'].rotation_euler.x=peak*.16
                p['LArm'].rotation_euler.x=-peak*.70;p['LForearm'].rotation_euler.x=-peak*.40
                p['LThigh'].rotation_euler.x=-peak*.18;p['LShin'].rotation_euler.x=peak*.20
                p['RThigh'].rotation_euler.x=peak*.12;p['RShin'].rotation_euler.x=peak*.20
                p['Hips'].location.y=-peak*.025;p['HairTip'].rotation_euler.z=-sweep*.16;p['Scarf'].rotation_euler.z=-sweep*.10
            elif name=='guard':
                p['RArm'].rotation_euler.x=-1.05;p['RArm'].rotation_euler.z=-.30
                p['RForearm'].rotation_euler.x=-1.05;p['RHand'].rotation_euler.z=.12
                p['LArm'].rotation_euler.x=-.95;p['LForearm'].rotation_euler.x=-1.1
                p['LThigh'].rotation_euler.x=-.12;p['LShin'].rotation_euler.x=.16
                p['Chest'].rotation_euler.x=.08;p['Hips'].location.y=-.012
            elif name=='dodge':
                p['Root'].rotation_euler.x=-t*math.tau;p['Root'].location.y=peak*.45
                p['Spine'].rotation_euler.x=peak*.5;p['Head'].rotation_euler.x=peak*.18
                for side in ['L','R']:
                    p[side+'Thigh'].rotation_euler.x=-peak*.8;p[side+'Shin'].rotation_euler.x=peak*1.25
                    p[side+'Arm'].rotation_euler.x=-peak*.55;p[side+'Forearm'].rotation_euler.x=-peak*.8
            elif name=='jump':
                p['LThigh'].rotation_euler.x=-peak*.52;p['RThigh'].rotation_euler.x=peak*.3
                p['RShin'].rotation_euler.x=peak*.85;p['LShin'].rotation_euler.x=peak*.3
                p['LArm'].rotation_euler.x=-peak*.6;p['RArm'].rotation_euler.x=peak*.2
                p['HairBase'].rotation_euler.x=-peak*.13;p['Scarf'].rotation_euler.x=-peak*.18
            elif name=='stagger':
                p['Hips'].rotation_euler.x=-peak*.06;p['Spine'].rotation_euler.x=-peak*.15
                p['Chest'].rotation_euler.x=-peak*.18;p['Head'].rotation_euler.x=-peak*.12
            elif name=='death':
                down=smooth(t*2);p['Root'].rotation_euler.x=-down*1.48;p['Root'].location.y=down*.29
                p['Head'].rotation_euler.z=down*.20;p['RThigh'].rotation_euler.x=-down*.18;p['RShin'].rotation_euler.x=down*.3
            if hero:
                poses={b.name:tuple(b.rotation_euler) for b in p}
                for b in p:
                    if b.name.startswith(('L','R')):
                        other=('R' if b.name[0]=='L' else 'L')+b.name[1:]
                        if other in poses:
                            x,y,z=poses[other];b.rotation_euler=(x,-y,-z)
            if hero and name=='idle':
                p['LArm'].rotation_euler.z=-.20
                p['LForearm'].rotation_euler.x=-.15
                p['RArm'].rotation_euler.z=.10
                p['RForearm'].rotation_euler.x=-.40
            if hero:
                # Mio's authored rest fingers already enclose the sword handle.
                for finger in ['Thumb','Index','Middle','Ring','Pinky']:
                    for j in range(1,4):p['L'+finger+str(j)].rotation_euler=(0,0,0)
                if name.startswith('attack') or name=='skill':
                    p['LClavicle'].rotation_euler.x=-.20*peak
                    # The loose scarf ends move behind the raised sword arm; the knot stays fixed.
                    p['Scarf'].location=p['Scarf'].bone.matrix_local.to_3x3().inverted()@Vector((0,.16*peak,.02*peak))
                if name=='guard':
                    p['LClavicle'].rotation_euler.x=-.10
                    p['RArm'].rotation_euler.x=-.45;p['RForearm'].rotation_euler.x=-.80
            for bone in p:
                bone.keyframe_insert(data_path='rotation_euler',frame=frame)
                if bone.name in ['Root','Hips','Scarf']:bone.keyframe_insert(data_path='location',frame=frame)
        track=RIG.animation_data.nla_tracks.new();track.name=name;track.strips.new(name,1,action);track.mute=True
    RIG.animation_data.action=None
    for bone in RIG.pose.bones:bone.rotation_euler=(0,0,0);bone.location=(0,0,0)
    return actions

def consolidate(scene):
    """One skinned mesh per material keeps six humans affordable in the village."""
    batches={}
    for o in scene.objects:
        if o.type=='MESH':batches.setdefault(o.data.materials[0],[]).append(o)
    for material,objects in batches.items():
        bpy.ops.object.select_all(action='DESELECT')
        for o in objects:o.select_set(True)
        bpy.context.view_layer.objects.active=objects[0]
        if len(objects)>1:bpy.ops.object.join()
        o=bpy.context.object
        o.name='Travel coat' if material==TEAL or 'resident work jacket' in material.name else material.name.replace(PREFIX,'')

def validate(scene,actions):
    meshes=[o for o in scene.objects if o.type=='MESH'];mixed=0;vertices=0;triangles=0
    for o in meshes:
        o.data.calc_loop_triangles();triangles+=len(o.data.loop_triangles)
        if any('Mio painted' in m.name for m in o.data.materials):
            assert len(o.data.uv_layers)==1 and o.data.uv_layers[0].name=='UVMap', (o.name, list(o.data.uv_layers.keys()))
            # The gathered hair unwrap crosses the repeating texture's U seam.
            u_max=2 if any('Mio painted hair' in m.name for m in o.data.materials) else 1
            assert all(math.isfinite(u) and math.isfinite(v) and 0<=u<=u_max and 0<=v<=1
                       for u,v in (uv.uv for uv in o.data.uv_layers[0].data)), o.name
        for v in o.data.vertices:
            vertices+=1;weights=[g.weight for g in v.groups]
            assert weights and abs(sum(weights)-1)<.001,(o.name,v.index,weights)
            if len([w for w in weights if w>.02])>1:mixed+=1
    assert len(RIG.data.bones)>=58
    assert mixed>2000, mixed
    assert len(actions)==12
    assert triangles<160000,triangles
    return {'bones':len(RIG.data.bones),'meshes':len(meshes),'vertices':vertices,'triangles':triangles,'blended_vertices':mixed,'clips':list(CLIPS)}

def build(name,hero,export=True):
    scene_name=PREFIX+name
    if scene_name in bpy.data.scenes:
        old=bpy.data.scenes[scene_name]
        for o in list(old.objects):bpy.data.objects.remove(o,do_unlink=True)
        bpy.data.scenes.remove(old)
    scene=bpy.data.scenes.new(scene_name);bpy.context.window.scene=scene;scene.render.fps=30
    human_rig(name)
    if hero:
        import runpy
        runpy.run_path(ROOT+'/tools/mio_illustrated.py',init_globals={'B':globals()})['build']()
    else:
        garments(False);boots_and_hands(False);face();hair(False)
    consolidate(scene)
    actions=animate(hero);audit=validate(scene,actions)
    # NLA track export includes exactly this rig's twelve clips.
    bpy.ops.object.select_all(action='SELECT')
    bpy.context.view_layer.objects.active=RIG
    if export:
        bpy.ops.export_scene.gltf(filepath=OUT+'/'+name+'.glb',export_format='GLB',use_selection=True,use_active_scene=True,
            export_animations=True,export_animation_mode='NLA_TRACKS',export_apply=False)
    RIG.animation_data.action=None
    for bone in RIG.pose.bones:bone.rotation_euler=(0,0,0);bone.location=(0,0,0)
    bpy.context.view_layer.update()
    print(name,json.dumps(audit));return audit

def main():
    global IRIS
    IRIS=mat('painted amber eyes',(1,1,1),.43)
    image=bpy.data.images.load(ROOT+'/assets/art/amber-iris.png',check_existing=True)
    node=IRIS.node_tree.nodes.new('ShaderNodeTexImage');node.image=image
    IRIS.node_tree.links.new(node.outputs['Color'],IRIS.node_tree.nodes.get('Principled BSDF').inputs['Base Color'])
    results={name:build(name,hero) for name,hero in [('mio',True),('villager',False)]}
    bpy.ops.file.pack_all()
    bpy.ops.wm.save_as_mainfile(filepath=ROOT+'/assets/source/characters.blend')
    with open(ROOT+'/artifacts/human-model-audit.json','w') as f:json.dump(results,f,indent=2)
    print('HUMAN_REFINEMENT_COMPLETE')

if __name__=='__main__':main()
