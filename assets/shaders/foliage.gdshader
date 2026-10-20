shader_type spatial;
// Crowns cast shadows but do not receive them: self-shadowing cards speckle with acne and block the blossoms' transmitted light,
// while the crown's ellipsoid normals and baked occlusion (tools/build_props.py) already shade it as a volume.
render_mode cull_disabled, diffuse_burley, shadows_disabled;
uniform sampler2D atlas : source_color, filter_linear_mipmap, repeat_disable;
instance uniform vec4 tint : source_color = vec4(1.0);
void vertex(){
    vec3 w=(MODEL_MATRIX*vec4(VERTEX,1.0)).xyz;
    VERTEX.xz+=COLOR.r*.12*vec2(sin(TIME*1.3+w.x*.35+w.z*.2),cos(TIME*1.1+w.z*.3));
}
void fragment(){
    // cull_disabled flips NORMAL on back faces; undo it so both sides of a card keep the crown's spherised normal.
    if(!FRONT_FACING)NORMAL=-NORMAL;
    vec4 t=texture(atlas,UV);
    // Cards seen edge-on smear their sprite into streaks: by the card's real facing, not the spherised normal, the scissor erodes them away.
    float facing=abs(dot(normalize(cross(dFdx(VERTEX),dFdy(VERTEX))),VIEW));
    ALBEDO=t.rgb*tint.rgb*COLOR.g; ALPHA=t.a*smoothstep(.12,.4,facing); ALPHA_SCISSOR_THRESHOLD=.4;
    BACKLIGHT=t.rgb*.35; ROUGHNESS=.9; SPECULAR=.1;
}
