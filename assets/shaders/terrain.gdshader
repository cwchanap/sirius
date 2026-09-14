shader_type spatial;
render_mode cull_back, diffuse_burley;
uniform sampler2D atlas : source_color, filter_linear_mipmap_anisotropic, repeat_enable;
varying vec3 world;
varying vec3 terrain_normal;
void vertex() { world=(MODEL_MATRIX*vec4(VERTEX,1.0)).xyz; terrain_normal=MODEL_NORMAL_MATRIX*NORMAL; }
vec3 tile(vec2 uv, vec2 cell) { return texture(atlas,(fract(uv)*0.97+0.015+cell)*0.5).rgb; }
void fragment() {
    vec2 p=world.xz;
    float road=sin(p.y*0.068)*2.4;
    road=mix(road,10.0*sin((-p.y-72.0)*0.045),smoothstep(0.0,12.0,-p.y-72.0));
    road*=1.0-smoothstep(0.0,12.0,-p.y-134.0);
    float path=1.0-smoothstep(2.0,3.3,abs(p.x-road));
    float plaza=(1.0-smoothstep(7.0,11.0,length(p-vec2(0.0,21.0))));
    plaza=max(plaza,1.0-smoothstep(5.0,8.0,length(p-vec2(-2.0,-155.0))));
    float rocky=smoothstep(0.22,0.5,1.0-normalize(terrain_normal).y);
    vec3 grass=tile(p*0.16,vec2(0.0));
    vec3 earth=tile(p*0.22,vec2(1.0,0.0));
    vec3 stone=tile(p*0.19,vec2(0.0,1.0));
    float mottling=0.9+0.1*sin(p.x*.27)*sin(p.y*.31);
    ALBEDO=mix(mix(grass*vec3(1.05,1.2,.94)*mottling,earth,max(path,plaza)*.93),stone,rocky*.8);
    ROUGHNESS=.95;
}
