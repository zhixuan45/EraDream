shader_type canvas_item;

uniform vec4 color : source_color = vec4(1.0);
uniform float gradient_position : hint_range(0.0, 1.0) = 0.5;
// 0=左到右, 1=右到左, 2=上到下, 3=下到上
uniform int gradient_direction : hint_range(0, 3) = 0;

void fragment() {
    float gradient_value = 0.0;

    // 根据方向计算渐变值
    if (gradient_direction == 0) { // 左到右
        gradient_value = 1.0 - smoothstep(0.0, gradient_position, UV.x);
    } else if (gradient_direction == 1) { // 右到左
        gradient_value = 1.0 - smoothstep(0.0, gradient_position, 1.0 - UV.x);
    } else if (gradient_direction == 2) { // 上到下
        gradient_value = 1.0 - smoothstep(0.0, gradient_position, UV.y);
    } else { // 下到上 (gradient_direction == 3)
        gradient_value = 1.0 - smoothstep(0.0, gradient_position, 1.0 - UV.y);
    }

    COLOR = vec4(color.rgb, color.a * gradient_value) * COLOR;
}