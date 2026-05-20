shader_type canvas_item;

void fragment() {
    vec4 col = texture(TEXTURE, UV);
    // Perceptual brightness: discard near-white pixels (sprite background)
    float lum = col.r * 0.299 + col.g * 0.587 + col.b * 0.114;
    if (lum > 0.88 && col.a > 0.01) {
        col.a = 0.0;
    }
    COLOR = col;
}
