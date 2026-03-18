shader_type canvas_item;

uniform vec4 center_color : source_color = vec4(1.0);
uniform vec4 edge_color : source_color = vec4(0.0);
uniform float gradient_power = 1.0;
uniform float gradient_offset = 0.0;

void fragment() {
    // 计算距离并应用幂函数和偏移
    float gradient = pow(length(UV - vec2(0.5)), gradient_power);
    gradient = clamp(gradient, 0.0, 1.0);

    // 应用颜色混合和modulate
    COLOR = mix(center_color, edge_color, gradient);
}