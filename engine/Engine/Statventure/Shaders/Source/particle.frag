#version 460
// Particles (Docs/Renderer-3D.md, "Particles"): the shape in its colour with its ink line centred
// on its edge (the effect's width in pixels), both times its opacity (and a puff's fade), as
// premultiplied linear colour for the particle layer.
#include "common.glsl"
#include "particle_types.glsl"

layout(location = 0) flat in uint vIndex;
layout(location = 1) in vec2 vLocal;
layout(location = 0) out vec4 outColor;

void main()
{
    ParticleQuad q = pc.quads.items[vIndex];
    float fade;
    float d = shapeDistance(q, vLocal, fade) / q.pixel;
    float shape = shapeCoverage(d) * fade * q.opacity;
    float line = q.outline > 0.0 ? clamp(0.5 * q.outline - abs(d) + 0.5, 0.0, 1.0) * q.opacity : 0.0;
    outColor = vec4(pc.scene.ink.rgb * line + q.color.rgb * shape * (1.0 - line), line + shape * (1.0 - line));
}
