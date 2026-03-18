shader_type canvas_item;
render_mode unshaded;
uniform sampler2D gradient :source_color;
uniform float gradient_mix_amount = 1.0;

vec2 rotateUV(vec2 uv, vec2 pivot, float rotation) {

    float cosa = cos(rotation);	// Get the cos angle for the rotation
    float sina = sin(rotation);	// Get the sin angle for the rotation
	// Offset the pivot point
    uv -= pivot;
	// Calculate the rotation with basic trigonometry
	// https://education.siggraph.org/static/HyperGraph/modeling/mod_tran/2drota.htm
	// "+ pivot" restores the offset to default
    return vec2(cosa * uv.x - sina * uv.y,
				cosa * uv.y + sina * uv.x) + pivot;
}

void fragment() {
	// Where the values vec3(0.299, 0.587, 0.114) comes from
	// https://stackoverflow.com/questions/687261/converting-rgb-to-grayscale-intensity/689547#689547

	// Rotate the gradient 90 degrees, around the center (0.5,0.5)
	vec2 myUv = rotateUV(UV,vec2(0.5,0.5), radians(90));
	vec3 gradientTex = texture(gradient, myUv).rgb;
	// Convert color into greyscale value, using the predefied RGB to grayscale values
	float greyscale_value = dot(gradientTex.rgb, vec3(0.299, 0.587, 0.114));
	// Here we sample the rgb color from the gradient texture.
	// The gradient texture is 1 pixel high, thereof we only sample on the X-axis,
	// and leave the Y-axis at 0.0. vec2(X-value,Y-value)) -> vec2(greyscale_value, 0.0)
	vec3 sampled_color = texture(gradient, vec2(greyscale_value, 0.0)).rgb;

	// Mix the gradient texture color, with the sampled color using the gradient mix amount
	COLOR.rgb = mix(gradientTex.rgb, sampled_color, gradient_mix_amount);
	// Get the pixel alpha value from the texture
	vec4 pixelColor = texture(TEXTURE,UV);
	// Preserve the alpha value of the texture
	COLOR.a = pixelColor.a;
}