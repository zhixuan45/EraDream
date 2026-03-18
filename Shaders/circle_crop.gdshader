shader_type canvas_item;

// 可以在检查器中调整圆的大小 (0.0 到 1.0, 0.5 代表填满边框)
uniform float radius : hint_range(0.0, 1.0) = 0.5;

// 边缘柔化程度 (抗锯齿)，数值越大边缘越模糊
uniform float smoothing : hint_range(0.0, 1.0) = 0.01;

void fragment() {
	// 获取当前像素的颜色
	vec4 color = texture(TEXTURE, UV);
	
	// 计算当前 UV 坐标到中心点 (0.5, 0.5) 的距离
	float dist = distance(UV, vec2(0.5));
	
	// 使用 smoothstep 进行平滑裁剪
	// 如果距离小于 radius，alpha 为 1
	// 如果距离在 radius 和 radius + smoothing 之间，alpha 平滑过渡到 0
	float alpha = smoothstep(radius, radius - smoothing, dist);
	
	// 应用新的透明度
	color.a *= alpha;
	
	// 输出最终颜色
	COLOR = color;
}