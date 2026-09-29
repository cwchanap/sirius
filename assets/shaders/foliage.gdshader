shader_type spatial;
render_mode cull_disabled, diffuse_burley;
uniform sampler2D atlas : source_color, filter_linear_mipmap, repeat_disable;
instance uniform vec4 tint : source_color = vec4(1.0);
void vertex(){
    vec3 w=(MODEL_MATRIX*vec4(VERTEX,1.0)).xyz;
    VERTEX.xz+=COLOR.r*.12*vec2(sin(TIME*1.3+w.x*.35+w.z*.2),cos(TIME*1.1+w.z*.3));
}
void fragment(){
    vec4 t=texture(atlas,UV);
    ALBEDO=t.rgb*tint.rgb*COLOR.g; ALPHA=t.a; ALPHA_SCISSOR_THRESHOLD=.45;
    BACKLIGHT=t.rgb*.35; ROUGHNESS=.9;
}
