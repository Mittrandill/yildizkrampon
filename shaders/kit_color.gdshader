shader_type canvas_item;

// Team jersey colour — defaults to red (home kit).
// Set via ShaderMaterial.SetShaderParameter("kit_color", teamColor).
uniform vec4 kit_color : source_color = vec4(0.86, 0.18, 0.12, 1.0);

void fragment() {
    vec4 col = texture(TEXTURE, UV);

    // ── 1. Discard white / near-white sprite background ───────────────────────
    float lum    = col.r * 0.299 + col.g * 0.587 + col.b * 0.114;
    float maxCh  = max(col.r, max(col.g, col.b));
    float minCh  = min(col.r, min(col.g, col.b));
    float chroma = maxCh - minCh;   // 0 = achromatic (grey/white), >0.15 = clearly coloured

    // Primary pass: bright + low-saturation → background white / off-white / light grey.
    // chroma < 0.15 keeps coloured pixels (skin, jersey, hair) even when fairly bright.
    if (lum > 0.80 && chroma < 0.15 && col.a > 0.01) {
        COLOR = vec4(0.0);
        return;
    }
    // Safety pass: catch any remaining very-bright pixels from AI-generated backgrounds.
    if (lum > 0.91 && col.a > 0.01) {
        COLOR = vec4(0.0);
        return;
    }

    // ── 2. Detect red jersey pixels and recolor ──────────────────────────────
    // A jersey pixel has a large red channel that clearly dominates green+blue.
    // Skin pixels have a warm tone but much less red dominance (red_over < 0.38).
    float red_over = col.r - max(col.g, col.b);
    if (col.r > 0.40 && red_over > 0.40 && col.a > 0.05) {
        // Map the red channel brightness onto the kit colour.
        COLOR = vec4(kit_color.rgb * col.r, col.a);
        return;
    }

    COLOR = col;
}
