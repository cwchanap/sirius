shader_type spatial;
render_mode blend_mix, cull_disabled;
varying vec3 world;
void vertex(){ world=(MODEL_MATRIX*vec4(VERTEX,1.0)).xyz; VERTEX.y+=sin(VERTEX.x*1.5+TIME)*.035; }
void fragment(){
 float wave=sin(world.x*1.7+world.z*5.0-TIME*1.8)*sin(world.x*.9-world.z*7.0-TIME*2.1);
 ALBEDO=mix(vec3(.035,.27,.29),vec3(.4,.72,.66),smoothstep(.3,.9,wave));
 ROUGHNESS=.25; METALLIC=.25; ALPHA=.82;
}
