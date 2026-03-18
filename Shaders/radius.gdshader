shader_type canvas_item;

// 圆角半径（0-1范围，基于最短边）
uniform float corner_radius : hint_range(0.0, 1) = 0.1;
// 边框颜色（可选）
uniform vec4 border_color : source_color = vec4(1.0);
// 边框宽度（0-1范围）
uniform float border_width : hint_range(0.0, 0.2) = 0.02;
// 抗锯齿宽度
uniform float aa_width : hint_range(0.001, 0.1) = 0.005;

void fragment() {
    // 获取标准化坐标（0-1范围）
    vec2 uv = UV;

    // 计算宽高比例
    float aspect = 1.0/SCREEN_PIXEL_SIZE.x * SCREEN_PIXEL_SIZE.y;
    vec2 size = vec2(1.0, 1.0/aspect);

    // 计算实际半径（基于最短边）
    float radius = corner_radius * min(size.x, size.y);

    // 计算到四个角的距离
    vec2 q = abs(uv - 0.5) * size;
    vec2 corner = size * 0.5 - radius;

    // 计算圆角区域
    float dist = length(max(q - corner, 0.0)) - radius;

    // 抗锯齿计算
    float alpha = smoothstep(-aa_width, aa_width, -dist);

    // 边框计算
    float border_alpha = smoothstep(
        -aa_width,
        aa_width,
        abs(dist + border_width * 0.5) - border_width * 0.5
    );

    // 最终颜色混合
    vec4 final_color = mix(
        border_color * COLOR,
        COLOR,
        border_alpha
    );

    COLOR = final_color * vec4(1.0, 1.0, 1.0, alpha);
}