shader_type spatial;
// The sword trail. UV.x is a sample's age over the trail's life (0 new), UV.y runs across the ribbon from its inner end (0)
// to the blade's tip (1). The edge the tip draws burns white while fresh; the inner end and the old samples fade out.
render_mode unshaded, blend_add, cull_disabled, depth_draw_never, shadows_disabled;
uniform vec4 tint : source_color = vec4(1.0);
uniform float energy = 2.5;
void fragment(){
 float fresh=1.0-UV.x;
 float edge=smoothstep(.72,1.0,UV.y)*fresh;
 ALBEDO=mix(tint.rgb,vec3(1.0),edge*.75)*energy;
 ALPHA=clamp(fresh*fresh*(UV.y*UV.y*.5+edge*.8),0.0,1.0);
}
