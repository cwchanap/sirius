shader_type spatial;
// A fall's curtain: UV.x runs across it, UV.y from the lip (0) down to the pool (1). Streaks race down and thicken to foam at its foot;
// its sides thin out against the gorge walls.
render_mode blend_mix, cull_disabled, depth_draw_never, specular_schlick_ggx;
uniform float speed = 2.6;
float hash(vec2 p){ return fract(sin(dot(p,vec2(127.1,311.7)))*43758.5453); }
float noise(vec2 p){
 vec2 i=floor(p),f=fract(p); f=f*f*(3.0-2.0*f);
 return mix(mix(hash(i),hash(i+vec2(1,0)),f.x),mix(hash(i+vec2(0,1)),hash(i+vec2(1,1)),f.x),f.y);
}
void fragment(){
 vec2 q=vec2(UV.x*18.0,UV.y*4.0-TIME*speed);
 float streak=noise(vec2(q.x,q.y*.4))*.65+noise(q*vec2(2.1,1.3))*.35;
 float foot=smoothstep(.7,1.0,UV.y),lip=1.0-smoothstep(0.0,.12,UV.y);
 float white=clamp(streak*.85+foot*.7-lip*.4,0.0,1.0);
 ALBEDO=mix(vec3(.16,.27,.27),vec3(.86,.9,.9),white);
 ALPHA=clamp(.5+.45*streak+foot*.3,0.0,1.0)*smoothstep(0.0,.1,UV.x)*smoothstep(1.0,.9,UV.x);
 ROUGHNESS=.3; SPECULAR=.35;
}
