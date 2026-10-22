shader_type spatial;
// Premultiplied, so the sky's fresnel reflection stays bright where the water is clear enough to show the bed.
render_mode blend_premul_alpha, cull_disabled, specular_schlick_ggx;
// World-space drift of the ripples, m/s: each water mesh's material sets its own (the rivers down -X toward the sea, the brook down -Z to its mouth, each gorge reach along its course, the sea still).
uniform vec2 flow = vec2(.35, 0.0);
uniform sampler2D depth : hint_depth_texture, filter_nearest;
varying vec3 world;
// The sheet's own normal: up on level water, leaning downstream on a gorge's steep reaches.
varying vec3 up;
// The wave stays per vertex and at most 3.5 cm: the banks bury the sheets' edges 4 cm deep, and the brook mouth shares the river's vertices.
void vertex(){ world=(MODEL_MATRIX*vec4(VERTEX,1.0)).xyz; up=normalize((MODEL_MATRIX*vec4(NORMAL,0.0)).xyz); VERTEX.y+=sin(VERTEX.x*1.5+TIME)*.035; }
float hash(vec2 p){ return fract(sin(dot(p,vec2(127.1,311.7)))*43758.5453); }
float noise(vec2 p){
 vec2 i=floor(p),f=fract(p); f=f*f*(3.0-2.0*f);
 return mix(mix(hash(i),hash(i+vec2(1,0)),f.x),mix(hash(i+vec2(0,1)),hash(i+vec2(1,1)),f.x),f.y);
}
float ripples(vec2 p){
 vec2 q=p-flow*TIME;
 return noise(q*1.6)*.55+noise(q*4.1+vec2(TIME*.21,-TIME*.17))*.3+noise(q*9.3-vec2(TIME*.4,TIME*.33))*.15;
}
void fragment(){
 vec2 p=world.xz; float h=ripples(p),e=.04;
 vec3 n=normalize(up*e*4.0+vec3(h-ripples(p+vec2(e,0.0)),0.0,h-ripples(p+vec2(0.0,e))));
 NORMAL=normalize((VIEW_MATRIX*vec4(n,0.0)).xyz);
 // Water depth under this pixel, square to the sheet, from the opaque depth already drawn: measured vertically, a steep reach seen
 // from below would read as no water at all. A floor nearer the camera than the surface means no depth texture (it can only be the
 // near plane), so the water reads as deep, the old opaque look.
 vec4 bed=INV_PROJECTION_MATRIX*vec4(SCREEN_UV*2.0-1.0,texture(depth,SCREEN_UV).r,1.0); bed.xyz/=bed.w;
 float deep=bed.z<VERTEX.z?max(dot(world-(INV_VIEW_MATRIX*vec4(bed.xyz,1.0)).xyz,up),0.0):2.0;
 float t=smoothstep(0.0,1.3,deep);
 // Down a reach steeper than about 1 in 8 the water breaks white, in streaks racing along the reach. They follow the material's flow,
 // not the sheet's lean: turned by a degree, a frame 150 m from the origin shifts the streaks by several, scrambling them cell to cell.
 vec2 down=normalize(flow+vec2(1e-5)); float steep=smoothstep(.12,.3,length(up.xz));
 float rapids=steep*smoothstep(.42,.8,noise(vec2(dot(p,vec2(-down.y,down.x))*3.0,dot(p,down)*.35-TIME*1.8))*.7+noise(p*3.0-down*TIME*3.0)*.3);
 float foam=(1.0-smoothstep(0.0,.12,deep))*smoothstep(.5,.85,noise(p*7.0-flow*TIME*1.6));
 float fresnel=pow(1.0-clamp(dot(NORMAL,VIEW),0.0,1.0),4.0);
 float alpha=clamp(mix(.25,.88,t)+fresnel*.3+foam*.3+rapids*.5,0.0,1.0)*smoothstep(0.0,.035,deep);
 ALBEDO=mix(mix(vec3(.24,.26,.17),vec3(.035,.14,.15),t),vec3(.8,.84,.8),max(foam*.45,rapids*.7))*alpha;
 ALPHA=alpha; ROUGHNESS=.12; METALLIC=0.0; SPECULAR=.28;
}
