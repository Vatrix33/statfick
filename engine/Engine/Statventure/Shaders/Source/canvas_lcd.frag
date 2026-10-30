#version 460
// LCD (subpixel) text: each colour channel has its own coverage, so the result is blended per
// channel with dual-source blending: destination * (1 - outBlend) + outColor.
#include "canvas_fragment.glsl"

layout(location = 0) in vec2 vLocal;
layout(location = 1) flat in uint vIndex;
layout(location = 0, index = 0) out vec4 outColor;
layout(location = 0, index = 1) out vec4 outBlend;

void main()
{
    Primitive p = pc.primitives.items[vIndex];
    vec2 t = clamp(vLocal / max(p.size, vec2(1e-4)), 0.0, 1.0);
    vec4 mask = sampleTexture(p.textureIndex, p.samplerIndex, mix(p.uv.xy, p.uv.zw, t));
    vec3 cov = (p.flags & FLAG_LCD32) != 0u ? min(mask.rgb * (255.0 / 32.0), vec3(1.0)) : mask.rgb;
    // The quad has an antialiasing margin; glyph masks end at their box.
    if (any(lessThan(vLocal, vec2(0.0))) || any(greaterThan(vLocal, p.size)))
        cov = vec3(0.0);
    cov *= clipCoverage(gl_FragCoord.xy + pc.targetOrigin, p.clip, p.clipRadii);
    float alpha = max(cov.r, max(cov.g, cov.b));
    outColor = vec4(p.color.rgb * cov, p.color.a * alpha);
    outBlend = vec4(p.color.a * cov, p.color.a * alpha);
}
