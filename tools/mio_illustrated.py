"""Mio's illustrated surfaces, loaded by refine_humans.py with its shared rig helpers.

The original key art is the design authority. Facial apertures, eyelids and
garment construction carry the form; imagegen supplies material surface detail.
"""
import bpy, math
from mathutils import Vector

for _name in ('ROOT','RIG','mesh','tube','bind','union','curve','uvball','softbox','mat','smooth','blend','axial','upper_weights','leg_weights','hand_weights','foot_weights','bezier','GOLD','LEATHER','SOLE','STEEL','SIGNAL','boots_and_hands'):
    globals()[_name]=B[_name]

def lerp_profile(z,rows):
    if z<=rows[0][0]:return rows[0][1:]
    for a,b in zip(rows,rows[1:]):
        if z<=b[0]:
            t=(z-a[0])/(b[0]-a[0]);return tuple(x+(y-x)*t for x,y in zip(a[1:],b[1:]))
    return rows[-1][1:]

def painted(name,path,emission=.18):
    m=mat(name,(1,1,1),1)
    p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Specular IOR Level'].default_value=.05
    tex=m.node_tree.nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(ROOT+path,check_existing=True)
    m.node_tree.links.new(tex.outputs['Color'],p.inputs['Base Color'])
    m.node_tree.links.new(tex.outputs['Color'],p.inputs['Emission Color']);p.inputs['Emission Strength'].default_value=emission
    return m

def uv(o,mapper,material):
    o.data.materials.clear();o.data.materials.append(material)
    for old in list(o.data.uv_layers):o.data.uv_layers.remove(old)
    layer=o.data.uv_layers.new(name='UVMap')
    for p in o.data.polygons:
        back=p.normal.y>.05
        for li in p.loop_indices:
            v=o.matrix_world@o.data.vertices[o.data.loops[li].vertex_index].co
            mapped=mapper(v,back)
            if o.get('anatomical_face') and (p.center.y>-.030 or p.center.z<CHIN-.005):
                mapped=(.03,.94)
            layer.data[li].uv=mapped
    return o

def fabric_uv(o,cell):
    """Use whole-garment projections so tiny remeshed islands cannot magnify the weave."""
    o.data.materials.clear();o.data.materials.append(COSTUME)
    for old in list(o.data.uv_layers):o.data.uv_layers.remove(old)
    layer=o.data.uv_layers.new(name='UVMap')
    ox=.5*(cell%2)+.025;oy=.5*(1-cell//2)+.025
    low=[min(v.co[i] for v in o.data.vertices) for i in range(3)]
    span=[max(v.co[i] for v in o.data.vertices)-low[i] for i in range(3)]
    for p in o.data.polygons:
        axes=(0,2) if abs(p.normal.y)>=abs(p.normal.x) else (1,2)
        if abs(p.normal.z)>.8:axes=(0,1)
        for li in p.loop_indices:
            v=o.data.vertices[o.data.loops[li].vertex_index].co
            u,w=[(v[i]-low[i])/max(.001,span[i]) for i in axes]
            layer.data[li].uv=(ox+u*.45,oy+w*.45)
    return o

def pixels(x,y):return (max(1,min(1253,x))/1254,1-max(1,min(1253,y))/1254)

def shell(name,rows,material,opening=.45,back_split=False):
    """An open-front garment with a curved cross-section and actual thickness."""
    vs=[];fs=[];n=48
    for j,(z,rx,ry) in enumerate(rows):
        for i in range(n):
            a=opening+(math.tau-2*opening)*i/(n-1)
            zz=z
            if j==0:zz-=.024*abs(math.sin(a))
            if back_split and j==0:zz+=.26*max(0,1-abs(a-math.pi)/(math.pi/2))+.04*max(0,math.cos(a))+.016*math.sin(a)
            vs.append((math.sin(a)*rx,-math.cos(a)*ry,zz))
    for j in range(len(rows)-1):
        for i in range(n-1):
            if back_split and i==n//2-1 and j<len(rows)-2:continue
            a=j*n+i;fs.append((a,a+1,a+n+1,a+n))
    o=mesh(name,vs,fs,material)
    return o

def thickness(o,value=.003):
    bpy.context.view_layer.objects.active=o
    mod=o.modifiers.new('Cloth edge thickness','SOLIDIFY');mod.thickness=value
    bpy.ops.object.modifier_apply(modifier=mod.name)
    return o

def reduce_detail(o,ratio):
    bpy.context.view_layer.objects.active=o
    mod=o.modifiers.new('Small detail budget','DECIMATE');mod.ratio=ratio
    while o.modifiers[0]!=mod:bpy.ops.object.modifier_move_up(modifier=mod.name)
    bpy.ops.object.modifier_apply(modifier=mod.name)
    return o

def assert_connected(o):
    adjacency=[[] for _ in o.data.vertices]
    for e in o.data.edges:
        a,b=e.vertices;adjacency[a].append(b);adjacency[b].append(a)
    seen={0};pending=[0]
    while pending:
        for i in adjacency[pending.pop()]:
            if i not in seen:seen.add(i);pending.append(i)
    assert len(seen)==len(adjacency), (o.name, 'detached surface', len(adjacency)-len(seen))

# Front-face control cage: eye loops are part of the facial surface.
HEAD_TOP=1.782
CHIN=1.552
EYE_Z=1.650
EYE_X=.044
EYE_HALF_WIDTH=.0255
EYE_TILT=.0025
EYE_UPPER_HEIGHT=.0134
MOUTH_Z=1.5875
FACE_LEVELS=[(CHIN,.012,.068,.004),(1.558,.021,.075,.026),
    (1.574,.040,.079,.048),(1.592,.057,.079,.050),(1.607,.073,.080,.060),
    (1.628,.085,.081,.074),(1.650,.0915,.079,.082),(1.680,.090,.083,.089),
    (1.710,.095,.084,.094),(1.740,.083,.073,.083),(1.763,.059,.049,.060),
    (1.777,.029,.024,.030),(HEAD_TOP,.004,.004,.005)]
# Central nose, lip and chin depth is independent of the lateral jaw contour.
NOSE_PROFILE=[(CHIN,.068),(1.555,.072),(1.565,.078),(1.572,.080),
    (1.578,.080),(1.582,.0825),(1.584,.084),(MOUTH_Z,.084),(1.591,.087),(1.596,.087),
    (1.603,.088),(1.609,.089),(1.615,.098),(1.619,.101),(1.625,.096),(1.638,.080),
    (1.654,.081),(1.680,.083),(1.740,.073),(HEAD_TOP,.004)]

def jaw_center(z):
    return curved_profile(z,[(CHIN,-.061),(1.558,-.055),(1.574,-.052),
        (1.592,-.035),(1.607,-.016),(1.628,.001),(1.650,.007),(1.680,.006),
        (1.710,0),(HEAD_TOP,0)])[0]

def face_y(x,z,section=None):
    rx,front,_=curved_profile(z,FACE_LEVELS)
    # The upper cheek turns into a narrower jaw, with a separate lip/chin profile.
    center=jaw_center(z)
    if section is not None:rx,center=section
    exponent=.40+.08*math.exp(-((z-1.603)/.030)**2)
    if section is not None:exponent+=.65*smooth((1.606-z)/(1.606-CHIN))
    y=center-(front+center)*max(0,1-(x/rx)**2)**exponent
    width=lerp_profile(z,[(CHIN,.021),(1.572,.025),(1.584,.020),
        (MOUTH_Z,.020),(1.591,.019),(1.608,.016),(1.630,.013),(HEAD_TOP,.028)])[0]
    y-=(curved_profile(z,NOSE_PROFILE)[0]-front)*max(0,1-(x/width)**2)**1.5
    g=lambda cx,cz,sx,sz:math.exp(-((x-cx)/sx)**2-((z-cz)/sz)**2)
    # Cheek support continues beside the mouth instead of pinching inward.
    y-=.007*(g(-.052,1.627,.036,.024)+g(.052,1.627,.036,.024))*max(0,1-(x/rx)**4)
    y-=.005*(g(-.043,1.594,.028,.024)+g(.043,1.594,.028,.024))*max(0,1-(x/rx)**4)
    # Broad lower-cheek fullness fades out before reaching the eye opening.
    y-=.003*(g(-.047,1.611,.036,.026)+g(.047,1.611,.036,.026))*max(0,1-(x/rx)**4)*smooth((1.642-z)/.017)
    y-=.003*(g(-.044,1.679,.027,.015)+g(.044,1.679,.027,.015))
    # Small nasal wings and lip cushions keep these forms readable from below.
    y-=.0032*(g(-.0065,1.609,.0045,.004)+g(.0065,1.609,.0045,.004))
    y-=.0009*(g(-.0045,MOUTH_Z+.0023,.0055,.002)+g(.0045,MOUTH_Z+.0023,.0055,.002))
    y-=.0010*g(0,MOUTH_Z-.002,.012,.0024)
    return y

def eye_edge(a,side):
    # Separate authored arcs meet at tapered canthi, rather than an oval socket.
    upper=math.sin(a)>0
    p=bezier([(-EYE_HALF_WIDTH,0,-EYE_TILT),
        (-EYE_HALF_WIDTH*.55,0,EYE_UPPER_HEIGHT*.95 if upper else -.0115),
        (EYE_HALF_WIDTH*.45,0,EYE_UPPER_HEIGHT*1.18 if upper else -.0115),
        (EYE_HALF_WIDTH,0,EYE_TILT)],(math.cos(a)+1)/2)
    return Vector((side*(EYE_X+p.x),eye_surface(p.x,p.z)-.0008,EYE_Z+p.z))

def eye_surface(dx,dz,side=1):
    # Seat the orbit inside the continuous forehead/cheek envelope. An independent
    # forward-facing globe pushes the far eye through the oblique face silhouette.
    envelope=face_y(side*EYE_X+dx,EYE_Z+dz)
    return envelope+.004-.0018*math.sqrt(max(.03,1-(dx/.035)**2-(dz/.027)**2))

def finish_cage(o,levels=2):
    import bmesh
    bm=bmesh.new();bm.from_mesh(o.data)
    bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=.00001)
    bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(o.data);bm.free()
    bpy.context.view_layer.objects.active=o
    if o.name in ['Mio continuous facial control surface','Open tailored traveling coat']:
        scene=bpy.data.scenes.get('MioControlCages') or bpy.data.scenes.new('MioControlCages')
        old=scene.objects.get(o.name+' control cage')
        if old:bpy.data.objects.remove(old,do_unlink=True)
        control=o.copy();control.data=o.data.copy();control.name=o.name+' control cage'
        scene.collection.objects.link(control)
        mod=control.modifiers.new('Editable subdivision preview','SUBSURF');mod.levels=2
    if levels:
        sub=o.modifiers.new('Authored control surface','SUBSURF');sub.levels=levels
        bpy.ops.object.modifier_apply(modifier=sub.name)
    return o

def facial_curve(name,points,radius,material):
    o=curve(name,points,radius,material,'Head')
    if material==FACE:uv(o,lambda v,b:(.03,.94),FACE)
    return reduce_detail(o,.3)

def join_neck(head,neck):
    """Blend the jaw into the neck while retaining the authored eye apertures."""
    import bmesh
    from mathutils.kdtree import KDTree
    cap=FACE.copy();cap.name='Temporary socket cap'
    neck_tag=FACE.copy();neck_tag.name='Temporary neck surface'
    head.data.materials.append(cap)
    neck.data.materials.clear();neck.data.materials.append(neck_tag)
    bm=bmesh.new();bm.from_mesh(head.data)
    caps=bmesh.ops.holes_fill(bm,edges=[e for e in bm.edges if e.is_boundary],sides=0)['faces']
    assert len(caps)==2, 'Only the two eye apertures should be open'
    for f in caps:f.material_index=1
    bm.to_mesh(head.data);bm.free()
    bpy.context.view_layer.objects.active=head
    mod=head.modifiers.new('Continuous jaw and neck','BOOLEAN');mod.operation='UNION';mod.solver='EXACT';mod.material_mode='TRANSFER';mod.object=neck
    bpy.ops.object.modifier_apply(modifier=mod.name)
    materials=list(head.data.materials);neck_index=materials.index(neck_tag);cap_index=materials.index(cap)
    bm=bmesh.new();bm.from_mesh(head.data)
    seam={v for e in bm.edges if len(e.link_faces)==2 and {f.material_index for f in e.link_faces}=={0,neck_index} for v in e.verts}
    assert seam, 'The head and neck must intersect'
    tree=KDTree(len(seam))
    for i,v in enumerate(seam):tree.insert(v.co,i)
    tree.balance()
    weights={v:(1-tree.find(v.co)[2]/.024)**2 for v in bm.verts if tree.find(v.co)[2]<.024}
    for _ in range(18):
        updates={v:v.co.lerp(sum((e.other_vert(v).co for e in v.link_edges),Vector())/len(v.link_edges),.40*w) for v,w in weights.items() if v.link_edges}
        for v,co in updates.items():v.co=co
    bmesh.ops.delete(bm,geom=[f for f in bm.faces if f.material_index==cap_index],context='FACES_ONLY')
    for f in bm.faces:f.material_index=0;f.smooth=True
    bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(head.data);bm.free()
    head.data.materials.clear();head.data.materials.append(FACE)
    bpy.data.objects.remove(neck,do_unlink=True)
    for material in [cap,neck_tag]:
        if material.users==0:bpy.data.materials.remove(material)
    assert_connected(head)
    finished_eye_loops(head)
    return head

def finished_eye_loops(head):
    """Use the actual open edges after subdivision and globe clearance fitting."""
    import bmesh
    bm=bmesh.new();bm.from_mesh(head.data)
    bm.verts.ensure_lookup_table()
    edges={tuple(sorted((e.verts[0].index,e.verts[1].index))) for e in bm.edges if e.is_boundary}
    loops={}
    for side in [-1,1]:
        ids={i for edge in edges for i in edge if side*bm.verts[i].co.x>0}
        assert len(ids)>40, 'Missing finished eye boundary'
        def angle(i):
            p=bm.verts[i].co;dx=side*p.x-EYE_X
            return math.atan2((p.z-EYE_Z-EYE_TILT*dx/EYE_HALF_WIDTH)/EYE_UPPER_HEIGHT,dx/EYE_HALF_WIDTH)
        ordered=sorted(ids,key=angle)
        assert all(tuple(sorted((a,b))) in edges for a,b in zip(ordered,ordered[1:]+ordered[:1])), 'Eye boundary must be one ordered loop'
        loops[side]=[(angle(i),bm.verts[i].co.copy()) for i in ordered]
    assert sum(len(loop) for loop in loops.values())==len(edges), 'Unexpected face boundary'
    bm.free();return loops

def facial_features(head):
    loops=finished_eye_loops(head)
    for side,label in [(-1,'L'),(1,'R')]:
        boundary=[p for _,p in loops[side]]
        def fitted_eye_y(x,z):
            # Approach the actual rim continuously from inside the aperture.
            # A binary skin-hit offset makes triangles jump across the lower lid.
            nearest=None;distance=float('inf')
            for a,b in zip(boundary,boundary[1:]+boundary[:1]):
                dx=b.x-a.x;dz=b.z-a.z
                t=max(0,min(1,((x-a.x)*dx+(z-a.z)*dz)/(dx*dx+dz*dz)))
                q=a.lerp(b,t);d=(x-q.x)**2+(z-q.z)**2
                if d<distance:nearest=q;distance=d
            y=eye_surface(x-side*EYE_X,z-EYE_Z,side)
            rim=eye_surface(nearest.x-side*EYE_X,nearest.z-EYE_Z,side)
            y+=(nearest.y+.0012-rim)*math.exp(-distance/.006**2)
            hit,surface,_,_=head.ray_cast(Vector((x,-.3,z)),Vector((0,1,0)))
            if hit and surface.y<0:
                y=max(y,surface.y+.0012*smooth(math.sqrt(distance)/.003))
            return y
        # Full eyeball volume behind a genuine opening; the iris follows its curvature.
        white=uvball('Mio eyeball '+label,(side*EYE_X,-.064,EYE_Z),(.035,.010,.027),EYEWHITE)
        for v in white.data.vertices:v.co.y+=side*.26*v.co.x
        # Fit the hidden globe to the actual socket; do not push the cheek out to it.
        for v in white.data.vertices:
            p=v.co+white.location
            v.co.y=max(v.co.y,fitted_eye_y(p.x,p.z)-white.location.y)
        bind(white,label+'Eye')
        # Tuck the upper iris under the lid, keeping its bright lower arc visible.
        vs=[(side*EYE_X,fitted_eye_y(side*EYE_X,EYE_Z)-.0004,EYE_Z)];fs=[];coords=[(.5,.51)];n=48
        for j in range(1,7):
            r=j/6
            for i in range(n):
                a=i*math.tau/n;dx=.0155*r*math.cos(a);dz=.0145*r*math.sin(a)
                x=side*EYE_X+dx;z=EYE_Z+dz;y=fitted_eye_y(x,z)-.0004
                vs.append((x,y,z))
                # Stay inside the painted ring; its outer white margin must not enter the eye.
                coords.append((.5+.43*r*math.cos(a),.51+.43*r*math.sin(a)))
        for i in range(n):fs.append((0,1+i,1+(i+1)%n))
        for j in range(5):
            for i in range(n):
                a=1+j*n+i;b=1+j*n+(i+1)%n;fs.append((a,a+n,b+n,b))
        eye=mesh('Mio amber iris '+label,vs,fs,EYES)
        assert all(p.normal.y<0 for p in eye.data.polygons), 'Iris must face out of the socket'
        layer=eye.data.uv_layers.new(name='UVMap')
        for p in eye.data.polygons:
            for li in p.loop_indices:layer.data[li].uv=coords[eye.data.loops[li].vertex_index]
        bind(eye,label+'Eye')
        # Root every lash strip in the finished opening and follow the adjacent skin.
        vs=[];fs=[]
        widths=[]
        for a,p in loops[side]:
            sn=math.sin(a);cs=math.cos(a)
            w=((.0008+.0034*smooth((cs+.50)/1.35))*sn**.65+.0005*smooth((cs-.75)/.25)) if sn>0 else .0004*smooth((cs-.10)/.90)
            if w<.00002:w=0
            widths.append(w)
            outward=Vector((side*cs,0,sn)).normalized()
            for t in [0,.25,.5,.75,1]:
                q=p+outward*w*t
                if t and w:
                    hit,surface,_,_=head.ray_cast(Vector((q.x,-.3,q.z)),Vector((0,1,0)))
                    assert hit and surface.y<0, 'Lash must follow skin outside the opening'
                    q.y=surface.y
                # Keep the fine inner taper against the rim in oblique views.
                q.y-=.00012+.00023*smooth((cs+.2)/1.2)
                vs.append(tuple(q))
        n=len(widths)
        for i in range(n):
            j=(i+1)%n
            if max(widths[i],widths[j])<1e-7:continue
            for k in range(4):fs.append((i*5+k,j*5+k,j*5+k+1,i*5+k+1))
        used=sorted({i for f in fs for i in f});remap={old:new for new,old in enumerate(used)}
        vs=[vs[i] for i in used];fs=[tuple(remap[i] for i in f) for f in fs]
        lash=mesh('Mio fitted eyelash '+label,vs,fs,LASH);finish_cage(lash,0);bind(lash,'Head')
        lash['fitted_lid_boundary']=True
        outer=max((p for a,p in loops[side]),key=lambda p:abs(p.x))
        vs=[]
        for dx,dz in [(.0003,.0015),(.0031,.0018),(.0003,-.0002)]:
            x=outer.x+side*dx;z=outer.z+dz
            hit,p,_,_=head.ray_cast(Vector((x,-.3,z)),Vector((0,1,0)))
            assert hit and p.y<0, 'Lash tip must follow the outer canthus skin'
            vs.append((x,p.y-.0002,z))
        wing=mesh('Mio tapered outer lash '+label,vs,[(0,1,2)],LASH)
        bpy.context.view_layer.objects.active=wing
        sub=wing.modifiers.new('Fit curved canthus','SUBSURF');sub.subdivision_type='SIMPLE';sub.levels=2
        bpy.ops.object.modifier_apply(modifier=sub.name)
        for v in wing.data.vertices:
            hit,p,_,_=head.ray_cast(Vector((v.co.x,-.3,v.co.z)),Vector((0,1,0)))
            assert hit and p.y<0
            v.co.y=p.y-.0002
        bind(wing,'Head')
        vs=[];fs=[]
        for i in range(17):
            t=i/16;x=side*(.021+.050*t);z=1.668+.005*t+.0015*math.sin(t*math.pi)
            w=.0024*math.sin(math.pi*t)**.5
            for zz in [z-w*.35,z+w]:
                hit,p,_,_=head.ray_cast(Vector((x,-.3,zz)),Vector((0,1,0)))
                assert hit and p.y<0, 'Brow must follow the finished forehead'
                vs.append((x,p.y-.0002,zz))
        for i in range(16):a=i*2;fs.append((a,a+2,a+3,a+1))
        brow=mesh('Mio shaped brow '+label,vs,fs,LASH);bind(brow,'Head')
        points=[]
        for i in range(13):
            a=math.acos(-.70+1.42*i/12);q=eye_edge(a,side);q.z+=.0025
            hit,p,_,_=head.ray_cast(Vector((q.x,-.3,q.z)),Vector((0,1,0)))
            assert hit and p.y<0, 'Upper lid fold must follow the skin'
            points.append((q.x,p.y-.00012,q.z))
        facial_curve('Mio subtle upper lid fold '+label,points,.00012,LID)
        # Tiny underside marks follow the nasal wings rather than a painted front slit.
        vs=[];n=12
        for r,a in [(0,0)]+[(1,i*math.tau/n) for i in range(n)]:
            x=side*.0065+.0012*r*math.cos(a);z=1.6074+.00055*r*math.sin(a)
            hit,p,_,_=head.ray_cast(Vector((x,-.3,z)),Vector((0,1,0)))
            assert hit and p.y<0, 'Nostril must follow the nasal wing'
            vs.append((x,p.y-.00012,z))
        mark=mesh('Mio subtle nostril '+label,vs,[(0,1+i,1+(i+1)%n) for i in range(n)],LIPLINE);bind(mark,'Head')
    vs=[];fs=[]
    for i in range(25):
        t=i/24;x=-.014+.028*t;z=MOUTH_Z+.0002*(abs(x)/.014)**1.4
        width=.00042*math.sin(math.pi*t)**.6+.00012*math.sin(math.pi*t)**4
        for zz in (z-width,z+width):
            hit,p,_,_=head.ray_cast(Vector((x,-.3,zz)),Vector((0,1,0)))
            assert hit and p.y<0, 'Mouth line must follow the sculpted lips'
            vs.append((x,p.y-.00030,zz))
    for i in range(24):a=i*2;fs.append((a,a+2,a+3,a+1))
    mouth=mesh('Quiet tapered mouth opening',vs,fs,LIPLINE);bind(mouth,'Head')

def curved_profile(z,rows):
    """Cubic interpolation keeps the sculpted section silhouettes smooth."""
    if z<=rows[0][0]:return rows[0][1:]
    if z>=rows[-1][0]:return rows[-1][1:]
    for i,(a,b) in enumerate(zip(rows,rows[1:])):
        if z<=b[0]:
            t=(z-a[0])/(b[0]-a[0]);before=rows[max(0,i-1)];after=rows[min(len(rows)-1,i+2)]
            return tuple((2*t**3-3*t*t+1)*a[k]+(t**3-2*t*t+t)*(b[0]-a[0])*(b[k]-before[k])/(b[0]-before[0])
                +(-2*t**3+3*t*t)*b[k]+(t**3-t*t)*(b[0]-a[0])*(after[k]-a[k])/(after[0]-a[0]) for k in range(1,len(a)))

def face():
    existing=set(bpy.context.scene.objects)
    vs=[];fs=[]
    # Close support rows preserve upper/lower lip volume under subdivision.
    zs=[CHIN,1.555,1.565,1.572,1.578,1.582,1.584,1.5855,MOUTH_Z,1.5898,1.591,1.596,1.603,1.608,
        1.615,1.621,1.630,1.646,1.660,1.676,1.695,1.718,1.742,1.763,1.777,HEAD_TOP]
    us=[0,.11,.22,.36,.50,.64,.78,.88,1]
    nj=len(zs);ni=len(us);eye_low=zs.index(1.615);eye_high=zs.index(1.676)
    jaw_top=1.615
    jaw_rx,_,jaw_back=curved_profile(jaw_top,FACE_LEVELS)
    for side in [-1,1]:
        offset=len(vs)
        for z in zs:
            rx,_,_=curved_profile(z,FACE_LEVELS)
            jaw=max(0,(jaw_top-z)/(jaw_top-CHIN))
            for u in us:
                # Loft the lower face from an anatomical jaw arch, rather than
                # shrinking every horizontal section toward a tiny bottom ring.
                radius=(.056*jaw+jaw_rx*(1-jaw)) if jaw else rx
                center=.006*jaw+jaw_center(jaw_top)*(1-jaw) if jaw else jaw_center(z)
                x=side*radius*u
                y=face_y(x,z,(radius,center))
                zz=z+.042*u**1.7*jaw
                vs.append((x,y,zz))
        def at(i,j):return offset+j*ni+i
        for j in range(nj-1):
            for i in range(ni-1):
                if 1<=i<7 and eye_low<=j<eye_high:continue
                fs.append((at(i,j),at(i+1,j),at(i+1,j+1),at(i,j+1)))
        # Concentric quad loops connect the lid opening all the way into the face.
        boundary=[at(i,eye_high) for i in range(1,8)]+[at(7,j) for j in range(eye_high-1,eye_low-1,-1)]+[at(i,eye_low) for i in range(6,0,-1)]+[at(1,j) for j in range(eye_low+1,eye_high)]
        outer=[Vector(vs[i]) for i in boundary];n=len(outer);rings=[]
        for t in [0,.08,.25,.55]:
            ring=[]
            for q in outer:
                a=math.atan2((q.z-EYE_Z)/.028,(abs(q.x)-EYE_X)/.037)
                p=eye_edge(a,side);v=p.lerp(q,t)
                # Carry the face curvature through the orbit; blending absolute
                # lid depth creates a flat shelf in the side silhouette.
                base=face_y(v.x,v.z)
                v.y=base+(p.y-face_y(p.x,p.z))*(1-smooth(t/.55))
                v.y-=.0005*math.sin(math.pi*min(1,t/.25))*max(0,math.sin(a))
                ring.append(len(vs));vs.append(tuple(v))
            rings.append(ring)
        rings.append(boundary)
        for inner,outer in zip(rings,rings[1:]):
            for i in range(n):k=(i+1)%n;fs.append((inner[i],inner[k],outer[k],outer[i]))
        # Side and occipital surface meet the cheek boundary with welded vertices.
        back_start=len(vs);nr=9
        for j,z in enumerate(zs):
            rx,_,back=curved_profile(z,FACE_LEVELS)
            jaw=max(0,(jaw_top-z)/(jaw_top-CHIN))
            for k in range(nr):
                a=k/(nr-1)*math.pi/2
                x=side*((.056*jaw+jaw_rx*(1-jaw)) if jaw else rx)*math.cos(a)
                y=jaw_center(z)+back*math.sin(a)
                if jaw:y=(.006+.035*math.sin(a))*jaw+(jaw_center(jaw_top)+jaw_back*math.sin(a))*(1-jaw)
                zz=z+(.042+.008*math.sin(a))*jaw
                vs.append((x,y,zz))
        # A raised jaw rim must not fold a longitudinal strip back on itself.
        for start,count in [(offset,ni),(back_start,nr)]:
            assert all(vs[start+(j+1)*count+i][2]>vs[start+j*count+i][2]
                for j in range(nj-1) for i in range(count)), 'Folded jaw control rows'
        for j in range(nj-1):
            for k in range(nr-1):
                a=back_start+j*nr+k;fs.append((a,a+1,a+nr+1,a+nr))
        for j in range(nj-1):fs.append((at(ni-1,j),back_start+j*nr,back_start+(j+1)*nr,at(ni-1,j+1)))
        # The underside spans the curved jaw rim and intersects the upper neck.
        lower=[at(i,0) for i in range(ni)]+[back_start+k for k in range(1,nr)]
        # Curve the midline under the chin as well as the sides. A linear
        # midline leaves a flat ledge even when the lateral jaw is rounded.
        under=lower
        for t in (.14,.38,.70):
            ring=[]
            for index in lower:
                p=Vector(vs[index])
                q=p.lerp(Vector((0,-.023,CHIN+.024)),t)
                q.z-=.009*math.sin(math.pi*t)
                ring.append(len(vs));vs.append(tuple(q))
            for i in range(len(lower)-1):fs.append((under[i],under[i+1],ring[i+1],ring[i]))
            under=ring
        center=len(vs);vs.append((0,-.023,CHIN+.024))
        for a,b in zip(under,under[1:]):fs.append((a,b,center))
        top=[at(i,nj-1) for i in range(ni)]+[back_start+(nj-1)*nr+k for k in range(1,nr)]
        fs.append(tuple(reversed(top)))
    head=mesh('Mio continuous facial control surface',vs,fs,FACE)
    # Remove vertices inside the eye rectangles that no longer belong to a face.
    import bmesh
    bm=bmesh.new();bm.from_mesh(head.data)
    bmesh.ops.delete(bm,geom=[v for v in bm.verts if not v.link_faces],context='VERTS')
    bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=.00001)
    # Relax the cheek/jaw join across its control rows, preserving the apertures
    # and the central nose and mouth. This prevents a horizontal plane break.
    # Blend across the jaw rim as well; dense lip support rows must not crease it.
    weights={v:max(smooth((abs(v.co.x)-.026)/.035)*math.exp(-((v.co.z-1.615)/.013)**2),
        .35*smooth((abs(v.co.x)-.008)/.030)*math.exp(-((v.co.z-(CHIN+.042*min(1,abs(v.co.x)/.056)**1.7))/.014)**2))
        for v in bm.verts if not v.is_boundary and v.co.z<1.630}
    for _ in range(10):
        updates={v:v.co.lerp(sum((e.other_vert(v).co for e in v.link_edges),Vector())/len(v.link_edges),.45*w)
            for v,w in weights.items() if w>.001 and v.link_edges}
        for v,co in updates.items():v.co=co
    bm.to_mesh(head.data);bm.free()
    finish_cage(head,2);assert_connected(head)
    # Retain the editable quad cage while keeping the denser lips within the game budget.
    reduce_detail(head,.65);assert_connected(head)
    head.data.update()
    # The skin-only atlas allows continuous mapping through the upper face.
    def complexion(v,back):
        py=lerp_profile(v.z,[(CHIN,1240),(1.560,1120),(MOUTH_Z,943),
            (1.606,876),(1.611,850),(1.613,825),(1.615,790),
            (1.630,700),(EYE_Z,565),(1.680,400),(HEAD_TOP,70)])[0]
        t=smooth((v.y+.005)/.060);py=py*(1-t)+1100*t
        return pixels(627+v.x/.2167*1254,py)
    uv(head,complexion,FACE)
    for side in [-1,1]:
        hit,p,_,_=head.ray_cast(Vector((side*EYE_X,-.3,EYE_Z)),Vector((0,1,0)))
        assert hit and p.y>0, 'Eye surface must have an open socket'
    neck=tube('Mio neck with clavicle flare',[((0,y,z),rx,ry) for z,y,rx,ry in
        [(1.423,.000,.090,.045),(1.456,.000,.065,.037),(1.478,.002,.043,.032),
         (1.501,.003,.031,.030),(1.533,.004,.030,.031),(1.557,-.002,.031,.035),
         (1.581,.000,.036,.039),(1.606,.007,.046,.043),(1.625,.009,.059,.051),
         (1.638,.012,.065,.055)]],FACE,20)
    finish_cage(neck,2);uv(neck,lambda v,b:(.03,.94),FACE)
    join_neck(head,neck)
    bind(head,lambda v:axial(v.z,[(1.45,'Chest'),(1.51,'Neck'),(CHIN,'Head')]))
    for side in [-1,1]:
        # Helix, recessed concha and lobe share one surface rather than an oval bead.
        vs=[];fs=[];n=32
        for r,depth in [(0,.092),(.28,.091),(.52,.096),(.76,.104),(1,.101)]:
            for i in range(n):
                a=i*math.tau/n
                y=.006+.013*r*math.cos(a)+.003*r*math.sin(a)
                z=1.646+.022*r*math.sin(a)
                vs.append((side*(depth+.003*r*math.sin(a)),y,z))
        for j in range(4):
            for i in range(n):
                a=j*n+i;b=j*n+(i+1)%n;fs.append((a,b,b+n,a+n))
        ear=mesh('Mio sculpted ear bowl',vs,fs,FACE);finish_cage(ear,1)
        thickness(ear,.002);uv(ear,lambda v,b:(.03,.94),FACE);bind(ear,'Head')
        facial_curve('Ear antihelix',[(side*.099,.010,1.660),(side*.100,.003,1.654),(side*.098,.006,1.642),(side*.097,.003,1.634)],.0009,FACE)
    facial_features(head)
    # A single proportional sculpt shortens the lower face, including its features
    # and neck junction, so the lips and eye layers retain their surface attachment.
    parts=set(bpy.context.scene.objects)-existing
    parts.add(bpy.data.scenes['MioControlCages'].objects['Mio continuous facial control surface control cage'])
    for o in parts:
        if o.type!='MESH':continue
        inv=o.matrix_world.inverted()
        for v in o.data.vertices:
            p=o.matrix_world@v.co
            p.z+=.008*smooth((p.z-1.51)/(CHIN-1.51))*smooth((EYE_Z-p.z)/(EYE_Z-CHIN))
            v.co=inv@p
        o.data.update()

def hair_ribbon(name,points,width,index=0,bone='Head',roll=0):
    # Closed curved locks give the illustrated hair a solid silhouette from every view.
    vs=[];fs=[];coords=[];n=8;rows=24 if name=='Mio gathered ponytail' else 18
    if name in ('Mio narrow face framing wisp','Mio layered temple strand','Mio sweep behind ear','Mio tapered ear and nape lock'):
        points=list(points)
        root=Vector((points[0][0]/.103,(points[0][1]-.012)/.103,(points[0][2]-1.707)/.128)).normalized()*.955
        points[0]=(root.x*.103,root.y*.103+.012,root.z*.128+1.707)
    for j in range(rows):
        t=j/(rows-1);c=bezier(points,t)
        tangent=(bezier(points,min(1,t+.01))-bezier(points,max(0,t-.01))).normalized()
        if name in ('Mio narrow face framing wisp','Mio layered temple strand'):
            offset=smooth((1.730-c.z)/.080)
            c.x+=math.copysign(.010,c.x)*offset;c.y+=.012*offset
        side=Vector((1,0,0));side=(side-tangent*side.dot(tangent)).normalized()
        if roll:
            from mathutils import Quaternion
            side=Quaternion(tangent,roll)@side
        depth=tangent.cross(side).normalized()
        if depth.y>0:depth=-depth
        w=width*(.72+.28*math.sin(math.pi*t))*(1-t)**.70
        if name in ('Mio narrow face framing wisp','Mio layered temple strand'):
            w*=1-.20*smooth((t-.45)/.30)
        if name!='Mio gathered ponytail':w*=.35+.65*smooth(t/.18)
        if name=='Mio gathered ponytail':w*=.16+.84*smooth(t/.28)
        if j==rows-1:w=.00006
        for i in range(n):
            u=i/n;a=u*math.tau
            fullness=.60 if name=='Mio gathered ponytail' else .23
            v=c+side*math.cos(a)*w+depth*math.sin(a)*w*fullness
            vs.append(tuple(v));coords.append(((index*.131)% .72+u*.24,.96-(index%3)*.04-t*.80))
    for j in range(rows-1):
        for i in range(n):
            a=j*n+i;b=j*n+(i+1)%n;fs.append((a,a+n,b+n,b))
    fs.extend([tuple(reversed(range(n))),tuple(range((rows-1)*n,rows*n))])
    o=mesh(name,vs,fs,HAIR if index%3 else HAIRLIGHT)
    import bmesh
    bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(o.data);bm.free()
    layer=o.data.uv_layers.new(name='UVMap')
    for p in o.data.polygons:
        for li in p.loop_indices:layer.data[li].uv=coords[o.data.loops[li].vertex_index]
    bind(o,bone)
    return o

def hair():
    vs=[];fs=[];coords=[];n=64;rows=23
    gather=Vector((.020/.103,.078/.103,.069/.128)).normalized()
    right=(Vector((1,0,0))-gather*gather.x).normalized();up=gather.cross(right)
    def crown_uv(p):
        direction=Vector((p[0]/.103,(p[1]-.012)/.103,(p[2]-1.707)/.128)).normalized()
        return (.5+math.atan2(direction.dot(up),direction.dot(right))/math.tau,
                1-math.acos(max(-1,min(1,direction.dot(gather))))/math.pi)
    for j in range(rows):
        for i in range(n):
            a=i*math.tau/n;c=math.cos(a)
            # Crown follows the skull; the ear is exposed where the sweep turns back.
            end=1.70+.35*max(0,-c)-.60*max(0,c)
            phi=.012+(end-.012)*j/(rows-1)
            vs.append((math.sin(a)*math.sin(phi)*.103,-c*math.sin(phi)*.103+.012,1.707+math.cos(phi)*.128))
            coords.append(crown_uv(vs[-1]))
    for j in range(rows-1):
        for i in range(n):a=j*n+i;b=j*n+(i+1)%n;fs.append((a,a+n,b+n,b))
    pole=len(vs);vs.append((0,.012,1.835));coords.append(crown_uv(vs[-1]))
    for i in range(n):fs.append((pole,i,(i+1)%n))
    o=mesh('Mio close swept crown',vs,fs,HAIR)
    import bmesh
    bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(o.data);bm.free()
    layer=o.data.uv_layers.new(name='UVMap')
    for p in o.data.polygons:
        values=[coords[o.data.loops[li].vertex_index] for li in p.loop_indices]
        seam=max(u for u,v in values)-min(u for u,v in values)>.5
        for li,(u,v) in zip(p.loop_indices,values):layer.data[li].uv=(u+1 if seam and u<.5 else u,v)
    bind(o,'Head')
    locks=[
        ([(-.052,-.033,1.818),(-.095,-.085,1.790),(-.110,-.098,1.717),(-.082,-.090,1.671)],.018),
        ([(-.050,-.038,1.817),(-.065,-.132,1.780),(-.025,-.125,1.736),(-.012,-.100,1.701)],.023),
        ([(-.040,-.025,1.823),(.016,-.125,1.790),(.055,-.118,1.735),(.066,-.090,1.685)],.022),
        ([(-.020,-.010,1.821),(.065,-.085,1.795),(.112,-.075,1.725),(.085,-.062,1.657)],.024),
        ([(-.061,-.031,1.813),(-.092,-.121,1.762),(-.066,-.121,1.712),(-.046,-.097,1.687)],.014),
        ([(-.032,-.038,1.817),(-.013,-.130,1.775),(.025,-.123,1.744),(.037,-.102,1.707)],.014),
        ([(-.058,-.042,1.813),(-.070,-.123,1.767),(-.037,-.126,1.727),(-.024,-.110,1.684)],.005),
        ([(-.031,-.038,1.820),(.031,-.122,1.779),(.052,-.122,1.723),(.039,-.102,1.673)],.006),
        ([(-.070,-.014,1.806),(-.116,-.070,1.762),(-.100,-.103,1.694),(-.084,-.071,1.643)],.008)]
    for i,(points,w) in enumerate(locks):
        points=[(x,y+.022*smooth((-y-.04)/.06),z) for x,y,z in points]
        # Stagger roots along the part and bury them just beneath the crown.
        rx=-.026+.004*(i%3);ry=-.034+.010*(i//3)
        points[0]=(rx,ry,1.707+.128*math.sqrt(1-(rx/.103)**2-((ry-.012)/.103)**2)-.0015)
        hair_ribbon('Mio shaped fringe',points,w,i)
    for side in [-1,1]:
        hair_ribbon('Mio narrow face framing wisp',[(side*.055,-.020,1.811),(side*.100,-.065,1.725),(side*.106,-.080,1.622),(side*.087,-.045,1.604)],.0085,7)
        hair_ribbon('Mio layered temple strand',[(side*.064,-.018,1.804),(side*.120,-.064,1.747),(side*.110,-.071,1.656),(side*.072,-.053,1.650)],.0105,9,roll=side*.32)
        hair_ribbon('Mio sweep behind ear',[(side*.087,.017,1.782),(side*.113,.045,1.724),(side*.093,.095,1.676),(side*.067,.086,1.616)],.014,8,roll=side*1.1)
        for i in range(2):
            hair_ribbon('Mio tapered ear and nape lock',[(side*.093,.028+i*.018,1.734),
                (side*(.107-i*.008),.033+i*.025,1.696),(side*(.085-i*.010),.053+i*.025,1.637),
                (side*(.060-i*.014),.038+i*.026,1.596+i*.005)],.009,18+i,roll=side*1.2)
    # Overlapping curved masses make a gathered ponytail with an irregular silhouette.
    for i,(dx,dy,dz,w,roll) in enumerate([(.006,.005,-.010,.049,.1),(-.026,-.004,.017,.028,-.45),(.031,.008,-.021,.023,.55),(-.012,.025,.037,.023,-.15),(.049,-.005,.008,.017,.65)]):
        hair_ribbon('Mio gathered ponytail',[(.020+dx*.08,.094,1.773),(.028+dx,.173,1.796+dz*.2),(.044+dx,.205+dy,1.663+dz*.5),(.045+dx*1.45,.126+dy,1.569+dz)],w,10+i,
            lambda v:blend('HairTip','HairBase',(v.z-1.59)/.17),roll=roll)
    tie=tube('Brass ponytail clasp',[((.020,.086,1.778),.017,.012),((.022,.109,1.769),.017,.012)],GOLD,24);bind(tie,'Head')

TORSO=[(1.012,.176,.114),(1.067,.157,.102),(1.128,.145,.096),(1.181,.139,.094),
    (1.239,.150,.101),(1.291,.157,.109),(1.345,.164,.111),(1.397,.153,.091),
    (1.432,.142,.074),(1.464,.090,.049),(1.494,.045,.038)]
BUST=.035
TAIL_ROWS=[(.43,.236,.165),(.68,.232,.164),(.80,.222,.157),(.96,.208,.151),(1.025,.185,.146),(1.09,.147,.095)]

def upper_weights(v):
    body=axial(v.z,[(1.03,'Hips'),(1.19,'Spine'),(1.39,'Chest')])
    side='L' if v.x<0 else 'R'
    arm=axial(v.z,[(1.16,side+'Forearm'),(1.275,side+'Arm'),(1.480,side+'Arm'),(1.53,side+'Clavicle')])
    # The torso sides stay on the spine; arm influence starts beyond the ribcage.
    inside=lerp_profile(v.z,[(1.0,.235),(1.18,.205),(1.30,.178),(1.35,.125),(1.435,.12),(1.50,.068)])[0]
    amount=smooth((abs(v.x)-inside)/.075)
    return {**{k:w*(1-amount) for k,w in body.items()},**{k:w*amount for k,w in arm.items()}}

def torso_volume(o):
    for v in o.data.vertices:
        x,y,z=v.co;front=smooth(-y/.07)
        bust=(math.exp(-((x-.067)/.061)**2)+math.exp(-((x+.067)/.061)**2))*math.exp(-((z-1.343)/.058)**2)
        v.co.y-=front*BUST*bust
        # Sternum, lower ribs and the small inward curve of the lumbar back.
        if y>0:
            v.co.y-=.010*math.exp(-((z-1.18)/.085)**2)
            v.co.y+=.006*math.exp(-((z-1.36)/.07)**2)
    o.data.update();return o

def torso_mesh(name,inset=0):
    sections=[]
    for i in range(33):
        z=TORSO[0][0]+(TORSO[-1][0]-TORSO[0][0])*i/32
        rx,ry=curved_profile(z,TORSO);sections.append(((0,0,z),rx-inset,ry-inset))
    return torso_volume(tube(name,sections,COSTUME,48))

def coat_control_mesh():
    # A single quad cage flows from ribcage through each armhole into its sleeve.
    vs=[];fs=[];n=33;sleeve_weights={}
    zs=[1.005,1.04,1.09,1.14,1.19,1.245,1.30,1.35,1.40,1.445,1.47,1.495]
    for z in zs:
        rx,ry=curved_profile(z,TORSO)
        for i in range(n):
            a=.48+(math.tau-.96)*i/(n-1)
            vs.append((math.sin(a)*rx,-math.cos(a)*ry,z))
    def at(i,j):return j*n+i
    for j in range(len(zs)-1):
        for i in range(n-1):
            if 6<=j<9 and (4<=i<9 or 23<=i<28):continue
            fs.append((at(i,j),at(i+1,j),at(i+1,j+1),at(i,j+1)))
    for side,start,end in [(1,4,9),(-1,23,28)]:
        border=[at(i,9) for i in range(start,end+1)]+[at(end,j) for j in range(8,5,-1)]+[at(i,6) for i in range(end-1,start-1,-1)]+[at(start,j) for j in range(7,9)]
        angles=[]
        for idx in border:
            p=Vector(vs[idx]);angles.append(math.atan2((side*p.x-.145)*.50+(p.z-1.375)*.866,-p.y))
        last=border;ring_number=0
        for x,z,rx,ry in [(.176,1.397,.054,.057),(.203,1.350,.053,.052),(.234,1.293,.049,.045),(.264,1.24,.042,.039),(.279,1.212,.042,.038),(.289,1.188,.045,.038),(.314,1.127,.037,.031),(.325,1.105,.029,.025)]:
            ring=[]
            for a in angles:
                ca=math.cos(a);sa=math.sin(a)
                # Deltoid and elbow folds follow their anatomical cross-section.
                fold=.0035*math.sin(a*3+z*100)*math.exp(-((z-1.215)/.055)**2)
                ring.append(len(vs));vs.append((side*(x+sa*(rx+fold)*.866),-ca*(ry+fold),z+sa*(rx+fold)*.50))
                prefix='R' if side>0 else 'L'
                w=axial(z,[(1.16,prefix+'Forearm'),(1.26,prefix+'Arm')])
                if ring_number==0:
                    body=upper_weights(Vector(vs[-1]))
                    w={k:.7*w.get(k,0)+.3*body.get(k,0) for k in set(w)|set(body)}
                sleeve_weights[ring[-1]]=w
            for i in range(len(ring)):
                k=(i+1)%len(ring);fs.append((last[i],last[k],ring[k],ring[i]))
            last=ring
            ring_number+=1
    o=mesh('Open tailored traveling coat',vs,fs,COSTUME)
    bind(o,upper_weights)
    # Sleeve cross-sections rotate together; position-only weights pinned the inner
    # sleeve to the chest and produced a pointed flap when the arm was raised.
    for index,weights in sleeve_weights.items():
        for group in o.vertex_groups:group.remove([index])
        for name,w in weights.items():
            group=o.vertex_groups.get(name) or o.vertex_groups.new(name=name)
            group.add([index],w,'REPLACE')
    for mod in list(o.modifiers):o.modifiers.remove(mod)
    import bmesh
    bm=bmesh.new();bm.from_mesh(o.data)
    bmesh.ops.delete(bm,geom=[v for v in bm.verts if not v.link_faces],context='VERTS');bm.to_mesh(o.data);bm.free()
    finish_cage(o,2);torso_volume(o);assert_connected(o)
    reduce_detail(o,.68)
    thickness(o,.003);fabric_uv(o,0)
    mod=o.modifiers.new('Smooth anatomical skinning','ARMATURE');mod.object=RIG
    return o

def coat_weights(v):
    w=.82*smooth((1.04-v.z)/.40)
    return {'Hips':1-w,('LThigh' if v.x<0 else 'RThigh'):w}

def garments():
    shirt=torso_mesh('Fitted ivory blouse',.007)
    # Keep an opaque undershirt beneath the lapels while tucking its sides inside the coat.
    for v in shirt.data.vertices:
        a=abs(math.atan2(v.co.x,-v.co.y));t=smooth((a-.52)/.42)
        v.co.x*=1-.22*t;v.co.y*=1-.22*t
    shirt.data.update();reduce_detail(shirt,.58)
    fabric_uv(shirt,1);bind(shirt,upper_weights)
    jacket=coat_control_mesh()
    for side,s in [('L',-1),('R',1)]:
        cuff=tube('Turned ivory cuff',[((s*.317,0,1.159),.039,.038),((s*.317,0,1.154),.044,.043),
            ((s*.320,0,1.148),.046,.045),((s*.325,-.002,1.132),.042,.041),
            ((s*.331,-.004,1.109),.036,.035),((s*.335,-.006,1.096),.033,.032)],COSTUME,24)
        # Open sleeves with a rolled lip; end caps used to cut across the wrist.
        import bmesh
        bm=bmesh.new();bm.from_mesh(cuff.data)
        bmesh.ops.delete(bm,geom=[f for f in bm.faces if len(f.verts)>4],context='FACES');bm.to_mesh(cuff.data);bm.free()
        finish_cage(cuff,1);thickness(cuff,.002)
        fabric_uv(cuff,1);bind(cuff,side+'Forearm')
        pts=[]
        for z,rx,ry in TORSO:
            x=s*math.sin(.5)*rx;y=-math.cos(.5)*ry-.004
            bust=(math.exp(-((x-.067)/.061)**2)+math.exp(-((x+.067)/.061)**2))*math.exp(-((z-1.343)/.058)**2)
            pts.append((x,y-BUST*bust,z))
        curve('Antique gold jacket edge',pts,.00125,GOLD,upper_weights)
    collar=shell('Raised military collar',[(1.466,.057,.049),(1.518,.066,.051),(1.564,.070,.053)],COSTUME,.72)
    fabric_uv(collar,0)
    thickness(collar,.004);bind(collar,'Neck')
    for s in [-1,1]:
        curve('Standing collar brass edge',[(s*.038,-.038,1.468),(s*.044,-.039,1.518),(s*.046,-.041,1.564)],.0014,GOLD,'Neck')
        for z,x,y in [(1.496,.051,-.034),(1.547,.054,-.035)]:uvball('Collar brass button',(s*x,y,z),(.004,.0025,.004),GOLD,'Neck')
    inner=shell('Ivory standing shirt collar',[(1.484,.042,.038),(1.516,.035,.034),(1.550,.034,.033)],COSTUME,.48)
    fabric_uv(inner,1);thickness(inner,.002);bind(inner,'Neck')
    tails=shell('Split long coat tails',TAIL_ROWS,COSTUME,.82,True)
    fabric_uv(tails,0)
    thickness(tails,.003)
    bind(tails,coat_weights)
    pieces=[uvball('Trouser pelvis',(0,0,.98),(.178,.122,.125),COSTUME)]
    for s in [-1,1]:
        pieces.append(tube('Fitted trousers',[((s*x,y,z),rx,ry) for x,y,z,rx,ry in [(.089,.010,1.025,.088,.105),(.099,.012,.952,.091,.107),(.105,-.005,.862,.085,.100),(.108,-.012,.773,.077,.089),(.106,-.024,.671,.057,.065),(.104,-.027,.586,.048,.051),(.104,-.018,.547,.046,.048)]],COSTUME,32))
    o=union('Painted trouser folds',pieces,COSTUME,.008)
    reduce_detail(o,.75)
    for v in o.data.vertices:
        p=o.matrix_world@v.co
        side=-1 if p.x<0 else 1
        angle=math.atan2(p.x-side*.104,-p.y)
        # Compression folds gather above the boots and under the hip crease.
        fold=.0045*math.sin(p.z*125+2*math.sin(angle))*math.exp(-((p.z-.66)/.065)**2)
        fold+=.005*math.sin(p.z*110-3*math.sin(angle))*math.exp(-((p.z-.91)/.065)**2)
        fold*=smooth((abs(p.x)-.025)/.04)
        p.x+=math.sin(angle)*fold;p.y-=math.cos(angle)*fold
        v.co=o.matrix_world.inverted()@p
    o.data.materials.clear();o.data.materials.append(TROUSERS)
    def pants_weights(v):
        weights=leg_weights(v)
        pelvis=smooth((v.z-.79)/.06)*(1-smooth((abs(v.x)-.025)/.060))
        weights={k:w*(1-pelvis) for k,w in weights.items()}
        weights['Hips']=weights.get('Hips',0)+pelvis
        return weights
    bind(o,pants_weights)

def garment_front(x,z,inset=0):
    if 'SHIRT_SURFACE' in globals():
        surfaces=[SHIRT_SURFACE] if inset else [SHIRT_SURFACE,JACKET_SURFACE]
        hits=[s.ray_cast(Vector((x,-1,z)),Vector((0,1,0)))[0] for s in surfaces]
        hits=[p.y for p in hits if p is not None]
        if hits:return min(hits)-(0 if inset else .009)
    rx,ry=curved_profile(z,TORSO);rx-=inset;ry-=inset
    y=-ry*math.sqrt(max(.01,1-(x/rx)**2))
    bust=(math.exp(-((x-.067)/.061)**2)+math.exp(-((x+.067)/.061)**2))*math.exp(-((z-1.343)/.058)**2)
    return y-BUST*bust*smooth(-y/.07)

def tailor_details():
    # Cloth tension belongs to the surface, so it shades and deforms with the garment.
    for o in bpy.context.scene.objects:
        if o.type!='MESH':continue
        if o.name in ['Open tailored traveling coat','Fitted ivory blouse']:
            for v in o.data.vertices:
                x,y,z=v.co
                if o.name=='Fitted ivory blouse' and y<0:
                    v.co.y-=.0075*math.sin(x*110+z*9)*math.exp(-((z-1.145)/.080)**2)
                    v.co.y-=.005*math.sin((z-abs(x)*.55)*100)*math.exp(-((z-1.285)/.055)**2)
                elif abs(x)>.21:
                    phase=z*105+abs(x)*32+2*math.tanh(y/.03)
                    fold=.014*math.sin(phase)*math.exp(-((z-1.22)/.095)**2)
                    v.co.y+=fold*math.tanh(y/.018)
                    v.co.x+=(1 if x>0 else -1)*fold*.35
                elif o.name=='Open tailored traveling coat':
                    fold=.0035*math.sin((z-abs(x)*.7)*110)*math.exp(-((z-1.19)/.075)**2)
                    v.co.y+=fold*(1 if y>0 else -1)
            o.data.update()
        if o.name=='Split long coat tails':
            for v in o.data.vertices:
                a=math.atan2(v.co.x,-v.co.y);fold=.011*math.sin(a*5+v.co.z*3)*smooth((1.0-v.co.z)/.3)
                v.co.x+=math.sin(a)*fold;v.co.y-=math.cos(a)*fold
    tails=bpy.context.scene.objects['Split long coat tails']
    # Trace the existing hem and vent vertices so the trim shares their fit and skinning.
    paths=[[j*48+i for j in range(len(TAIL_ROWS)-1,-1,-1)] for i in [0,47]]
    paths += [list(range(0,24,2))+[23],list(range(24,48,2))+[47]]
    paths += [[j*48+i for j in range(len(TAIL_ROWS)-1)] for i in [23,24]]
    for indices in paths:
        points=[]
        for i in indices:
            p=tails.data.vertices[i].co.copy()
            a=math.atan2(p.x,-p.y);p+=Vector((math.sin(a),-math.cos(a),0))*.002
            points.append(p)
        trim=curve('Coat hem and vent brass piping',points,.0018,GOLD,coat_weights)
        reduce_detail(trim,.25)
    from mathutils.bvhtree import BVHTree
    global SHIRT_SURFACE,JACKET_SURFACE
    shirt=bpy.context.scene.objects['Fitted ivory blouse']
    SHIRT_SURFACE=BVHTree.FromPolygons([v.co for v in shirt.data.vertices],[p.vertices[:] for p in shirt.data.polygons])
    jacket=bpy.context.scene.objects['Open tailored traveling coat']
    JACKET_SURFACE=BVHTree.FromPolygons([v.co for v in jacket.data.vertices],[p.vertices[:] for p in jacket.data.polygons])
    back_paths=[[(s*x,z) for x,z in [(.047,1.10),(.052,1.18),(.077,1.29),(.110,1.40)]] for s in [-1,1]]
    back_paths.append([(x,1.40-.025*(1-abs(x)/.12)) for x in [-.12,-.06,0,.06,.12]])
    for path in back_paths:
        points=[]
        samples=[(x+(xx-x)*j/6,z+(zz-z)*j/6) for (x,z),(xx,zz) in zip(path,path[1:]) for j in range(6)]+[path[-1]]
        for x,z in samples:
            hit,_,_,_=JACKET_SURFACE.ray_cast(Vector((x,1,z)),Vector((0,-1,0)))
            assert hit is not None, 'Back seam must follow the coat'
            points.append(hit+Vector((0,.002,0)))
        seam=curve('Tailored back panel seam',points,.0009,GOLD,upper_weights)
        reduce_detail(seam,.35)
    for z in [1.14,1.19,1.24,1.29,1.34,1.39]:
        uvball('Blouse antique button',(0,garment_front(0,z,.008)-.007,z),(.0038,.002,.0038),GOLD,upper_weights)
    for s in [-1,1]:
        vs=[];fs=[];edge=[]
        rows=[(1.240,.052,.056),(1.300,.059,.090),(1.370,.065,.113),(1.410,.061,.134),(1.465,.039,.086),(1.510,.028,.066)]
        dense=[]
        for a,b in zip(rows,rows[1:]):
            for j in range(4):dense.append(tuple(x+(y-x)*j/4 for x,y in zip(a,b)))
        dense.append(rows[-1])
        for z,inner,outer in dense:
            for i in range(5):
                t=i/4;x=s*(inner+(outer-inner)*t)
                y=garment_front(x,z)-.010-.009*math.sin(t*math.pi)
                vs.append((x,y,z))
            edge.append(vs[-1])
        for i in range(len(dense)-1):
            for j in range(4):
                a=i*5+j;fs.append((a,a+1,a+6,a+5))
        o=mesh('Turned traveling coat lapel',vs,fs,COSTUME)
        lapel_weights=lambda v:axial(v.z,[(1.03,'Hips'),(1.19,'Spine'),(1.39,'Chest')])
        finish_cage(o,0);thickness(o,.003);fabric_uv(o,0);bind(o,lapel_weights)
        curve('Lapel brass piping',edge,.0013,GOLD,lapel_weights)
        uvball('Lapel pin',edge[-3],(.0038,.003,.0038),GOLD,lapel_weights)
        seam=[]
        for z,x in [(1.095,.115),(1.18,.105),(1.26,.112),(1.34,.137),(1.40,.138)]:
            seam.append((s*x,garment_front(s*x,z)-.0015,z))
        curve('Tailored princess seam',seam,.0007,SEAM,upper_weights)
        for z in [1.14,1.26,1.37]:
            x=s*math.sin(.5)*curved_profile(z,TORSO)[0]
            uvball('Coat fastening stud',(x,garment_front(x,z)-.006,z),(.0035,.002,.0035),GOLD,upper_weights)

def boots():
    for side,s in [('L',-1),('R',1)]:
        sections=[((s*.104,y,z-.005),rx,rz) for y,z,rx,rz in [(.075,.079,.034,.042),(.036,.087,.040,.053),(-.02,.073,.043,.047),(-.09,.055,.048,.032),(-.16,.047,.039,.025),(-.196,.047,.009,.012)]]
        toe=tube('Riding boot shaped toe',sections,COSTUME,24)
        shaft=tube('Knee-high riding boot',[((s*.104,y,z),rx,ry) for z,y,rx,ry in [(.11,.012,.038,.043),(.18,.016,.036,.043),(.28,.021,.048,.056),(.37,.012,.060,.069),(.445,-.003,.060,.070),(.525,-.020,.051,.059),(.585,-.023,.054,.059)]],COSTUME,32)
        o=union('Mio riding boot '+side,[toe,shaft],COSTUME,.005)
        # Reserve detail for the visible leather folds and straps, not remesh density.
        reduce_detail(o,.65)
        for v in o.data.vertices:
            p=o.matrix_world@v.co;x=p.x-s*.104;angle=math.atan2(x,-(p.y-.014))
            fold=.006*math.sin(p.z*118+1.4*math.sin(angle))*math.exp(-((p.z-.19)/.085)**2)
            fold+=.0035*math.sin(p.z*95-angle)*math.exp(-((p.z-.49)/.055)**2)
            p.x+=math.sin(angle)*fold;p.y-=math.cos(angle)*fold
            v.co=o.matrix_world.inverted()@p
        fabric_uv(o,3);bind(o,lambda v,s=side:foot_weights(v,s))
        sole=tube('Mio boot outsole '+side,[((s*.104,y,.021),rx,rz) for y,rx,rz in [(.066,.032,.015),(.02,.042,.015),(-.08,.050,.015),(-.16,.041,.014),(-.20,.005,.008)]],SOLE,24)
        bind(sole,lambda v,s=side:foot_weights(v,s))
        profile=[(.11,.012,.038,.043),(.18,.016,.036,.043),(.28,.021,.048,.056),(.37,.012,.060,.069),(.445,-.003,.060,.070),(.525,-.020,.051,.059),(.585,-.023,.054,.059)]
        from mathutils.bvhtree import BVHTree
        surface=BVHTree.FromPolygons([o.matrix_world@v.co for v in o.data.vertices],[list(p.vertices) for p in o.data.polygons])
        def strap_point(a,z):
            y,_,_=lerp_profile(z,profile)
            direction=Vector((math.sin(a),-math.cos(a),0))
            origin=Vector((s*.104,y,z))+direction*.15
            hit,_,_,_=surface.ray_cast(origin,-direction,.3)
            assert hit is not None, 'Boot strap must meet the boot surface'
            return hit+direction*.004
        for z in [.17,.49,.565]:
            vs=[];fs=[];n=32
            for dz in [-.008,.008]:
                y,rx,ry=lerp_profile(z+dz,profile)
                for i in range(n):
                    a=i*math.tau/n
                    vs.append(tuple(strap_point(a,z+dz+.008*math.sin(a))))
            for i in range(n):k=(i+1)%n;fs.append((i,k,k+n,i+n))
            band=mesh('Riding boot leather strap',vs,fs,LEATHER);thickness(band,.002)
            bind(band,lambda v,side=side:foot_weights(v,side))
            y,rx,ry=lerp_profile(z,profile)
            center=strap_point(s*math.pi/4,z+s*.006)
            buckle=softbox('Boot buckle',center,(.020,.010,.022),GOLD,lambda v,side=side:foot_weights(v,side),.002)
            softbox('Boot buckle leather inset',(center[0],center[1]-.006,center[2]),(.011,.003,.013),LEATHER,lambda v,side=side:foot_weights(v,side),.001)

def accessories():
    bag=softbox('Illustrated utility satchel',(.238,-.007,.956),(.154,.112,.194),COSTUME,'Hips',.023)
    fabric_uv(bag,3)
    flap=softbox('Satchel overlapping flap',(.237,-.071,1.008),(.154,.014,.088),COSTUME,'Hips',.015)
    fabric_uv(flap,3)
    for x in [.203,.270]:
        softbox('Satchel closure strap',(x,-.087,.967),(.017,.007,.112),LEATHER,'Hips',.004)
        softbox('Satchel buckle',(x,-.094,.988),(.023,.007,.029),GOLD,'Hips',.003)
        softbox('Satchel buckle inset',(x,-.098,.988),(.014,.003,.019),LEATHER,'Hips',.002)
        softbox('Satchel buckle tongue',(x,-.101,.988),(.002,.003,.020),GOLD,'Hips',.0007)
        for z in [.927,.940,.953]:
            softbox('Satchel strap punched hole',(x,-.0915,z),(.0025,.001,.0035),SOLE,'Hips',.0005)
    curve('Satchel stitched flap',[(.170,-.081,1.039),(.170,-.083,.980),(.237,-.084,.970),(.304,-.083,.980),(.304,-.081,1.039)],.00065,GOLD,'Hips')
    for x in [.169,.307]:
        piping=curve('Satchel gusset piping',[(x,-.041,1.039),(x,-.057,.996),(x,-.057,.893),(x,-.036,.872),(x,.028,.879),(x,.040,.980),(x,.027,1.038)],.0015,LEATHER,'Hips')
        reduce_detail(piping,.3)
    for x in [.177,.298]:
        for z in [.894,1.032]:
            softbox('Satchel corner rivet',(x,-.068,z),(.006,.003,.006),GOLD,'Hips',.002)
    pocket=softbox('Satchel front patch pocket',(.237,-.066,.913),(.104,.012,.059),COSTUME,'Hips',.009)
    fabric_uv(pocket,3)
    curve('Satchel pocket seam',[(.188,-.074,.935),(.190,-.075,.892),(.237,-.075,.886),(.284,-.075,.892),(.286,-.074,.935)],.0006,GOLD,'Hips')
    for i,z in enumerate([1.112,1.066]):
        rx,ry=lerp_profile(z,TORSO)
        belt=tube('Waist belt' if i==0 else 'Sloped harness belt',[((0,0,z-.011),rx+.004,ry+.005),((0,0,z+.011),rx+.004,ry+.005)],COSTUME,56)
        if i:
            for v in belt.data.vertices:
                a=math.atan2(v.co.x/(rx+.004),-v.co.y/(ry+.005))
                v.co.z+=.042*math.sin(a)
                bx,by=[max(t,c)+.006 for t,c in zip(lerp_profile(v.co.z,TAIL_ROWS),curved_profile(v.co.z,TORSO))]
                v.co.x=math.sin(a)*bx;v.co.y=-math.cos(a)*by
        fabric_uv(belt,3);bind(belt,'Hips')
        x=-.004 if i==0 else -.034;y=-ry-.015;zz=z if i==0 else z-.009
        if i:y=-lerp_profile(zz,TAIL_ROWS)[1]-.016
        softbox('Waist buckle',(x,y,zz),(.046,.013,.032),GOLD,'Hips',.003)
        softbox('Waist buckle leather inset',(x,y-.008,zz),(.030,.004,.020),LEATHER,'Hips',.002)
        softbox('Waist buckle tongue',(x,y-.011,zz),(.031,.003,.0025),GOLD,'Hips',.0007)
    strap=[]
    for z,x in [(1.476,.074),(1.405,.130),(1.34,.135),(1.265,.122),(1.18,.108),(1.10,.145),(1.07,.181)]:
        rx,ry=curved_profile(z,TORSO);y=-ry*math.sqrt(max(.01,1-(x/rx)**2))
        bust=(math.exp(-((x-.067)/.061)**2)+math.exp(-((x+.067)/.061)**2))*math.exp(-((z-1.343)/.058)**2)
        y-=BUST*bust*smooth(-y/.07)+.009;strap.append((x,y,z))
    strap.append((.233,.010,1.049))
    strap=[tuple(a+(b-a)*j/6 for a,b in zip(p,q)) for p,q in zip(strap,strap[1:]) for j in range(6)]+[strap[-1]]
    strap=[(x,min(y,garment_front(x,z)-.006) if z>1.10 else y,z) for x,y,z in strap]
    strap_weights=lambda v:axial(v.z,[(1.10,'Hips'),(1.19,'Spine'),(1.39,'Chest')])
    vs=[];fs=[]
    for x,y,z in strap:vs.extend([(x-.009,y,z),(x+.009,y,z)])
    for i in range(len(strap)-1):a=i*2;fs.append((a,a+1,a+3,a+2))
    shoulder=mesh('Flat leather shoulder strap',vs,fs,LEATHER)
    thickness(shoulder,.004);bind(shoulder,strap_weights)
    seam=mesh('Shoulder strap seam',[(x+dx,y-.003,z) for x,y,z in strap for dx in [-.0078,-.0064]],fs,GOLD)
    bind(seam,strap_weights)
    for i in [6,24]:
        x,y,z=strap[i];softbox('Shoulder strap buckle',(x,y-.008,z),(.021,.012,.032),GOLD,'Chest' if i==6 else 'Spine',.003)
    # Original illustration: circular ornate terminal on her left (viewer-right) wrist.
    for z in [1.017,1.068]:
        cuff=tube('Terminal leather bracer',[((.338,-.021,z-.01),.036,.032),((.338,-.021,z+.01),.037,.032)],LEATHER,32);bind(cuff,'RForearm')
    o=uvball('Gold terminal bezel',(.34,-.054,1.047),(.042,.016,.049),GOLD,'RForearm')
    uvball('Terminal dark inset',(.34,-.070,1.047),(.033,.007,.039),LEATHER,'RForearm')
    uvball('Round jade terminal glass',(.34,-.077,1.047),(.024,.006,.029),SIGNAL,'RForearm')
    for i in range(12):
        a=i*math.tau/12
        curve('Terminal radial engraving',[(.34+math.sin(a)*.027,-.080,1.047+math.cos(a)*.032),(.34+math.sin(a)*.032,-.077,1.047+math.cos(a)*.037)],.0012,GOLD,'RForearm')
    for z in [1.007,1.087]:softbox('Terminal hinge',(.34,-.058,z),(.049,.022,.010),GOLD,'RForearm',.003)

def scarf():
    vs=[];fs=[];n=32
    profile=[(1.430,.048,.051),(1.452,.061,.068),(1.476,.059,.062),(1.496,.052,.052),(1.517,.044,.043),(1.536,.033,.033)]
    rows=[(z,*curved_profile(z,profile)) for z in [1.430+.106*j/16 for j in range(17)]]
    for j,(z,rx,ry) in enumerate(rows):
        for i in range(n):
            a=i*math.tau/n
            fold=.007*math.sin((z-1.430)*180-1.7*math.sin(a)+.4*math.sin(2*a+j*.3))*(.4+.6*max(0,math.cos(a)))
            fold*=math.sin(math.pi*j/16)**.6
            vs.append((math.sin(a)*(rx+fold*.45),-.026-math.cos(a)*(ry+fold),z-.015*math.cos(a)+.009*math.sin(a)))
    for j in range(len(rows)-1):
        for i in range(n):a=j*n+i;b=j*n+(i+1)%n;fs.append((a,b,b+n,a+n))
    wrap=mesh('Draped red scarf wrap',vs,fs,COSTUME);finish_cage(wrap,0);thickness(wrap,.002);fabric_uv(wrap,2);bind(wrap,'Neck')
    # A stable Y/Z cross-section avoids the generic loft's twist near its X axis.
    vs=[];fs=[];n=16
    for j in range(9):
        t=j/8;fullness=math.sin(math.pi*t)**.7
        for i in range(n):
            a=i*math.tau/n
            vs.append((-.044+.070*t,-.089-.016*math.sin(math.pi*t)+math.cos(a)*(.007+.006*fullness),
                       1.446-.007*math.sin(math.pi*t)+math.sin(a)*(.010+.008*fullness)))
    for j in range(8):
        for i in range(n):a=j*n+i;b=j*n+(i+1)%n;fs.append((a,b,b+n,a+n))
    fs.extend([tuple(reversed(range(n))),tuple(range(8*n,9*n))])
    knot=mesh('Folded scarf crossing',vs,fs,COSTUME);finish_cage(knot,0)
    fabric_uv(knot,2);bind(knot,'Neck')
    for k,points in enumerate([[(-.023,-.103,1.444),(-.036,-.168,1.383),(-.051,-.183,1.297),(-.042,-.148,1.225)],
        [(.004,-.102,1.440),(-.023,-.142,1.407),(-.099,-.166,1.392),(-.145,-.137,1.348)]]):
        vs=[];fs=[];n=9;rows=25
        for j in range(rows):
            t=j/(rows-1);c=bezier(points,t);w=(.026 if k==0 else .017)*(.55+.45*math.sin(t*math.pi))
            axis=Vector((1,.20*math.sin(t*5),.15*math.sin(t*4))).normalized()
            for i in range(n):
                q=2*i/(n-1)-1
                v=c+axis*q*w+Vector((0,.006*math.sin(q*math.pi+t*6)*math.sin(t*math.pi),0))
                vs.append(tuple(v))
        for j in range(rows-1):
            for i in range(n-1):a=j*n+i;fs.append((a,a+1,a+n+1,a+n))
        tail=mesh('Folded scarf tail '+str(k),vs,fs,COSTUME);finish_cage(tail,0);thickness(tail,.0015);fabric_uv(tail,2);bind(tail,lambda v:blend('Neck','Scarf',(v-Vector((-.015,-.103,1.444))).length/.15))

def sword():
    grip=tube('Mio wrapped sword grip',[((-.355,-.026,z),.014,.016) for z in [.976,.856]],LEATHER,20);bind(grip,'LHand')
    for j in range(7):curve('Sword grip crossing wrap',[(-.366,-.039,.867+j*.014),(-.342,-.043,.875+j*.014)],.0015,GOLD,'LHand')
    guard=tube('Circular engraved sword guard',[((-.355,-.026,.847),.057,.047),((-.355,-.026,.859),.057,.047)],GOLD,48);bind(guard,'LHand')
    vs=[];fs=[]
    for i in range(25):
        t=i/24;z=.835-t*.704;x=-.355-.095*t*t;w=.022*(1-.25*t)
        if i==24:w=.001
        vs.extend([(x-w,-.026,z),(x,-.034,z),(x+w,-.026,z)])
    for i in range(24):
        for j in range(2):a=i*3+j;fs.append((a,a+1,a+4,a+3))
    blade=mesh('Curved single-edge sword',vs,fs,STEEL);thickness(blade,.007);bind(blade,'LHand')

def hands():
    boots_and_hands(False,False)
    # Rest-pose finger paths wrap around the actual vertical sword handle.
    paths={}
    for finger,z,r in [('Index',.972,.008),('Middle',.952,.0085),('Ring',.932,.0078),('Pinky',.914,.0068)]:
        paths[finger]=[(-.329,-.026,z),(-.341,-.050,z-.001),(-.365,-.049,z-.002),(-.376,-.030,z-.003)]
    paths['Thumb']=[(-.321,-.009,.991),(-.324,-.037,.982),(-.344,-.058,.976),(-.363,-.056,.967)]
    bpy.ops.object.select_all(action='DESELECT');RIG.select_set(True);bpy.context.view_layer.objects.active=RIG
    bpy.ops.object.mode_set(mode='EDIT')
    for finger,points in paths.items():
        for j in range(3):
            bone=RIG.data.edit_bones['L'+finger+str(j+1)];bone.head=points[j];bone.tail=points[j+1]
    bpy.ops.object.mode_set(mode='OBJECT')
    for side,sign in [('L',-1),('R',1)]:
        hand=next(o for o in bpy.context.scene.objects if o.name=='Articulated hand '+side)
        for mod in list(hand.modifiers):hand.modifiers.remove(mod)
        hand.vertex_groups.clear()
        if side=='L':
            bpy.data.objects.remove(hand,do_unlink=True)
            pieces=[uvball('Mio grip palm',(-.335,-.012,.978),(.021,.023,.035),FACE)]
            for finger,points in paths.items():
                r=.008 if finger!='Pinky' else .0068
                sections=[]
                for j in range(13):
                    t=j/12;c=bezier(points,t);sections.append((tuple(c),r*(1-.28*t),r*(1-.28*t)))
                pieces.append(tube('Mio wrapped '+finger,sections,FACE,12))
            hand=union('Mio sword hand',pieces,FACE,.002)
        bridge=tube('Mio continuous wrist',[((sign*.324,-.014,1.115),.026,.024),((sign*.336,-.017,1.055),.025,.022),((sign*.343,-.020,1.01),.025,.021),((sign*.346,-.021,.984),.027,.022)],FACE,24)
        hand=union('Mio continuous hand '+side,[hand,bridge],FACE,.0022)
        pivot=Vector((sign*.342,-.018,1.015))
        for v in hand.data.vertices:
            world=hand.matrix_world@v.co
            # Keep the wrist at full size and taper the scaling into the fingers.
            factor=1-.1*smooth((1.04-world.z)/.08)
            world=pivot+(world-pivot)*factor;v.co=hand.matrix_world.inverted()@world
        uv(hand,lambda v,b:(.03,.94),FACE);bind(hand,lambda v,s=side:hand_weights(v,s))

def build():
    global COSTUME,FACE,HAIR,HAIRLIGHT,EARLINE,INKLINE,TROUSERS,EYES,EYEWHITE,LASH,LID,LIPLINE,SEAM
    # Regression: arm poses must not drag the waist or lower ribcage sideways.
    for side in [-1,1]:
        for x,z in [(.185,1.02),(.120,1.18),(.170,1.30)]:
            weights=upper_weights(Vector((side*x,0,z)))
            assert sum(w for name,w in weights.items() if name in ['Hips','Spine','Chest'])>.999
        assert sum(w for name,w in upper_weights(Vector((side*.309,0,1.17))).items() if 'Forearm' in name)>.95
    COSTUME=painted('Mio fabric atlas','/assets/art/mio-fabric-turnaround-v1.png',.15)
    FACE=painted('Mio painted skin','/assets/art/mio-skin-painted-v6.png',.025)
    HAIR=painted('Mio painted hair','/assets/art/mio-hair-painted-v3.png',.08)
    HAIRLIGHT=HAIR
    TROUSERS=mat('Mio charcoal trousers',(.028,.025,.030),.91)
    SEAM=mat('Mio cloth stitching',(.020,.060,.063),.95)
    EYES=painted('Mio illustrated amber eyes','/assets/art/mio-iris-painted-v8.png',.06)
    EYEWHITE=mat('Mio warm sclera',(.80,.77,.71),.78,emission=.04)
    EYES.node_tree.nodes.get('Image Texture').extension='EXTEND'
    # Painted highlights carry the reference's eye design; avoid extra specular beads.
    EYES.node_tree.nodes.get('Principled BSDF').inputs['Roughness'].default_value=.85
    EYES.node_tree.nodes.get('Principled BSDF').inputs['Specular IOR Level'].default_value=0
    LASH=mat('Mio fine lashes',(.011,.009,.014),.8)
    LID=mat('Mio eyelid crease',(.41,.24,.17),.85)
    LIPLINE=mat('Mio mouth line',(.44,.22,.18),.85,emission=.15)
    EARLINE=mat('Mio ear line',(.45,.27,.19),1)
    INKLINE=mat('Mio fine hair ink',(.008,.012,.021),1)
    hands()
    garments();tailor_details();boots();face()
    old=set(bpy.context.scene.objects);hair()
    for o in set(bpy.context.scene.objects)-old:
        for v in o.data.vertices:v.co.z-=.030
    accessories();scarf();sword()
    for o in list(bpy.context.scene.objects):
        if o.type=='MESH' and o.data.materials and o.data.materials[0]==GOLD:
            reduce_detail(o,.32)
    fit_proportions()
    import runpy
    runpy.run_path(ROOT+'/tools/mio_turnaround.py',init_globals={'RIG':RIG})['fit_to_sheet']()
    # Match Godot's four-weight skinning in the authoring scene and exported asset.
    for o in bpy.context.scene.objects:
        if o.type!='MESH':continue
        for v in o.data.vertices:
            weights=sorted([(g.group,g.weight) for g in v.groups],key=lambda p:-p[1])
            if len(weights)<=4:continue
            total=sum(w for _,w in weights[:4])
            for index,_ in weights[4:]:o.vertex_groups[index].remove([v.index])
            for index,w in weights[:4]:o.vertex_groups[index].add([v.index],w/total,'REPLACE')


def fit_proportions():
    def arm_drop(z):return .045*smooth((1.30-z)/.25)
    arm_names={b.name for b in RIG.data.bones if b.name.startswith(('L','R')) and any(k in b.name for k in ['Arm','Forearm','Hand','Thumb','Index','Middle','Ring','Pinky'])}
    for o in bpy.context.scene.objects:
        if o.type!='MESH':continue
        names={g.index:g.name for g in o.vertex_groups}
        for v in o.data.vertices:
            arm=sum(g.weight for g in v.groups if names[g.group] in arm_names)
            world=o.matrix_world@v.co
            world.z-=arm*arm_drop(world.z)
            v.co=o.matrix_world.inverted()@world
        o.data.update()
    bpy.ops.object.select_all(action='DESELECT');RIG.select_set(True);bpy.context.view_layer.objects.active=RIG
    bpy.ops.object.mode_set(mode='EDIT')
    for b in RIG.data.edit_bones:
        if b.name.startswith(('L','R')) and any(k in b.name for k in ['Thumb','Index','Middle','Ring','Pinky']):
            s=-1 if b.name.startswith('L') else 1;pivot=Vector((s*.342,-.018,1.015))
            b.head=pivot+(b.head-pivot)*.90;b.tail=pivot+(b.tail-pivot)*.90
        if b.name in arm_names:
            b.head.z-=arm_drop(b.head.z);b.tail.z-=arm_drop(b.tail.z)
    RIG.data.edit_bones['Head'].head=(0,.020,1.561)
    RIG.data.edit_bones['Head'].tail=(0,.030,1.785)
    RIG.data.edit_bones['Neck'].head=(0,.014,1.465)
    RIG.data.edit_bones['Neck'].tail=(0,.020,1.561)
    for side,sign in [('L',-1),('R',1)]:
        RIG.data.edit_bones[side+'Clavicle'].head=(sign*.032,0,1.462)
        RIG.data.edit_bones[side+'Clavicle'].tail=(sign*.169,0,1.413)
        RIG.data.edit_bones[side+'Arm'].head=(sign*.169,0,1.413)
        RIG.data.edit_bones[side+'Eye'].head=(sign*EYE_X,-.053,EYE_Z)
        RIG.data.edit_bones[side+'Eye'].tail=(sign*EYE_X,-.11,EYE_Z)
    RIG.data.edit_bones['Jaw'].head=(0,.020,1.620)
    RIG.data.edit_bones['Jaw'].tail=(0,-.075,1.563)
    RIG.data.edit_bones['HairBase'].head=(.016,.110,1.740)
    RIG.data.edit_bones['HairBase'].tail=(.089,.235,1.630)
    RIG.data.edit_bones['HairTip'].head=(.089,.235,1.630)
    RIG.data.edit_bones['HairTip'].tail=(.102,.209,1.562)
    bpy.ops.object.mode_set(mode='OBJECT')
