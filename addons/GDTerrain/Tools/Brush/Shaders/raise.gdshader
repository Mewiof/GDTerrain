shader_type canvas_item;
render_mode blend_disabled;

uniform sampler2D p_src_texture;
uniform vec4 p_src_rect;
uniform float p_opacity = 1.0;
uniform float p_factor = 1.0;

vec2 get_src_uv(vec2 screen_uv) {
	vec2 uv = p_src_rect.xy + screen_uv * p_src_rect.zw;
	return uv;
}

void fragment() {
	float brush_value = p_factor * p_opacity * texture(TEXTURE, UV).r;
	
	float src_h = texture(p_src_texture, get_src_uv(SCREEN_UV)).r;
	float h = src_h + brush_value;
	COLOR = vec4(h, 0.0, 0.0, 1.0);
}
