shader_type spatial;
render_mode cull_disabled, diffuse_burley;
// COLOR.rgb is the blade's linear colour, COLOR.a its sway weight: 0 at the root, 1 at the tip.
void vertex(){
    vec3 w=(MODEL_MATRIX*vec4(VERTEX,1.0)).xyz;
    float gust=sin(TIME*1.6+w.x*.35+w.z*.22)*.7+sin(TIME*3.1+w.x*1.7-w.z*1.1)*.3;
    VERTEX.xz+=COLOR.a*COLOR.a*vec2(.07*gust+.03,.04*cos(TIME*1.2+w.z*.4));
}
void fragment(){
    // Keep the authored, mostly upward normal on both sides, so blades shade like the lawn they stand in.
    if(!FRONT_FACING)NORMAL=-NORMAL;
    ALBEDO=COLOR.rgb; ROUGHNESS=.85; BACKLIGHT=COLOR.rgb*.5;
}
