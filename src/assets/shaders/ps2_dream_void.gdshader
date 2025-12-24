shader_type spatial;
render_mode unshaded, depth_draw_never, cull_back, blend_mix;

uniform float global_speed : hint_range(0.1, 2.0) = 0.5;

uniform vec3 nebula_color_1 : source_color = vec3(0.1, 0.7, 1.0);
uniform vec3 nebula_color_2 : source_color = vec3(0.05, 0.2, 0.6);
uniform float nebula_intensity : hint_range(0.0, 4.0) = 1.6;
uniform float nebula_speed : hint_range(0.1, 4.0) = 0.7;
uniform float nebula_scale : hint_range(0.5, 8.0) = 3.0;
uniform float nebula_roundness : hint_range(0.3, 2.0) = 1.0;
uniform float nebula_swirl : hint_range(0.0, 2.0) = 0.9;
uniform vec3 base_void_color : source_color = vec3(0.005, 0.005, 0.015);

uniform vec3 orb_color_1 : source_color = vec3(1.0, 0.3, 0.1);
uniform vec3 orb_color_2 : source_color = vec3(0.1, 1.0, 0.6);
uniform vec3 orb_color_3 : source_color = vec3(0.8, 0.2, 1.0);
uniform vec3 orb_color_4 : source_color = vec3(0.2, 0.6, 1.0);
uniform int orb_count : hint_range(1, 16) = 8;
uniform float orb_brightness : hint_range(1.0, 30.0) = 18.0;
uniform float orb_size : hint_range(0.001, 0.25) = 0.025;
uniform float tail_length : hint_range(0.1, 2.0) = 1.2;
uniform float tail_fade : hint_range(0.3, 3.0) = 1.5;
uniform float orb_radius : hint_range(0.5, 1.5) = 1.1;
uniform float orb_lens_size : hint_range(0.0, 10.0) = 5.0;
uniform float orb_lens_opacity : hint_range(0.0, 1.0) = 1.0;

uniform float vignette_strength : hint_range(0.0,1.0) = 0.55;
uniform float grain_amount : hint_range(0.0,0.1) = 0.018;
uniform float scanline_strength : hint_range(0.0,0.3) = 0.08;
uniform float chromatic_aberration : hint_range(0.0,0.005) = 0.0018;

// new line
uniform sampler2D SCREEN_TEXTURE : hint_screen_texture, filter_linear_mipmap;

// NOIZE 
float hash33(vec3 p){p=fract(p*0.1031);p+=dot(p,p.yzx+33.33);return fract((p.x+p.y)*p.z);}
float noise(vec3 x){
    vec3 i=floor(x),f=fract(x);f=f*f*(3.0-2.0*f);
    float a=hash33(i),b=hash33(i+vec3(1,0,0)),c=hash33(i+vec3(0,1,0)),d=hash33(i+vec3(1,1,0));
    float e=hash33(i+vec3(0,0,1)),ff=hash33(i+vec3(1,0,1)),g=hash33(i+vec3(0,1,1)),h=hash33(i+vec3(1,1,1));
    return mix(mix(mix(a,b,f.x),mix(c,d,f.x),f.y),mix(mix(e,ff,f.x),mix(g,h,f.x),f.y),f.z);
}
float fbm(vec3 p){float v=0.0,a=0.5;for(int i=0;i<6;i++){v+=a*noise(p);p*=2.02;a*=0.5;}return v;}

// lens flare thang
float lens_star(vec2 uv, vec2 pos, float size){
    vec2 dir = uv - pos;
    float dist = length(dir);
    if(dist > size*0.2) return 0.0;
    float angle = atan(dir.y, dir.x);
    float star = pow(abs(sin(angle*18.0)),70.0);
    float core = exp(-dist*25.0/size);
    return (star*0.9 + core*0.1)*size*0.8;
}

// ORB + TAIL + LENS
vec3 orb_with_tail_and_lens(vec2 uv, int idx){
    float t = TIME*global_speed*0.3 + float(idx)*7.831;
    float golden = 2.39996;
    float angle = float(idx)*golden;
    float radius = sqrt(float(idx)+0.5)/sqrt(float(orb_count))*orb_radius;
    vec2 orbit_center = vec2(cos(angle),sin(angle))*radius;
    vec2 pos = orbit_center + vec2(sin(t*0.77+float(idx)),cos(t*0.61+float(idx)*1.1))*0.25;

    vec3 col = (idx%4==0)?orb_color_1:(idx%4==1)?orb_color_2:(idx%4==2)?orb_color_3:orb_color_4;

    float head = smoothstep(orb_size,0.0,length(uv-pos))*12.0;

    vec3 tail=vec3(0.0);
    const int steps=26;
    for(int i=1;i<=steps;i++){
        float prog=float(i)/float(steps);
        float past=t-prog*tail_length*0.38;
        vec2 past_pos=orbit_center+vec2(sin(past*0.77+float(idx)),cos(past*0.61+float(idx)*1.1))*0.25;
        float w=orb_size*mix(1.6,0.08,prog);
        tail += smoothstep(w,w*0.35,length(uv-past_pos))*pow(1.0-prog,tail_fade);
    }

    float lens = lens_star(uv,pos,orb_lens_size*0.1)*orb_lens_opacity;

    float pulse = 0.6 + 0.4*sin(t*2.1 + float(idx));
    return col*(head*2.5 + tail*0.85 + lens*2.0)*pulse*orb_brightness;
}

void fragment(){
    vec2 uv=UV*2.0-1.0;
    uv.x *= VIEWPORT_SIZE.x/VIEWPORT_SIZE.y;

    vec3 col = base_void_color;

    // nebula
    vec2 p=uv*nebula_scale;
    p.y*=nebula_roundness;
    float ang=length(p)*2.5 + TIME*nebula_speed*0.12;
    p+=vec2(cos(ang),sin(ang))*nebula_swirl*0.12;

    col += nebula_color_1 * fbm(vec3(p,TIME*nebula_speed*0.22)) * nebula_intensity;
    col += nebula_color_2 * fbm(vec3(p*1.7,0.0)+vec3(4,9,TIME*nebula_speed*-0.18)) * nebula_intensity*0.7;

    // orbs
    for(int i=0;i<16;i++){
        if(i>=orb_count) break;
        col += orb_with_tail_and_lens(uv,i);
    }

    // typical post processing fx
    float vig = smoothstep(1.4, 0.0, length(uv));
    col = mix(col * (1.0 - vignette_strength), col, vig);

    float grain = fract(sin(dot(uv + TIME*123.45, vec2(12.9898,78.233))) * 43758.5453);
    col += grain * grain_amount;

    float scan = sin(uv.y * VIEWPORT_SIZE.y * 1.8) * scanline_strength;
    col -= scan;

    vec2 ca_offset = uv * chromatic_aberration;
    vec3 ca = vec3(
        texture(SCREEN_TEXTURE, SCREEN_UV + ca_offset).r,
        texture(SCREEN_TEXTURE, SCREEN_UV).g,
        texture(SCREEN_TEXTURE, SCREEN_UV - ca_offset).b
    );
    col = mix(col, ca, 0.35);

    ALBEDO = col;
}