#version 460
// Particles (Docs/Renderer-3D.md, "Particles"): each particle a quad in the view plane about its
// centre, along its axes (turned by its angle, or along its motion for a spark), six vertices a
// particle pulled by gl_VertexIndex; the fragment gets its place on the quad in metres.
#include "common.glsl"
#include "particle_types.glsl"

layout(location = 0) flat out uint vIndex;
layout(location = 1) out vec2 vLocal;

const vec2 CORNERS[6] = vec2[](vec2(-1.0, -1.0), vec2(1.0, -1.0), vec2(1.0, 1.0), vec2(-1.0, -1.0), vec2(1.0, 1.0), vec2(-1.0, 1.0));

void main()
{
    uint index = uint(gl_VertexIndex) / 6u;
    ParticleQuad q = pc.quads.items[index];
    vec2 local = CORNERS[gl_VertexIndex % 6] * vec2(q.halfU, q.halfV);
    vIndex = index;
    vLocal = local;
    gl_Position = pc.scene.viewProjection * vec4(q.centre + q.axisU * local.x + q.axisV * local.y, 1.0);
}
