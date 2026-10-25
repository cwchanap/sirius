shader_type spatial;
// Additive light for combat effects: tint times energy (above 1 it blooms), shaped by the mask texture and the vertex or
// particle colour, and faded by fade. A billboard turns to face the camera and keeps its scale; a mesh given its height fades
// from its foot to nothing at its top, and rim thins its middle so a column reads by its edges.
render_mode unshaded, blend_add, cull_disabled, depth_draw_never, shadows_disabled;
uniform vec4 tint : source_color = vec4(1.0);
uniform float energy = 1.0;
uniform float fade = 1.0;
uniform bool billboard = false;
uniform float height = 0.0;
uniform float rim = 0.0;
uniform sampler2D mask : source_color, hint_default_white;
varying float up;
void vertex(){
 up=height>0.0?VERTEX.y/height+.5:0.0;
 if(billboard){
  MODELVIEW_MATRIX=VIEW_MATRIX*mat4(INV_VIEW_MATRIX[0],INV_VIEW_MATRIX[1],INV_VIEW_MATRIX[2],MODEL_MATRIX[3]);
  MODELVIEW_MATRIX=MODELVIEW_MATRIX*mat4(vec4(length(MODEL_MATRIX[0].xyz),0.0,0.0,0.0),vec4(0.0,length(MODEL_MATRIX[1].xyz),0.0,0.0),vec4(0.0,0.0,length(MODEL_MATRIX[2].xyz),0.0),vec4(0.0,0.0,0.0,1.0));
 }
}
void fragment(){
 vec4 m=texture(mask,UV);
 ALBEDO=tint.rgb*COLOR.rgb*m.rgb*energy;
 ALPHA=clamp(fade*COLOR.a*m.a*(1.0-up)*(1.0-up)*mix(1.0,pow(1.0-abs(dot(NORMAL,VIEW)),1.5),rim),0.0,1.0);
}
