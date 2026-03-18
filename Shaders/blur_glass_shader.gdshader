shader_type canvas_item;

uniform float blur_radius : hint_range(0.0, 20.0) = 5.0;
uniform float mix_amount : hint_range(0.0, 1.0) = 0.5;
uniform vec4 color_over : source_color = vec4(1.0, 1.0, 1.0, 1.0);
uniform sampler2D SCREEN_TEXTURE : hint_screen_texture, filter_linear;

const float gaussian_weights[9] = float[](
    0.0162, 0.0540, 0.1216, 0.1945, 0.2270, 0.1945, 0.1216, 0.0540, 0.0162
);

void fragment() {
    vec2 tex_size = 1.0/SCREEN_PIXEL_SIZE;
    float pixel_size = blur_radius/tex_size.x;
    
    vec4 blurred = vec4(0.0);
    
    for (int i = -4; i <= 4; i++) {
        vec2 offset = vec2(float(i) * pixel_size, 0.0);
        blurred += texture(SCREEN_TEXTURE, SCREEN_UV + offset) * gaussian_weights[i + 4];
    }
    
    vec4 blurred_final = vec4(0.0);
    for (int i = -4; i <= 4; i++) {
        vec2 offset = vec2(0.0, float(i) * pixel_size);
        vec2 sample_uv = SCREEN_UV + offset;
        
        vec4 temp = vec4(0.0);
        for (int j = -4; j <= 4; j++) {
            vec2 h_offset = vec2(float(j) * pixel_size, 0.0);
            temp += texture(SCREEN_TEXTURE, sample_uv + h_offset) * gaussian_weights[j + 4];
        }
        
        blurred_final += temp * gaussian_weights[i + 4];
    }
    
    blurred_final.a = 1.0;
    
    vec4 fin = mix(blurred_final, color_over, mix_amount);
    COLOR = fin;
}
