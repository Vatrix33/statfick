// Particles (Docs/Renderer-3D.md, "Particles"): a particle as drawn (ParticleQuad in Particles.cs),
// what a particle draw pushes, and its shape. shape: 0 disc, 1 ring, 2 spark, 3 puff, 4 shard; p0,
// p1: the shape's measures in metres (a disc's radius; a ring's outer and inner radius; a puff's
// radius and where its fade starts; a spark's half length between its round ends and its radius; a
// shard's half length and half width); pixel: a pixel's width at the particle (metres); outline: its
// ink line's width (pixels); color: linear.
#include "mesh_types.glsl"

struct ParticleQuad
{
    vec3 centre; float pixel;
    vec3 axisU; float halfU;
    vec3 axisV; float halfV;
    vec4 color;
    uint shape; float p0; float p1; float outline;
    float opacity; float depth; uint glow; uint pad0;
};
layout(buffer_reference, scalar) readonly buffer ParticleQuads { ParticleQuad items[]; };
layout(push_constant, scalar) uniform ParticlePush { ParticleQuads quads; MeshScene scene; } pc;

// A rhombus's signed distance (half length b.x along x, half width b.y; Inigo Quilez's sdRhombus).
float rhombusDistance(vec2 p, vec2 b)
{
    p = abs(p);
    float h = clamp(((b.x - 2.0 * p.x) * b.x - (b.y - 2.0 * p.y) * b.y) / dot(b, b), -1.0, 1.0);
    float d = length(p - 0.5 * b * vec2(1.0 - h, 1.0 + h));
    return d * sign(p.x * b.y + p.y * b.x - b.x * b.y);
}

// The signed distance (metres, negative inside) from the shape's edge at p (metres along the quad's
// axes from its centre), and the puff's fade there (1 elsewhere).
float shapeDistance(ParticleQuad q, vec2 p, out float fade)
{
    fade = 1.0;
    float r = length(p);
    if (q.shape == 1u)
        return abs(r - 0.5 * (q.p0 + q.p1)) - 0.5 * (q.p0 - q.p1);
    if (q.shape == 2u)
        return length(vec2(max(abs(p.x) - q.p0, 0.0), p.y)) - q.p1;
    if (q.shape == 3u)
    {
        float t = clamp((r - q.p1) / max(q.p0 - q.p1, 1e-9), 0.0, 1.0);
        fade = 1.0 - t * t * (3.0 - 2.0 * t);
        return r - q.p0;
    }
    if (q.shape == 4u)
        return rhombusDistance(p, vec2(q.p0, q.p1));
    return r - q.p0;
}

// How much of the pixel the shape covers (a pixel-wide ramp across its edge, d in pixels).
float shapeCoverage(float d)
{
    return clamp(0.5 - d, 0.0, 1.0);
}
