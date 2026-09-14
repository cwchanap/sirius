shader_type spatial;
render_mode diffuse_burley;
uniform sampler2D atlas : source_color, filter_linear_mipmap_anisotropic, repeat_enable;
uniform vec2 cell = vec2(0.0,1.0);
uniform vec2 repeats = vec2(2.0);
uniform vec4 tint : source_color = vec4(1.0);
void fragment(){ ALBEDO=texture(atlas,(fract(UV*repeats)*.97+.015+cell)*.5).rgb*tint.rgb; ROUGHNESS=.88; }
