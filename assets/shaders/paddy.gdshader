shader_type spatial;
// Still paddy water over the mud: a sky mirror where it is seen low, muddier towards the levees, with faint wind ripples.
render_mode blend_premul_alpha, cull_back, specular_schlick_ggx;
// The sheet's size in metres; UV runs 0..1 across it, so the margin is measured from the levees inward.
uniform vec2 size = vec2(6.2, 8.4);
varying vec3 world;
void vertex(){ world=(MODEL_MATRIX*vec4(VERTEX,1.0)).xyz; }
float hash(vec2 p){ return fract(sin(dot(p,vec2(127.1,311.7)))*43758.5453); }
float noise(vec2 p){
 vec2 i=floor(p),f=fract(p); f=f*f*(3.0-2.0*f);
 return mix(mix(hash(i),hash(i+vec2(1,0)),f.x),mix(hash(i+vec2(0,1)),hash(i+vec2(1,1)),f.x),f.y);
}
float ripples(vec2 p){ return noise(p*2.3+vec2(TIME*.13,TIME*.07))*.6+noise(p*6.1-vec2(TIME*.09,-TIME*.15))*.4; }
void fragment(){
 vec2 p=world.xz; float h=ripples(p),e=.05;
 NORMAL=normalize((VIEW_MATRIX*vec4(normalize(vec3(h-ripples(p+vec2(e,0.0)),e*14.0,h-ripples(p+vec2(0.0,e)))),0.0)).xyz);
 vec2 edge=min(UV,1.0-UV)*size;
 float mud=1.0-smoothstep(0.0,.7,min(edge.x,edge.y));
 float fresnel=pow(1.0-clamp(dot(NORMAL,VIEW),0.0,1.0),3.0);
 float alpha=clamp(mix(.55,.85,mud)+fresnel*.2,0.0,1.0);
 ALBEDO=mix(vec3(.12,.15,.09),vec3(.24,.19,.12),mud)*alpha;
 ALPHA=alpha; ROUGHNESS=.05; METALLIC=0.0; SPECULAR=.16;
}
