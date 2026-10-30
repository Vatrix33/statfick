#version 460
// Ink lines, feature edges (feature.vert): a pixel within three quarters of a pixel of the edge
// records the nearest point on it (its offset from the pixel's centre), that point's view depth
// and the edge's part; ink_edges.frag keeps it where it lies on the visible surface. The depth test
// keeps the nearest edge.
#include "common.glsl"
#include "mesh_types.glsl"

struct FeatureEdge { uint a; uint b; float cosine; };
layout(buffer_reference, scalar) readonly buffer FeatureEdges { FeatureEdge items[]; };
layout(push_constant, scalar) uniform FeaturePush { MeshVertices vertices; FeatureEdges edges; MeshDraw draw; MeshScene scene; float creaseCos; } pc;

layout(location = 0) flat in vec2 vA;
layout(location = 1) flat in vec2 vB;
layout(location = 2) flat in vec2 vDepth;
layout(location = 0) out uvec4 outFeature;

void main()
{
    vec2 p = gl_FragCoord.xy;
    vec2 along = vB - vA;
    float length2 = dot(along, along);
    float t = length2 > 1e-8 ? clamp(dot(p - vA, along) / length2, 0.0, 1.0) : 0.0;
    vec2 offset = vA + along * t - p;
    if (dot(offset, offset) > 0.75 * 0.75)
        discard;
    // The view depth along the edge: linear on screen for orthographic views, its reciprocal for perspective ones.
    float depth = pc.scene.orthographic != 0u ? mix(vDepth.x, vDepth.y, t) : 1.0 / mix(1.0 / vDepth.x, 1.0 / vDepth.y, t);
    uvec2 q = uvec2(round(clamp(offset / 0.75 * 0.5 + 0.5, 0.0, 1.0) * 31.0));
    outFeature = uvec4(pc.draw.surface, floatBitsToUint(depth), q.x | q.y << 5, 1u);
}
