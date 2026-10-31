shader_type spatial;
render_mode unshaded, cull_disabled, shadows_disabled, fog_disabled;
// The painted view out of Mio's room, looked up by the direction it is seen in, as a sky is: it holds still behind the shoji
// wherever the camera stands, with its horizon at eye level. The room faces +Z; the painting spans `fov` radians across it,
// wrapped on a cylinder and mirrored past its sides, and past its top and bottom it fades to its own sky and ground, so a glance
// sideways or down from the engawa never stretches an edge into streaks.
uniform sampler2D view : source_color, filter_linear, repeat_disable;
uniform float fov = 1.5;
uniform float horizon = 0.4;
uniform vec3 sky : source_color = vec3(0.72, 0.78, 0.87);
uniform vec3 ground : source_color = vec3(0.27, 0.34, 0.18);
void fragment(){
 vec3 dir=mat3(INV_VIEW_MATRIX)*VERTEX;
 vec2 uv=vec2(0.5-atan(dir.x,dir.z)/fov,horizon-dir.y/length(dir.xz)*1.5/fov);
 vec3 c=texture(view,vec2(abs(mod(uv.x+1.0,2.0)-1.0),clamp(uv.y,0.0,1.0))).rgb;
 ALBEDO=mix(mix(c,sky,smoothstep(0.03,-0.03,uv.y)),ground,smoothstep(0.97,1.03,uv.y));
}
