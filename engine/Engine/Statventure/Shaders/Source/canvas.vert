#version 460
// Expands each primitive into a quad covering its box (plus room for antialiasing and shadows).
#include "canvas_common.glsl"

layout(location = 0) out vec2 vLocal;
layout(location = 1) flat out uint vIndex;

void main()
{
    Primitive p = pc.primitives.items[gl_InstanceIndex];
    vec2 corner = vec2(gl_VertexIndex & 1, gl_VertexIndex >> 1);

    // Local units per pixel, so the antialiasing margin is about two pixels whatever the scale.
    float scale = max(min(length(p.x0), length(p.x1)), 1e-4);
    float margin = 2.0 / scale;
    vec2 lo = vec2(-margin);
    vec2 hi = p.size + margin;
    if (p.kind == KIND_SHADOW)
    {
        vec2 reach = abs(p.params.z) + 3.0 * shadowBlur(p).xy + margin;
        lo = min(lo, p.params.xy - reach);
        hi = max(hi, p.size + p.params.xy + reach);
    }
    vec2 local = mix(lo, hi, corner);
    vec2 pixel = p.x0 * local.x + p.x1 * local.y + p.x2;
    vLocal = local;
    vIndex = gl_InstanceIndex;
    gl_Position = vec4((pixel - pc.targetOrigin) / pc.targetSize * 2.0 - 1.0, 0.0, 1.0);
}
