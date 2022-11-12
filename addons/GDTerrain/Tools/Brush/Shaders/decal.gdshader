shader_type spatial;
render_mode unshaded;

uniform sampler2D p_map_height;
uniform mat4 p_terrain_inv_transform;
uniform mat3 p_terrain_normal_basis;

void vertex() {
	vec2 cell_coords = (p_terrain_inv_transform * MODEL_MATRIX * vec4(VERTEX, 1)).xz;
	
	vec2 p = vec2(1.0) / vec2(textureSize(p_map_height, 0));
	vec2 uv = p * cell_coords;
	
	float k = 1.0;
	float left = texture(p_map_height, uv + vec2(-p.x, 0)).r * k;
	float right = texture(p_map_height, uv + vec2(p.x, 0)).r * k;
	float back = texture(p_map_height, uv + vec2(0, -p.y)).r * k;
	float fore = texture(p_map_height, uv + vec2(0, p.y)).r * k;
	vec3 n = normalize(vec3(left - right, 2.0, back - fore));
	
	n *= p_terrain_normal_basis;
	
	float h = texture(p_map_height, uv).r;
	VERTEX.y = h;
	VERTEX += 1.0 * n;
	NORMAL = n;
}

void fragment() {
	float len = length((2.0 * UV) - 1.0);
	float g = clamp(1.0 - (15.0 * abs(0.9 - len)), 0.0, 1.0);
	ALBEDO = vec3(1.0, 0.25, 0.25);
	ALPHA = g;
}
