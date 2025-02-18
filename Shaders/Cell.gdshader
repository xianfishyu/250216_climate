shader_type spatial;
uniform vec3 color = vec3(1,1,1);
void vertex() {
	
}

void fragment() {
	ALBEDO = color;
}

//void light() {
	// Called for every pixel for every light affecting the material.
	// Uncomment to replace the default light processing function with this one.
//}
