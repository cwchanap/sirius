shader_type spatial;
render_mode cull_back, diffuse_burley;
uniform sampler2D atlas : source_color, filter_linear_mipmap_anisotropic, repeat_enable;
// The paddies' aze are earth banks however steep, so their material turns the slope's stone off.
uniform float rocks = 1.0;
varying vec3 world;
varying vec3 terrain_normal;
void vertex() { world=(MODEL_MATRIX*vec4(VERTEX,1.0)).xyz; terrain_normal=MODEL_NORMAL_MATRIX*NORMAL; }
// The gradients come from the unwrapped uv, so the fract() seam at each tile's edge does not pick the smallest mip and draw a grid line.
// Past mip 3 a texel is wider than the 1.5% margin round each atlas cell and would bleed its neighbour in along every tile edge, so
// the sample stops at mip 3 and fades into the cell's own mean colour by mip 5.
vec3 tile(vec2 uv, vec2 cell, vec2 dx, vec2 dy) {
    vec2 gx=dx*0.485,gy=dy*0.485;
    float lod=log2(max(max(length(gx),length(gy))*float(textureSize(atlas,0).x),1e-6)),k=exp2(min(lod,3.0)-lod);
    vec3 near=textureGrad(atlas,(fract(uv)*0.97+0.015+cell)*0.5,gx*k,gy*k).rgb;
    return mix(near,textureLod(atlas,(cell+0.5)*0.5,6.0).rgb,smoothstep(3.0,5.0,lod));
}
void fragment() {
    vec2 p=world.xz;
    float road=sin(p.y*0.068)*2.4;
    road=mix(road,10.0*sin((-p.y-72.0)*0.045),smoothstep(0.0,12.0,-p.y-72.0));
    road*=1.0-smoothstep(0.0,12.0,-p.y-134.0);
    // Vertex green is 0 beyond the play area, where the road's curve would otherwise carry on over the beach.
    float path=(1.0-smoothstep(2.0,3.3,abs(p.x-road)))*COLOR.g;
    float plaza=(1.0-smoothstep(7.0,11.0,length(p-vec2(0.0,21.0))));
    plaza=max(plaza,1.0-smoothstep(5.0,8.0,length(p-vec2(-2.0,-155.0))));
    vec2 dx=dFdx(p),dy=dFdy(p);
    vec3 wx=dFdx(world),wy=dFdy(world);
    vec3 n=normalize(terrain_normal);
    float rocky=smoothstep(0.22,0.5,1.0-n.y)*rocks;
    vec3 grass=tile(p*0.16,vec2(0.0),dx*0.16,dy*0.16);
    vec3 earth=tile(p*0.22,vec2(1.0,0.0),dx*0.22,dy*0.22);
    // Rock is projected from whichever side faces it, so cliffs and gorge walls are not streaked by a top-down projection.
    vec3 stone=vec3(0.0);
    if(rocky>0.0){
        vec3 w=pow(abs(n),vec3(4.0));w/=w.x+w.y+w.z;
        stone=tile(world.xz*0.19,vec2(0.0,1.0),wx.xz*0.19,wy.xz*0.19)*w.y+tile(world.xy*0.19,vec2(0.0,1.0),wx.xy*0.19,wy.xy*0.19)*w.z
            +tile(world.zy*0.19,vec2(0.0,1.0),wx.zy*0.19,wy.zy*0.19)*w.x;
    }
    float mottling=0.9+0.1*sin(p.x*.27)*sin(p.y*.31);
    ALBEDO=mix(mix(grass*vec3(1.05,1.2,.94)*mottling,earth,max(path,plaza)*.93),stone,rocky*.8);
    // Vertex red is dryness, 1 - World.Shore(): 0 at the river, brook and Cedar River waterlines, back to 1 a little up their banks. Wet earth, then gravel by the water.
    float wet=1.0-COLOR.r;
    vec3 gravel=mix(earth*vec3(.42,.4,.36),tile(p*1.1,vec2(0.0,1.0),dx*1.1,dy*1.1)*vec3(.6,.6,.56),.3+.4*smoothstep(.45,.95,wet));
    ALBEDO=mix(ALBEDO,gravel,smoothstep(0.0,.55,wet));
    ROUGHNESS=mix(.95,.45,wet*wet);
}
