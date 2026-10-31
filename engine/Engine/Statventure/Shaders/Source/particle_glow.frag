#version 460
// Glow, pass 1 for particles (Docs/Renderer-3D.md, "Glow"): a glowing particle's shape in its colour
// (no ink), sRGB-encoded as the glow's source holds colours, premultiplied by its coverage and
// opacity.
#include "common.glsl"
#include "particle_types.glsl"

layout(location = 0) flat in uint vIndex;
layout(location = 1) in vec2 vLocal;
layout(location = 0) out vec4 outGlow;

float encode(float c)
{
    return c <= 0.0031308 ? c * 12.92 : 1.055 * pow(c, 1.0 / 2.4) - 0.055;
}

void main()
{
    ParticleQuad q = pc.quads.items[vIndex];
    float fade;
    float shape = shapeCoverage(shapeDistance(q, vLocal, fade) / q.pixel) * fade * q.opacity;
    vec3 c = clamp(q.color.rgb, 0.0, 1.0);
    outGlow = vec4(vec3(encode(c.r), encode(c.g), encode(c.b)) * shape, shape);
}
