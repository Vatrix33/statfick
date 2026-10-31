#version 460
// Glow, pass 1 for particles (Docs/Renderer-3D.md, "Glow"): where a glowing particle covers at least
// half the pixel, it owns the halo there: the particles' model id (1023, which no model has) and
// its view depth.
#include "common.glsl"
#include "particle_types.glsl"

layout(location = 0) flat in uint vIndex;
layout(location = 1) in vec2 vLocal;
layout(location = 0) out uvec2 outOwner;

void main()
{
    ParticleQuad q = pc.quads.items[vIndex];
    float fade;
    if (shapeCoverage(shapeDistance(q, vLocal, fade) / q.pixel) * fade * q.opacity < 0.5)
        discard;
    outOwner = uvec2(1023u, floatBitsToUint(q.depth));
}
